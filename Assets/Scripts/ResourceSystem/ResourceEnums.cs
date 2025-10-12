using System;

namespace FallowEarth.ResourcesSystem
{
    /// <summary>
    /// High level grouping for resources. Useful for storage filters and research requirements.
    /// </summary>
    public enum ResourceCategory
    {
        RawMaterial,
        Manufactured,
        Food,
        Luxury,
        Research,
    }

    /// <summary>
    /// Quality grade for items. Higher quality typically yields better construction stats and higher value.
    /// </summary>
    public enum ResourceQuality
    {
        Defective = 0,
        Common = 1,
        Fine = 2,
        Masterwork = 3,
        Legendary = 4
    }

    public static class ResourceQualityExtensions
    {
        /// <summary>
        /// Returns a multiplier representing how the quality affects the base value of the resource.
        /// </summary>
        public static float GetMassMultiplier(this ResourceQuality quality)
        {
            switch (quality)
            {
                case ResourceQuality.Defective:
                    return 0.9f;
                case ResourceQuality.Fine:
                    return 1.05f;
                case ResourceQuality.Masterwork:
                    return 1.1f;
                case ResourceQuality.Legendary:
                    return 1.2f;
                default:
                    return 1f;
            }
        }

        /// <summary>
        /// Human readable label for UI purposes.
        /// </summary>
        public static string GetDisplayName(this ResourceQuality quality)
        {
            switch (quality)
            {
                case ResourceQuality.Defective:
                    return "Defective";
                case ResourceQuality.Common:
                    return "Common";
                case ResourceQuality.Fine:
                    return "Fine";
                case ResourceQuality.Masterwork:
                    return "Masterwork";
                case ResourceQuality.Legendary:
                    return "Legendary";
                default:
                    return quality.ToString();
            }
        }
    }
}
