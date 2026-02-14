using Godot;
using System;

namespace PolisGrid.Core
{
    public enum CompassAxis
    {
        Economic = 0,
        Societal = 1,
        Authority = 2,
        Diplomatic = 3
    }

    [Serializable]
    public struct PoliticalCompass
    {
        public const float MinValue = -1f;
        public const float MaxValue = 1f;
        public const float MaxDistance = 4f;

        // Range: -1.0 to 1.0
        public float Economic;   // -1 (Collectivist) to +1 (Market)
        public float Societal;   // -1 (Tradition) to +1 (Progress)
        public float Authority;  // -1 (Liberty) to +1 (Order)
        public float Diplomatic; // -1 (Sovereignty) to +1 (Globalism)

        public PoliticalCompass(float econ, float soc, float auth, float dip)
        {
            Economic = Mathf.Clamp(econ, MinValue, MaxValue);
            Societal = Mathf.Clamp(soc, MinValue, MaxValue);
            Authority = Mathf.Clamp(auth, MinValue, MaxValue);
            Diplomatic = Mathf.Clamp(dip, MinValue, MaxValue);
        }

        // Euclidean distance: Returns 0.0 (Identical) to ~4.0 (Complete Opposite)
        // Lower number = Higher agreement
        public float DistanceTo(PoliticalCompass other)
        {
            float dE = Economic - other.Economic;
            float dS = Societal - other.Societal;
            float dA = Authority - other.Authority;
            float dD = Diplomatic - other.Diplomatic;

            return Mathf.Sqrt(dE*dE + dS*dS + dA*dA + dD*dD);
        }

        public float GetAxis(CompassAxis axis)
        {
            return axis switch
            {
                CompassAxis.Economic => Economic,
                CompassAxis.Societal => Societal,
                CompassAxis.Authority => Authority,
                CompassAxis.Diplomatic => Diplomatic,
                _ => Economic
            };
        }

        public PoliticalCompass Offset(PoliticalCompass delta)
        {
            return new PoliticalCompass(
                Economic + delta.Economic,
                Societal + delta.Societal,
                Authority + delta.Authority,
                Diplomatic + delta.Diplomatic
            );
        }

        // Helper for "Drift" (moving slightly towards a target)
        public PoliticalCompass NudgeTowards(PoliticalCompass target, float amount)
        {
            return new PoliticalCompass(
                Mathf.Lerp(Economic, target.Economic, amount),
                Mathf.Lerp(Societal, target.Societal, amount),
                Mathf.Lerp(Authority, target.Authority, amount),
                Mathf.Lerp(Diplomatic, target.Diplomatic, amount)
            );
        }

        public static PoliticalCompass operator +(PoliticalCompass a, PoliticalCompass b)
        {
            return a.Offset(b);
        }

        public static PoliticalCompass operator -(PoliticalCompass a, PoliticalCompass b)
        {
            return new PoliticalCompass(
                a.Economic - b.Economic,
                a.Societal - b.Societal,
                a.Authority - b.Authority,
                a.Diplomatic - b.Diplomatic
            );
        }

        public static PoliticalCompass operator *(PoliticalCompass a, float scalar)
        {
            return new PoliticalCompass(
                a.Economic * scalar,
                a.Societal * scalar,
                a.Authority * scalar,
                a.Diplomatic * scalar
            );
        }
    }
}
