using System.Collections.Generic;
using Godot;

namespace PolisGrid.Core
{
    public sealed class PolicyStateSnapshot
    {
        public string Name;
        public bool TargetActive;
        public float CurrentStrength;
        public float StepPerTurn;
    }

    public class PolicyManager
    {
        public IReadOnlyCollection<string> ActivePolicyNames => _activePolicyNames;

        private readonly Dictionary<string, Policy> _policiesByName = new Dictionary<string, Policy>();
        private readonly HashSet<string> _activePolicyNames = new HashSet<string>();
        private readonly Dictionary<string, PolicyRampState> _policyStates = new Dictionary<string, PolicyRampState>();

        private sealed class PolicyRampState
        {
            public bool TargetActive;
            public float CurrentStrength;
            public float StepPerTurn;
        }

        public void RegisterPolicy(Policy policy)
        {
            if (policy == null)
            {
                return;
            }

            _policiesByName[policy.Name] = policy;
        }

        public IReadOnlyCollection<string> GetAllPolicyNames()
        {
            return _policiesByName.Keys;
        }

        public void ClearAll()
        {
            _policiesByName.Clear();
            _activePolicyNames.Clear();
            _policyStates.Clear();
        }

        public bool HasPolicy(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && _policiesByName.ContainsKey(name);
        }

        public bool IsActive(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && _activePolicyNames.Contains(name);
        }

        public float GetPolicyStrength(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return 0f;
            }

            return _policyStates.TryGetValue(name, out PolicyRampState state) ? state.CurrentStrength : 0f;
        }

        public bool TryGetPolicy(string name, out Policy policy)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                policy = null;
                return false;
            }

            return _policiesByName.TryGetValue(name, out policy);
        }

        public bool SetPolicyActive(string name, bool active, int rampTurns)
        {
            if (string.IsNullOrWhiteSpace(name) || !_policiesByName.ContainsKey(name))
            {
                return false;
            }

            int turns = Mathf.Max(1, rampTurns);

            if (!_policyStates.TryGetValue(name, out PolicyRampState state))
            {
                state = new PolicyRampState
                {
                    TargetActive = active,
                    CurrentStrength = active ? 0f : 0f,
                    StepPerTurn = 1f / turns
                };

                _policyStates[name] = state;
            }
            else
            {
                state.TargetActive = active;
                state.StepPerTurn = 1f / turns;
            }

            if (active)
            {
                _activePolicyNames.Add(name);
            }
            else
            {
                _activePolicyNames.Remove(name);
            }

            return true;
        }

        public void AdvancePolicyRamps()
        {
            if (_policyStates.Count == 0)
            {
                return;
            }

            List<string> removeList = null;

            foreach (KeyValuePair<string, PolicyRampState> kvp in _policyStates)
            {
                PolicyRampState state = kvp.Value;
                float target = state.TargetActive ? 1f : 0f;

                state.CurrentStrength = Mathf.MoveToward(state.CurrentStrength, target, Mathf.Clamp(state.StepPerTurn, 0.0001f, 1f));

                if (!state.TargetActive && state.CurrentStrength <= 0.0001f)
                {
                    removeList ??= new List<string>();
                    removeList.Add(kvp.Key);
                }
            }

            if (removeList == null)
            {
                return;
            }

            foreach (string key in removeList)
            {
                _policyStates.Remove(key);
            }
        }

        public Dictionary<string, float> GetCombinedEffects()
        {
            Dictionary<string, float> combined = new Dictionary<string, float>();

            foreach (KeyValuePair<string, PolicyRampState> stateEntry in _policyStates)
            {
                string policyName = stateEntry.Key;
                float strength = Mathf.Clamp(stateEntry.Value.CurrentStrength, 0f, 1f);
                if (strength <= 0.0001f)
                {
                    continue;
                }

                if (!_policiesByName.TryGetValue(policyName, out Policy policy))
                {
                    continue;
                }

                foreach (KeyValuePair<string, float> kvp in policy.Effects)
                {
                    float weighted = kvp.Value * strength;
                    if (combined.TryGetValue(kvp.Key, out float current))
                    {
                        combined[kvp.Key] = current + weighted;
                    }
                    else
                    {
                        combined[kvp.Key] = weighted;
                    }
                }
            }

            return combined;
        }

        public float GetCombinedEffect(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return 0f;
            }

            float total = 0f;
            foreach (KeyValuePair<string, PolicyRampState> stateEntry in _policyStates)
            {
                string policyName = stateEntry.Key;
                float strength = Mathf.Clamp(stateEntry.Value.CurrentStrength, 0f, 1f);
                if (strength <= 0.0001f)
                {
                    continue;
                }

                if (_policiesByName.TryGetValue(policyName, out Policy policy))
                {
                    total += policy.GetEffectOrDefault(key) * strength;
                }
            }

            return total;
        }

        public PoliticalCompass GetPolicyShift()
        {
            return new PoliticalCompass(
                GetCombinedEffect(Policy.ShiftEconomic),
                GetCombinedEffect(Policy.ShiftSocietal),
                GetCombinedEffect(Policy.ShiftAuthority),
                GetCombinedEffect(Policy.ShiftDiplomatic));
        }

        public Dictionary<string, float> GetPolicyContributionsForBloc(BlocCategory category, bool turnout)
        {
            Dictionary<string, float> contributions = new Dictionary<string, float>();
            string key = turnout ? Policy.TurnoutKey(category) : Policy.HappinessKey(category);
            string generalKey = turnout ? Policy.TurnoutKey(BlocCategory.General) : Policy.HappinessKey(BlocCategory.General);

            foreach (KeyValuePair<string, PolicyRampState> stateEntry in _policyStates)
            {
                float strength = Mathf.Clamp(stateEntry.Value.CurrentStrength, 0f, 1f);
                if (strength <= 0.0001f)
                {
                    continue;
                }

                if (!_policiesByName.TryGetValue(stateEntry.Key, out Policy policy))
                {
                    continue;
                }

                float value = policy.GetEffectOrDefault(generalKey) + policy.GetEffectOrDefault(key);
                float weighted = value * strength;
                if (Mathf.Abs(weighted) <= 0.0001f)
                {
                    continue;
                }

                contributions[policy.Name] = weighted;
            }

            return contributions;
        }

        public List<PolicyStateSnapshot> ExportPolicyStates()
        {
            List<PolicyStateSnapshot> snapshots = new List<PolicyStateSnapshot>();
            foreach (KeyValuePair<string, PolicyRampState> kvp in _policyStates)
            {
                snapshots.Add(new PolicyStateSnapshot
                {
                    Name = kvp.Key,
                    TargetActive = kvp.Value.TargetActive,
                    CurrentStrength = kvp.Value.CurrentStrength,
                    StepPerTurn = kvp.Value.StepPerTurn
                });
            }

            return snapshots;
        }

        public void ImportPolicyStates(IEnumerable<PolicyStateSnapshot> snapshots)
        {
            _policyStates.Clear();
            _activePolicyNames.Clear();

            if (snapshots == null)
            {
                return;
            }

            foreach (PolicyStateSnapshot snapshot in snapshots)
            {
                if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.Name) || !_policiesByName.ContainsKey(snapshot.Name))
                {
                    continue;
                }

                _policyStates[snapshot.Name] = new PolicyRampState
                {
                    TargetActive = snapshot.TargetActive,
                    CurrentStrength = Mathf.Clamp(snapshot.CurrentStrength, 0f, 1f),
                    StepPerTurn = Mathf.Clamp(snapshot.StepPerTurn, 0.0001f, 1f)
                };

                if (snapshot.TargetActive)
                {
                    _activePolicyNames.Add(snapshot.Name);
                }
            }
        }
    }
}
