using UnityEngine;

namespace FallowEarth.Saving
{
    /// <summary>
    /// Implemented by components that can be saved and restored by the world data manager.
    /// </summary>
    public interface ISaveable
    {
        string SaveId { get; }
        SaveCategory Category { get; }
        Vector3 SavePosition { get; }
        void PopulateSaveData(SaveData saveData);
        void LoadFromSaveData(SaveData saveData);
    }
}
