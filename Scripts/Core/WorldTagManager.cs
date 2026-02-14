using System;
using System.Collections.Generic;

namespace PolisGrid.Core
{
    public class WorldTagManager
    {
        private readonly Dictionary<string, int> _tagDurations = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, int> ActiveTags => _tagDurations;

        public void SetTag(string tag, int durationTurns)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            if (durationTurns <= 0)
            {
                _tagDurations[tag] = -1;
                return;
            }

            if (_tagDurations.TryGetValue(tag, out int existing))
            {
                if (existing < 0)
                {
                    return;
                }

                _tagDurations[tag] = Math.Max(existing, durationTurns);
                return;
            }

            _tagDurations[tag] = durationTurns;
        }

        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            _tagDurations.Remove(tag);
        }

        public bool HasTag(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && _tagDurations.ContainsKey(tag);
        }

        public int GetRemainingTurns(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return 0;
            }

            return _tagDurations.TryGetValue(tag, out int turns) ? turns : 0;
        }

        public void AdvanceTurn()
        {
            if (_tagDurations.Count == 0)
            {
                return;
            }

            List<string> expired = null;
            List<string> keys = new List<string>(_tagDurations.Keys);
            foreach (string key in keys)
            {
                int current = _tagDurations[key];
                if (current < 0)
                {
                    continue;
                }

                int next = current - 1;
                if (next <= 0)
                {
                    expired ??= new List<string>();
                    expired.Add(key);
                }
                else
                {
                    _tagDurations[key] = next;
                }
            }

            if (expired == null)
            {
                return;
            }

            foreach (string key in expired)
            {
                _tagDurations.Remove(key);
            }
        }

        public void Clear()
        {
            _tagDurations.Clear();
        }

        public void ImportTags(IEnumerable<KeyValuePair<string, int>> tags)
        {
            _tagDurations.Clear();
            if (tags == null)
            {
                return;
            }

            foreach (KeyValuePair<string, int> kvp in tags)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    continue;
                }

                _tagDurations[kvp.Key] = kvp.Value;
            }
        }
    }
}
