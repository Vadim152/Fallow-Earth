using System;

namespace FallowEarth.ResourcesSystem
{
    /// <summary>
    /// Represents a quantity of a resource with a specific quality.
    /// </summary>
    [Serializable]
    public struct ResourceStack
    {
        public ResourceDefinition Definition { get; private set; }
        public ResourceQuality Quality { get; private set; }
        public int Amount { get; private set; }

        public ResourceStack(ResourceDefinition definition, ResourceQuality quality, int amount)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Definition = definition;
            Quality = quality;
            Amount = amount;
        }

        public float TotalMass => Amount * Definition.MassPerUnit * Quality.GetMassMultiplier();

        public ResourceStack WithAmount(int newAmount)
        {
            return new ResourceStack(Definition, Quality, newAmount);
        }

        public bool IsEmpty => Amount <= 0 || Definition == null;
    }
}
