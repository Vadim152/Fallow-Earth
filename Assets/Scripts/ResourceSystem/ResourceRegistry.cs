using System;
using System.Collections.Generic;

namespace FallowEarth.ResourcesSystem
{
    /// <summary>
    /// Global registry for resource definitions. Allows scripts to look up metadata at runtime.
    /// </summary>
    public static class ResourceRegistry
    {
        private static readonly Dictionary<string, ResourceDefinition> definitions = new Dictionary<string, ResourceDefinition>();
        private static bool initialized;

        /// <summary>
        /// Provides a read-only view of all definitions.
        /// </summary>
        public static IReadOnlyCollection<ResourceDefinition> AllDefinitions
        {
            get
            {
                EnsureInitialized();
                return definitions.Values;
            }
        }

        /// <summary>
        /// Ensures the registry contains the core resource definitions used by legacy systems.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;

            // Default timber entry used by chopped trees.
            if (!definitions.ContainsKey(DefaultResourceIds.Wood))
            {
                var def = new ResourceDefinition(DefaultResourceIds.Wood, "Timber", ResourceCategory.RawMaterial, 0.5f);
                Register(def);
            }
            if (!definitions.ContainsKey(DefaultResourceIds.Stone))
            {
                Register(new ResourceDefinition(DefaultResourceIds.Stone, "Stone Block", ResourceCategory.RawMaterial, 1.2f));
            }
            if (!definitions.ContainsKey(DefaultResourceIds.ResearchData))
            {
                Register(new ResourceDefinition(DefaultResourceIds.ResearchData, "Research Data", ResourceCategory.Research, 0.1f));
            }
        }

        public static void Register(ResourceDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            definitions[definition.Id] = definition;
        }

        public static bool TryGet(string id, out ResourceDefinition definition)
        {
            EnsureInitialized();
            return definitions.TryGetValue(id, out definition);
        }

        public static ResourceDefinition GetOrThrow(string id)
        {
            if (!TryGet(id, out var def))
                throw new KeyNotFoundException($"No resource definition registered for id '{id}'");
            return def;
        }
    }

    public static class DefaultResourceIds
    {
        public const string Wood = "wood";
        public const string Stone = "stone";
        public const string ResearchData = "research.data";
    }
}
