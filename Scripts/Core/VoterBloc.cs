using System;
using Godot;

namespace PolisGrid.Core
{
    [Serializable]
    public class VoterBloc
    {
        public string Name;          // e.g., "Industrial Workers", "Students"
        public int Population;
        public BlocCategory Category;
        public PoliticalCompass Ideology;
        public float Happiness;      // 0.0 to 1.0
        public float TurnoutChance;  // 0.0 to 1.0
        public float Anger;          // 0.0 to 1.0
        public float Hope;           // 0.0 to 1.0
        public float LastIdeologyHappinessPenalty; // 0.0 to 1.0
        public float LastIssueHappinessPenalty;    // 0.0 to 1.0
        public float LastPolicyHappinessModifier;  // -1.0 to 1.0
        public float LastPolicyTurnoutModifier;    // -1.0 to 1.0

        public VoterBloc(string name, int pop, PoliticalCompass ideology)
        {
            Name = name;
            Population = pop;
            Category = InferCategory(name);
            Ideology = ideology;
            Happiness = 0.5f;        // Start neutral
            TurnoutChance = 0.6f;
            Anger = 0.4f;
            Hope = 0.5f;
        }

        public void ProcessTurn(PoliticalCompass governmentPolicy, CompassAxis localIssue, PolicyTurnEffects policyEffects = null)
        {
            float normalizedDistance = Ideology.DistanceTo(governmentPolicy) / PoliticalCompass.MaxDistance;
            float issueDistance = Mathf.Abs(Ideology.GetAxis(localIssue) - governmentPolicy.GetAxis(localIssue)) * 0.5f;
            LastIdeologyHappinessPenalty = normalizedDistance * 0.75f;
            LastIssueHappinessPenalty = issueDistance * 0.45f;

            Happiness = Mathf.Clamp(1.0f - (LastIdeologyHappinessPenalty + LastIssueHappinessPenalty), 0f, 1f);
            LastPolicyHappinessModifier = 0f;

            if (policyEffects != null)
            {
                LastPolicyHappinessModifier = policyEffects.GetHappinessOffset(Category);
                Happiness = Mathf.Clamp(Happiness + LastPolicyHappinessModifier, 0f, 1f);
            }

            Anger = Mathf.Clamp(1.0f - Happiness + issueDistance * 0.2f, 0f, 1f);
            Hope = Mathf.Clamp(Happiness * 0.8f + (1.0f - issueDistance) * 0.2f, 0f, 1f);
            TurnoutChance = Mathf.Clamp(0.2f + Anger * 0.35f + Hope * 0.45f, 0f, 1f);
            LastPolicyTurnoutModifier = 0f;

            if (policyEffects != null)
            {
                LastPolicyTurnoutModifier = policyEffects.GetTurnoutOffset(Category);
                TurnoutChance = Mathf.Clamp(TurnoutChance + LastPolicyTurnoutModifier, 0f, 1f);
            }
        }

        public void ApplyGlobalShift(PoliticalCompass shift, float strength = 1f)
        {
            float clampedStrength = Mathf.Clamp(strength, 0f, 1f);
            Ideology = Ideology + (shift * clampedStrength);
        }

        public void NudgeIdeologyTowards(PoliticalCompass target, float amount)
        {
            Ideology = Ideology.NudgeTowards(target, Mathf.Clamp(amount, 0f, 1f));
        }

        private static BlocCategory InferCategory(string blocName)
        {
            if (string.IsNullOrWhiteSpace(blocName))
            {
                return BlocCategory.General;
            }

            string name = blocName.ToLowerInvariant();

            if (name.Contains("student"))
            {
                return BlocCategory.Students;
            }

            if (name.Contains("retire"))
            {
                return BlocCategory.Retirees;
            }

            if (name.Contains("farm") || name.Contains("agrarian"))
            {
                return BlocCategory.Farmers;
            }

            if (name.Contains("manager") || name.Contains("business"))
            {
                return BlocCategory.Business;
            }

            if (name.Contains("knowledge") || name.Contains("professional") || name.Contains("tech"))
            {
                return BlocCategory.Professionals;
            }

            if (name.Contains("worker") || name.Contains("labour") || name.Contains("trades") || name.Contains("precariat"))
            {
                return BlocCategory.Workers;
            }

            return BlocCategory.General;
        }
    }
}
