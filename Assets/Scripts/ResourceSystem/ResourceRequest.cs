using System;
using System.Collections.Generic;

namespace FallowEarth.ResourcesSystem
{
    /// <summary>
    /// Represents a request to consume resources from the global inventory.
    /// </summary>
    [Serializable]
    public struct ResourceRequest
    {
        public ResourceDefinition Definition { get; private set; }
        public ResourceQuality MinimumQuality { get; private set; }
        public int Amount { get; private set; }

        public ResourceRequest(ResourceDefinition definition, int amount, ResourceQuality minimumQuality = ResourceQuality.Defective)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Definition = definition;
            MinimumQuality = minimumQuality;
            Amount = amount;
        }
    }

    /// <summary>
    /// Utility helpers for building requests lists.
    /// </summary>
    public static class ResourceRequestExtensions
    {
        public static int TotalAmount(this IEnumerable<ResourceRequest> requests)
        {
            int total = 0;
            if (requests == null)
                return total;
            foreach (var req in requests)
                total += req.Amount;
            return total;
        }
    }
}
