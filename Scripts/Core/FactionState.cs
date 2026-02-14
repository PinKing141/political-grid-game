using System;

namespace PolisGrid.Core
{
    [Serializable]
    public sealed class FactionState
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public float Approval { get; set; } = 55f;
        public string CurrentDemand { get; set; } = string.Empty;

        public FactionState Clone()
        {
            return new FactionState
            {
                Id = Id,
                Name = Name,
                Approval = Approval,
                CurrentDemand = CurrentDemand
            };
        }
    }
}
