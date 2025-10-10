using System;

namespace FallowEarth.Saving
{
    /// <summary>
    /// Categories of saveable content used to organize world data by type.
    /// </summary>
    [Serializable]
    public enum SaveCategory
    {
        Structure,
        Creature,
        Zone
    }
}
