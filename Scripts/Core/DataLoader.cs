using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

namespace PolisGrid.Core
{
    public static class DataLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static bool TryLoadPolicies(out List<Policy> policies)
        {
            policies = new List<Policy>();
            if (!TryReadJsonFile("res://Data/policies.json", out PolicyRoot root) || root?.Policies == null)
            {
                return false;
            }

            foreach (PolicyDto dto in root.Policies)
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                {
                    continue;
                }

                Dictionary<string, float> effects = dto.Effects ?? new Dictionary<string, float>();
                policies.Add(new Policy(dto.Name, dto.CapitalCost, effects));
            }

            return policies.Count > 0;
        }

        public static bool TryLoadEvents(out List<SimulationEvent> events)
        {
            events = new List<SimulationEvent>();
            if (!TryReadJsonFile("res://Data/events.json", out EventRoot root) || root?.Events == null)
            {
                return false;
            }

            foreach (EventDto dto in root.Events)
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
                {
                    continue;
                }

                PoliticalCompass shift = ToCompass(dto.VoterShift);
                List<EventWeightRule> weightRules = new List<EventWeightRule>();
                if (dto.WeightRules != null)
                {
                    foreach (WeightRuleDto wr in dto.WeightRules)
                    {
                        if (wr == null)
                        {
                            continue;
                        }

                        weightRules.Add(new EventWeightRule
                        {
                            RequireTag = wr.RequireTag,
                            ForbidTag = wr.ForbidTag,
                            MinUnrest = wr.MinUnrest,
                            MaxUnrest = wr.MaxUnrest,
                            MinStability = wr.MinStability,
                            MaxStability = wr.MaxStability,
                            WeightMultiplier = wr.WeightMultiplier,
                            WeightAdd = wr.WeightAdd
                        });
                    }
                }

                List<EventTagEffect> tagEffects = new List<EventTagEffect>();
                if (dto.TagEffects != null)
                {
                    foreach (TagEffectDto te in dto.TagEffects)
                    {
                        if (te == null || string.IsNullOrWhiteSpace(te.Tag))
                        {
                            continue;
                        }

                        tagEffects.Add(new EventTagEffect
                        {
                            Tag = te.Tag,
                            DurationTurns = te.DurationTurns
                        });
                    }
                }

                events.Add(new SimulationEvent(
                    dto.Title,
                    dto.Description ?? string.Empty,
                    shift,
                    dto.StabilityImpact,
                    dto.Weight <= 0f ? 0.01f : dto.Weight,
                    weightRules,
                    tagEffects));
            }

            return events.Count > 0;
        }

        public static bool TryLoadDefaultParties(out List<Party> parties)
        {
            parties = new List<Party>();
            if (!TryReadJsonFile("res://Data/parties_defaults.json", out PartyRoot root) || root?.Parties == null)
            {
                return false;
            }

            foreach (PartyDto dto in root.Parties)
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                {
                    continue;
                }

                Color color = Color.FromString(dto.Color ?? "#ffffff", Colors.White);
                PoliticalCompass platform = ToCompass(dto.Platform);
                parties.Add(new Party(dto.Name, color, platform, dto.Opportunism));
            }

            return parties.Count > 0;
        }

        public static bool TryLoadCandidateTraits(out List<CandidateTraitDefinition> traits)
        {
            traits = new List<CandidateTraitDefinition>();
            if (!TryReadJsonFile("res://Data/candidate_traits.json", out CandidateTraitRoot root) || root?.Traits == null)
            {
                return false;
            }

            foreach (CandidateTraitDefinition dto in root.Traits)
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Id))
                {
                    continue;
                }

                traits.Add(dto);
            }

            return traits.Count > 0;
        }

        public static bool TryLoadCandidateBackgrounds(out List<CandidateBackgroundDefinition> backgrounds)
        {
            backgrounds = new List<CandidateBackgroundDefinition>();
            if (!TryReadJsonFile("res://Data/backgrounds.json", out CandidateBackgroundRoot root) || root?.Backgrounds == null)
            {
                return false;
            }

            foreach (CandidateBackgroundDefinition dto in root.Backgrounds)
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Id))
                {
                    continue;
                }

                backgrounds.Add(dto);
            }

            return backgrounds.Count > 0;
        }

        private static PoliticalCompass ToCompass(CompassDto dto)
        {
            if (dto == null)
            {
                return new PoliticalCompass(0f, 0f, 0f, 0f);
            }

            return new PoliticalCompass(dto.Economic, dto.Societal, dto.Authority, dto.Diplomatic);
        }

        private static bool TryReadJsonFile<T>(string resourcePath, out T payload)
        {
            payload = default;

            string absolutePath = ProjectSettings.GlobalizePath(resourcePath);
            if (!File.Exists(absolutePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(absolutePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                payload = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return payload != null;
            }
            catch (Exception ex)
            {
                GD.PushWarning($"DataLoader: failed to load '{resourcePath}': {ex.Message}");
                return false;
            }
        }

        private sealed class PolicyRoot
        {
            public List<PolicyDto> Policies { get; set; }
        }

        private sealed class PolicyDto
        {
            public string Name { get; set; }
            public float CapitalCost { get; set; }
            public Dictionary<string, float> Effects { get; set; }
        }

        private sealed class EventRoot
        {
            public List<EventDto> Events { get; set; }
        }

        private sealed class EventDto
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public CompassDto VoterShift { get; set; }
            public float StabilityImpact { get; set; }
            public float Weight { get; set; }
            public List<WeightRuleDto> WeightRules { get; set; }
            public List<TagEffectDto> TagEffects { get; set; }
        }

        private sealed class WeightRuleDto
        {
            public string RequireTag { get; set; }
            public string ForbidTag { get; set; }
            public float MinUnrest { get; set; } = float.NaN;
            public float MaxUnrest { get; set; } = float.NaN;
            public float MinStability { get; set; } = float.NaN;
            public float MaxStability { get; set; } = float.NaN;
            public float WeightMultiplier { get; set; } = 1f;
            public float WeightAdd { get; set; } = 0f;
        }

        private sealed class TagEffectDto
        {
            public string Tag { get; set; }
            public int DurationTurns { get; set; }
        }

        private sealed class PartyRoot
        {
            public List<PartyDto> Parties { get; set; }
        }

        private sealed class PartyDto
        {
            public string Name { get; set; }
            public string Color { get; set; }
            public CompassDto Platform { get; set; }
            public float Opportunism { get; set; }
        }

        private sealed class CompassDto
        {
            public float Economic { get; set; }
            public float Societal { get; set; }
            public float Authority { get; set; }
            public float Diplomatic { get; set; }
        }

        private sealed class CandidateTraitRoot
        {
            public List<CandidateTraitDefinition> Traits { get; set; }
        }

        private sealed class CandidateBackgroundRoot
        {
            public List<CandidateBackgroundDefinition> Backgrounds { get; set; }
        }
    }
}
