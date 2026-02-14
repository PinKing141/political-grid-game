using System;
using System.Collections.Generic;
using Godot;

namespace PolisGrid.Core
{
    [Serializable]
    public class Policy
    {
        public const string ShiftEconomic = "policy.shift.economic";
        public const string ShiftSocietal = "policy.shift.societal";
        public const string ShiftAuthority = "policy.shift.authority";
        public const string ShiftDiplomatic = "policy.shift.diplomatic";

        public const string StabilityOffset = "national.stability.offset";
        public const string TurnoutOffset = "national.turnout.offset";
        public const string TreasuryPerTick = "economy.treasury.per_tick";
        public const string PoliticalCapitalPerTick = "state.capital.per_tick";
        public const string BlocHappinessPrefix = "bloc.happiness.";
        public const string BlocTurnoutPrefix = "bloc.turnout.";

        public string Name { get; }
        public float PoliticalCapitalCost { get; }
        public IReadOnlyDictionary<string, float> Effects => _effects;

        private readonly Dictionary<string, float> _effects = new Dictionary<string, float>();

        public Policy(string name, float politicalCapitalCost, IDictionary<string, float> effects)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Unnamed Policy" : name;
            PoliticalCapitalCost = Mathf.Max(0f, politicalCapitalCost);

            if (effects == null)
            {
                return;
            }

            foreach (KeyValuePair<string, float> kvp in effects)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    continue;
                }

                _effects[kvp.Key] = kvp.Value;
            }
        }

        public float GetEffectOrDefault(string key, float fallback = 0f)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            return _effects.TryGetValue(key, out float value) ? value : fallback;
        }

        public static string HappinessKey(BlocCategory category)
        {
            return $"{BlocHappinessPrefix}{category}";
        }

        public static string TurnoutKey(BlocCategory category)
        {
            return $"{BlocTurnoutPrefix}{category}";
        }
    }
}
