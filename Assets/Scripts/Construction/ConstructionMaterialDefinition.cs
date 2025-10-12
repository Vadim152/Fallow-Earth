using System;
using System.Collections.Generic;
using FallowEarth.ResourcesSystem;

namespace FallowEarth.Construction
{
    /// <summary>
    /// Defines material properties for buildings and the resource costs required to use it.
    /// </summary>
    [Serializable]
    public class ConstructionMaterialDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public float StructuralIntegrity { get; }
        public float ThermalInsulation { get; }
        public IReadOnlyList<ResourceRequest> Cost { get; }

        public ConstructionMaterialDefinition(string id, string displayName, float structuralIntegrity, float thermalInsulation, IEnumerable<ResourceRequest> cost)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Material id cannot be null or empty", nameof(id));
            if (string.IsNullOrEmpty(displayName))
                throw new ArgumentException("Display name cannot be null or empty", nameof(displayName));
            if (structuralIntegrity <= 0)
                throw new ArgumentOutOfRangeException(nameof(structuralIntegrity));

            Id = id;
            DisplayName = displayName;
            StructuralIntegrity = structuralIntegrity;
            ThermalInsulation = thermalInsulation;
            Cost = cost != null ? new List<ResourceRequest>(cost) : new List<ResourceRequest>();
        }
    }
}
