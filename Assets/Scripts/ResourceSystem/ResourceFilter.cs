using System;
using System.Collections.Generic;
using System.Linq;

namespace FallowEarth.ResourcesSystem
{
    /// <summary>
    /// Filter that controls which resources are accepted by a storage zone.
    /// </summary>
    [Serializable]
    public class ResourceFilter
    {
        public HashSet<string> AllowedResourceIds { get; } = new HashSet<string>();
        public HashSet<ResourceCategory> AllowedCategories { get; } = new HashSet<ResourceCategory>();
        public HashSet<ResourceQuality> AllowedQualities { get; } = new HashSet<ResourceQuality>();

        public bool Allows(ResourceStack stack)
        {
            if (stack.IsEmpty)
                return false;

            if (AllowedResourceIds.Count > 0 && !AllowedResourceIds.Contains(stack.Definition.Id))
                return false;
            if (AllowedCategories.Count > 0 && !AllowedCategories.Contains(stack.Definition.Category))
                return false;
            if (AllowedQualities.Count > 0 && !AllowedQualities.Contains(stack.Quality))
                return false;
            return true;
        }

        public void AllowCategory(ResourceCategory category)
        {
            AllowedCategories.Add(category);
        }

        public void AllowResource(string resourceId)
        {
            AllowedResourceIds.Add(resourceId);
        }

        public void AllowAllQualities()
        {
            AllowedQualities.Clear();
        }

        public void RestrictQualities(IEnumerable<ResourceQuality> qualities)
        {
            AllowedQualities.Clear();
            if (qualities == null)
                return;
            foreach (var q in qualities)
                AllowedQualities.Add(q);
        }
    }
}
