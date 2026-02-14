using System;
using System.Collections.Generic;

namespace PolisGrid.Core
{
    [Serializable]
    public class EventWeightRule
    {
        public string RequireTag;
        public string ForbidTag;
        public float MinUnrest = float.NaN;
        public float MaxUnrest = float.NaN;
        public float MinStability = float.NaN;
        public float MaxStability = float.NaN;
        public float WeightMultiplier = 1f;
        public float WeightAdd = 0f;
    }

    [Serializable]
    public class EventTagEffect
    {
        public string Tag;
        public int DurationTurns;
    }

    [Serializable]
    public class SimulationEvent
    {
        public string Title;
        public string Description;
        public PoliticalCompass VoterShift;
        public float StabilityImpact;
        public float Weight;
        public List<EventWeightRule> WeightRules;
        public List<EventTagEffect> TagEffects;

        public SimulationEvent(
            string title,
            string description,
            PoliticalCompass voterShift,
            float stabilityImpact,
            float weight = 1f,
            List<EventWeightRule> weightRules = null,
            List<EventTagEffect> tagEffects = null)
        {
            Title = title;
            Description = description;
            VoterShift = voterShift;
            StabilityImpact = stabilityImpact;
            Weight = weight;
            WeightRules = weightRules ?? new List<EventWeightRule>();
            TagEffects = tagEffects ?? new List<EventTagEffect>();
        }
    }
}
