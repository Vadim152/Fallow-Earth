using System;
using UnityEngine;

namespace FallowEarth.ResourcesSystem
{
    /// <summary>
    /// Metadata that describes a resource type.
    /// </summary>
    [Serializable]
    public class ResourceDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private ResourceCategory category;
        [SerializeField] private float massPerUnit = 1f;
        [SerializeField] private Sprite sprite;

        public ResourceDefinition(string id, string displayName, ResourceCategory category, float massPerUnit, Sprite sprite = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Resource id cannot be null or empty", nameof(id));
            if (string.IsNullOrEmpty(displayName))
                throw new ArgumentException("Display name cannot be null or empty", nameof(displayName));
            if (massPerUnit <= 0f)
                throw new ArgumentOutOfRangeException(nameof(massPerUnit), "Mass must be positive");

            this.id = id;
            this.displayName = displayName;
            this.category = category;
            this.massPerUnit = massPerUnit;
            this.sprite = sprite;
        }

        public string Id => id;
        public string DisplayName => displayName;
        public ResourceCategory Category => category;
        public float MassPerUnit => massPerUnit;
        public Sprite Sprite => sprite;

        /// <summary>
        /// Instantiates a resource stack for this definition.
        /// </summary>
        public ResourceStack CreateStack(int amount, ResourceQuality quality = ResourceQuality.Common)
        {
            return new ResourceStack(this, quality, amount);
        }
    }
}
