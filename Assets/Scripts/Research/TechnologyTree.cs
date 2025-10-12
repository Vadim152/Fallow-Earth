using System.Collections.Generic;
using FallowEarth.ResourcesSystem;

namespace FallowEarth.Research
{
    /// <summary>
    /// Holds the default technology definitions used by the colony.
    /// </summary>
    public static class TechnologyTree
    {
        private static readonly Dictionary<string, TechnologyDefinition> technologies = new Dictionary<string, TechnologyDefinition>();
        private static bool initialized;

        public static IReadOnlyDictionary<string, TechnologyDefinition> Technologies
        {
            get
            {
                EnsureInitialized();
                return technologies;
            }
        }

        public static void EnsureInitialized()
        {
            if (initialized)
                return;
            initialized = true;
            ResourceRegistry.EnsureInitialized();

            technologies[TechIds.StructuralEngineering] = new TechnologyDefinition(
                TechIds.StructuralEngineering,
                "Structural Engineering",
                researchCost: 300,
                prerequisites: null,
                unlockCosts: new[]
                {
                    new ResourceRequest(ResourceRegistry.GetOrThrow(DefaultResourceIds.ResearchData), 50)
                });

            technologies[TechIds.AdvancedCarpentry] = new TechnologyDefinition(
                TechIds.AdvancedCarpentry,
                "Advanced Carpentry",
                researchCost: 200,
                prerequisites: new[] { TechIds.StructuralEngineering },
                unlockCosts: new[]
                {
                    new ResourceRequest(ResourceRegistry.GetOrThrow(DefaultResourceIds.Wood), 100, ResourceQuality.Common)
                });
        }

        public static TechnologyDefinition Get(string id)
        {
            EnsureInitialized();
            return technologies.TryGetValue(id, out var tech) ? tech : null;
        }
    }

    public static class TechIds
    {
        public const string StructuralEngineering = "tech.structural";
        public const string AdvancedCarpentry = "tech.carpentry";
    }
}
