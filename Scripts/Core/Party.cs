using System;
using Godot;

namespace PolisGrid.Core
{
    [Serializable]
    public class Party
    {
        public string Name;
        public Color Color;
        public PoliticalCompass Platform;
        public float Opportunism; // 0 = rigid, 1 = fully opportunistic

        public Party(string name, Color color, PoliticalCompass platform, float opportunism)
        {
            Name = name;
            Color = color;
            Platform = platform;
            Opportunism = Mathf.Clamp(opportunism, 0f, 1f);
        }

        public float AlignmentScore(PoliticalCompass voterIdeology)
        {
            float normalizedDistance = voterIdeology.DistanceTo(Platform) / PoliticalCompass.MaxDistance;
            return Mathf.Clamp(1f - normalizedDistance, 0f, 1f);
        }

        public void DriftTowards(PoliticalCompass target, float baseAmount)
        {
            float amount = Mathf.Clamp(baseAmount * (0.2f + Opportunism * 0.8f), 0f, 1f);
            Platform = Platform.NudgeTowards(target, amount);
        }
    }
}
