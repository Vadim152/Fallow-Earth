using System.Collections.Generic;
using FallowEarth.ResourcesSystem;

namespace FallowEarth.Construction
{
    /// <summary>
    /// Collection of known construction materials and recipes.
    /// </summary>
    public static class ConstructionCatalog
    {
        private static readonly Dictionary<string, ConstructionMaterialDefinition> materials = new Dictionary<string, ConstructionMaterialDefinition>();
        private static bool initialized;

        public static IReadOnlyDictionary<string, ConstructionMaterialDefinition> Materials
        {
            get
            {
                EnsureInitialized();
                return materials;
            }
        }

        public static void EnsureInitialized()
        {
            if (initialized)
                return;
            initialized = true;
            ResourceRegistry.EnsureInitialized();

            if (!materials.ContainsKey(DefaultMaterialIds.Timber))
            {
                var timber = new ConstructionMaterialDefinition(
                    DefaultMaterialIds.Timber,
                    "Timber",
                    structuralIntegrity: 60f,
                    thermalInsulation: 20f,
                    cost: new[]
                    {
                        new ResourceRequest(ResourceRegistry.GetOrThrow(DefaultResourceIds.Wood), 25, ResourceQuality.Defective)
                    });
                materials[timber.Id] = timber;
            }

            if (!materials.ContainsKey(DefaultMaterialIds.StoneBlock))
            {
                var stone = new ConstructionMaterialDefinition(
                    DefaultMaterialIds.StoneBlock,
                    "Stone Block",
                    structuralIntegrity: 120f,
                    thermalInsulation: 10f,
                    cost: new[]
                    {
                        new ResourceRequest(ResourceRegistry.GetOrThrow(DefaultResourceIds.Stone), 15, ResourceQuality.Common)
                    });
                materials[stone.Id] = stone;
            }
        }

        public static ConstructionMaterialDefinition GetMaterial(string id)
        {
            EnsureInitialized();
            return materials.TryGetValue(id, out var mat) ? mat : null;
        }
    }

    public static class DefaultMaterialIds
    {
        public const string Timber = "material.timber";
        public const string StoneBlock = "material.stone";
    }
}
