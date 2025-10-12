using System;
using System.Collections.Generic;
using FallowEarth.ResourcesSystem;

namespace FallowEarth.Research
{
    /// <summary>
    /// Represents a technology that can be researched to unlock new content.
    /// </summary>
    [Serializable]
    public class TechnologyDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> Prerequisites { get; }
        public int ResearchCost { get; }
        public IReadOnlyList<ResourceRequest> UnlockCosts { get; }

        public TechnologyDefinition(string id, string displayName, int researchCost, IEnumerable<string> prerequisites, IEnumerable<ResourceRequest> unlockCosts)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Technology id cannot be null or empty", nameof(id));
            if (string.IsNullOrEmpty(displayName))
                throw new ArgumentException("Display name cannot be null or empty", nameof(displayName));
            if (researchCost <= 0)
                throw new ArgumentOutOfRangeException(nameof(researchCost));

            Id = id;
            DisplayName = displayName;
            ResearchCost = researchCost;
            Prerequisites = prerequisites != null ? new List<string>(prerequisites) : new List<string>();
            UnlockCosts = unlockCosts != null ? new List<ResourceRequest>(unlockCosts) : new List<ResourceRequest>();
        }
    }
}
