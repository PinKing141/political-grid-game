using System;
using System.Collections.Generic;
using Godot;

namespace PolisGrid.Core
{
    [Serializable]
    public sealed class CandidateProfile
    {
        public string Name { get; set; } = "Candidate";
        public string BackgroundId { get; set; } = string.Empty;
        public List<string> TraitIds { get; set; } = new List<string>();

        public float Charisma { get; set; } = 0.5f;
        public float Competence { get; set; } = 0.5f;
        public float Integrity { get; set; } = 0.5f;
        public float Populism { get; set; } = 0.5f;

        public float PolicyCostMultiplier { get; set; } = 1f;
        public float PropagandaCostMultiplier { get; set; } = 1f;
        public float CampaignStrengthMultiplier { get; set; } = 1f;
        public float PollingBonus { get; set; }
        public float CapitalGainMultiplier { get; set; } = 1f;
        public float DebtStressMitigation { get; set; }
        public float EventShockMultiplier { get; set; } = 1f;

        public static CandidateProfile Build(
            string name,
            string backgroundId,
            IEnumerable<string> traitIds,
            IReadOnlyDictionary<string, CandidateBackgroundDefinition> backgrounds,
            IReadOnlyDictionary<string, CandidateTraitDefinition> traits)
        {
            CandidateProfile profile = new CandidateProfile
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Candidate" : name.Trim(),
                BackgroundId = backgroundId ?? string.Empty,
                TraitIds = new List<string>()
            };

            float charisma = 0.5f;
            float competence = 0.5f;
            float integrity = 0.5f;
            float populism = 0.5f;

            float policyCostMultiplier = 1f;
            float propagandaCostMultiplier = 1f;
            float campaignStrengthMultiplier = 1f;
            float pollingBonus = 0f;
            float capitalGainMultiplier = 1f;
            float debtStressMitigation = 0f;
            float eventShockMultiplier = 1f;

            if (!string.IsNullOrWhiteSpace(backgroundId) && backgrounds != null && backgrounds.TryGetValue(backgroundId, out CandidateBackgroundDefinition background))
            {
                ApplyBackground(background,
                    ref charisma,
                    ref competence,
                    ref integrity,
                    ref populism,
                    ref policyCostMultiplier,
                    ref propagandaCostMultiplier,
                    ref campaignStrengthMultiplier,
                    ref pollingBonus,
                    ref capitalGainMultiplier,
                    ref debtStressMitigation,
                    ref eventShockMultiplier);
            }

            if (traitIds != null)
            {
                HashSet<string> dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string traitIdRaw in traitIds)
                {
                    string traitId = traitIdRaw?.Trim() ?? string.Empty;
                    if (traitId.Length == 0 || !dedupe.Add(traitId))
                    {
                        continue;
                    }

                    profile.TraitIds.Add(traitId);

                    if (traits == null || !traits.TryGetValue(traitId, out CandidateTraitDefinition trait))
                    {
                        continue;
                    }

                    ApplyTrait(trait,
                        ref charisma,
                        ref competence,
                        ref integrity,
                        ref populism,
                        ref policyCostMultiplier,
                        ref propagandaCostMultiplier,
                        ref campaignStrengthMultiplier,
                        ref pollingBonus,
                        ref capitalGainMultiplier,
                        ref debtStressMitigation,
                        ref eventShockMultiplier);
                }
            }

            profile.Charisma = Mathf.Clamp(charisma, 0f, 1f);
            profile.Competence = Mathf.Clamp(competence, 0f, 1f);
            profile.Integrity = Mathf.Clamp(integrity, 0f, 1f);
            profile.Populism = Mathf.Clamp(populism, 0f, 1f);

            profile.PolicyCostMultiplier = Mathf.Clamp(policyCostMultiplier, 0.7f, 1.35f);
            profile.PropagandaCostMultiplier = Mathf.Clamp(propagandaCostMultiplier, 0.7f, 1.35f);
            profile.CampaignStrengthMultiplier = Mathf.Clamp(campaignStrengthMultiplier, 0.75f, 1.5f);
            profile.PollingBonus = Mathf.Clamp(pollingBonus, -0.08f, 0.08f);
            profile.CapitalGainMultiplier = Mathf.Clamp(capitalGainMultiplier, 0.7f, 1.4f);
            profile.DebtStressMitigation = Mathf.Clamp(debtStressMitigation, 0f, 0.6f);
            profile.EventShockMultiplier = Mathf.Clamp(eventShockMultiplier, 0.7f, 1.3f);

            return profile;
        }

        private static void ApplyBackground(
            CandidateBackgroundDefinition def,
            ref float charisma,
            ref float competence,
            ref float integrity,
            ref float populism,
            ref float policyCostMultiplier,
            ref float propagandaCostMultiplier,
            ref float campaignStrengthMultiplier,
            ref float pollingBonus,
            ref float capitalGainMultiplier,
            ref float debtStressMitigation,
            ref float eventShockMultiplier)
        {
            charisma += def.CharismaDelta;
            competence += def.CompetenceDelta;
            integrity += def.IntegrityDelta;
            populism += def.PopulismDelta;

            policyCostMultiplier += def.PolicyCostMultiplierDelta;
            propagandaCostMultiplier += def.PropagandaCostMultiplierDelta;
            campaignStrengthMultiplier += def.CampaignStrengthMultiplierDelta;
            pollingBonus += def.PollingBonusDelta;
            capitalGainMultiplier += def.CapitalGainMultiplierDelta;
            debtStressMitigation += def.DebtStressMitigationDelta;
            eventShockMultiplier += def.EventShockMultiplierDelta;
        }

        private static void ApplyTrait(
            CandidateTraitDefinition def,
            ref float charisma,
            ref float competence,
            ref float integrity,
            ref float populism,
            ref float policyCostMultiplier,
            ref float propagandaCostMultiplier,
            ref float campaignStrengthMultiplier,
            ref float pollingBonus,
            ref float capitalGainMultiplier,
            ref float debtStressMitigation,
            ref float eventShockMultiplier)
        {
            charisma += def.CharismaDelta;
            competence += def.CompetenceDelta;
            integrity += def.IntegrityDelta;
            populism += def.PopulismDelta;

            policyCostMultiplier += def.PolicyCostMultiplierDelta;
            propagandaCostMultiplier += def.PropagandaCostMultiplierDelta;
            campaignStrengthMultiplier += def.CampaignStrengthMultiplierDelta;
            pollingBonus += def.PollingBonusDelta;
            capitalGainMultiplier += def.CapitalGainMultiplierDelta;
            debtStressMitigation += def.DebtStressMitigationDelta;
            eventShockMultiplier += def.EventShockMultiplierDelta;
        }
    }
}
