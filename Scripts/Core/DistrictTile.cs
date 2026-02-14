using System.Collections.Generic;
using Godot;

namespace PolisGrid.Core
{
    public class DistrictTile
    {
        public Vector2I Coordinates; // Grid position (x, y)
        public PopulationDensity Density;
        public IndustryType DominantIndustry;
        public List<VoterBloc> Blocs;

        // Calculated Stats
        public int TotalPopulation => GetTotalPop();
        public float Stability;   // 0-100
        public float Turnout;     // 0-1
        public float ExternalStabilityPressure;
        public float MediaAccess;
        public CompassAxis LocalIssue;
        public PoliticalCompass AverageIdeology;

        public DistrictTile(int x, int y, PopulationDensity density, IndustryType industry)
        {
            Coordinates = new Vector2I(x, y);
            Density = density;
            DominantIndustry = industry;
            Blocs = new List<VoterBloc>();
            Stability = 50f;
            Turnout = 0.5f;
            ExternalStabilityPressure = 0f;
            MediaAccess = GetBaseMediaAccess(density);
            LocalIssue = CompassAxis.Economic;
            AverageIdeology = new PoliticalCompass(0f, 0f, 0f, 0f);
        }

        private static float GetBaseMediaAccess(PopulationDensity density)
        {
            return density switch
            {
                PopulationDensity.Metro => 0.9f,
                PopulationDensity.Urban => 0.6f,
                _ => 0.2f
            };
        }

        public void AddBloc(string name, int size, PoliticalCompass ideology)
        {
            Blocs.Add(new VoterBloc(name, size, ideology));
        }

        private int GetTotalPop()
        {
            int total = 0;
            foreach (var b in Blocs) total += b.Population;
            return total;
        }

        private CompassAxis DetermineLocalIssue(PoliticalCompass currentPolicy)
        {
            int totalPopulation = Mathf.Max(1, TotalPopulation);
            float economicPressure = 0f;
            float societalPressure = 0f;
            float authorityPressure = 0f;
            float diplomaticPressure = 0f;

            foreach (var bloc in Blocs)
            {
                float weight = (float)bloc.Population / totalPopulation;
                economicPressure += Mathf.Abs(bloc.Ideology.Economic - currentPolicy.Economic) * weight;
                societalPressure += Mathf.Abs(bloc.Ideology.Societal - currentPolicy.Societal) * weight;
                authorityPressure += Mathf.Abs(bloc.Ideology.Authority - currentPolicy.Authority) * weight;
                diplomaticPressure += Mathf.Abs(bloc.Ideology.Diplomatic - currentPolicy.Diplomatic) * weight;
            }

            float max = economicPressure;
            CompassAxis issue = CompassAxis.Economic;

            if (societalPressure > max)
            {
                max = societalPressure;
                issue = CompassAxis.Societal;
            }
            if (authorityPressure > max)
            {
                max = authorityPressure;
                issue = CompassAxis.Authority;
            }
            if (diplomaticPressure > max)
            {
                issue = CompassAxis.Diplomatic;
            }

            return issue;
        }

        public void ProcessTurn(PoliticalCompass currentPolicy, PolicyTurnEffects policyEffects = null)
        {
            LocalIssue = DetermineLocalIssue(currentPolicy);

            float weightedHappiness = 0f;
            float weightedTurnout = 0f;
            float weightedEconomic = 0f;
            float weightedSocietal = 0f;
            float weightedAuthority = 0f;
            float weightedDiplomatic = 0f;
            int totalPopulation = Mathf.Max(1, TotalPopulation);

            foreach (var bloc in Blocs)
            {
                bloc.ProcessTurn(currentPolicy, LocalIssue, policyEffects);
                float popWeight = (float)bloc.Population / totalPopulation;

                weightedHappiness += bloc.Happiness * popWeight;
                weightedTurnout += bloc.TurnoutChance * popWeight;
                weightedEconomic += bloc.Ideology.Economic * popWeight;
                weightedSocietal += bloc.Ideology.Societal * popWeight;
                weightedAuthority += bloc.Ideology.Authority * popWeight;
                weightedDiplomatic += bloc.Ideology.Diplomatic * popWeight;
            }

            Turnout = Mathf.Clamp(weightedTurnout, 0f, 1f);
            Stability = Mathf.Clamp(weightedHappiness * 100f - (1f - weightedHappiness) * 10f + ExternalStabilityPressure, 0f, 100f);
            AverageIdeology = new PoliticalCompass(weightedEconomic, weightedSocietal, weightedAuthority, weightedDiplomatic);
        }
    }
}
