using System;

namespace PolisGrid.Core
{
    [Serializable]
    public sealed class CandidateTraitDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public float CharismaDelta { get; set; }
        public float CompetenceDelta { get; set; }
        public float IntegrityDelta { get; set; }
        public float PopulismDelta { get; set; }

        public float PolicyCostMultiplierDelta { get; set; }
        public float PropagandaCostMultiplierDelta { get; set; }
        public float CampaignStrengthMultiplierDelta { get; set; }
        public float PollingBonusDelta { get; set; }
        public float CapitalGainMultiplierDelta { get; set; }
        public float DebtStressMitigationDelta { get; set; }
        public float EventShockMultiplierDelta { get; set; }
    }
}
