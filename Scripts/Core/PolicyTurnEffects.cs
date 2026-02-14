using System.Collections.Generic;

namespace PolisGrid.Core
{
    public class PolicyTurnEffects
    {
        public float GlobalHappinessOffset;
        public float GlobalTurnoutOffset;

        private readonly Dictionary<BlocCategory, float> _happinessByCategory = new Dictionary<BlocCategory, float>();
        private readonly Dictionary<BlocCategory, float> _turnoutByCategory = new Dictionary<BlocCategory, float>();

        public void AddHappinessOffset(BlocCategory category, float value)
        {
            if (_happinessByCategory.TryGetValue(category, out float current))
            {
                _happinessByCategory[category] = current + value;
            }
            else
            {
                _happinessByCategory[category] = value;
            }
        }

        public void AddTurnoutOffset(BlocCategory category, float value)
        {
            if (_turnoutByCategory.TryGetValue(category, out float current))
            {
                _turnoutByCategory[category] = current + value;
            }
            else
            {
                _turnoutByCategory[category] = value;
            }
        }

        public float GetHappinessOffset(BlocCategory category)
        {
            return GetOffset(_happinessByCategory, category);
        }

        public float GetTurnoutOffset(BlocCategory category)
        {
            return GetOffset(_turnoutByCategory, category);
        }

        private static float GetOffset(Dictionary<BlocCategory, float> source, BlocCategory category)
        {
            float value = 0f;

            if (source.TryGetValue(BlocCategory.General, out float general))
            {
                value += general;
            }

            if (category != BlocCategory.General && source.TryGetValue(category, out float specific))
            {
                value += specific;
            }

            return value;
        }
    }
}