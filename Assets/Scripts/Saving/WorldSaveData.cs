using System;
using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.Saving
{
    /// <summary>
    /// Contains serialized data for the entire game world including region chunk information.
    /// </summary>
    [Serializable]
    public class WorldSaveData
    {
        public string worldId = Guid.NewGuid().ToString();
        public List<RegionSaveData> regions = new List<RegionSaveData>();

        public RegionSaveData GetOrCreateRegion(Vector2Int coord)
        {
            foreach (var region in regions)
            {
                if (region.coordinate == coord)
                    return region;
            }

            var newRegion = new RegionSaveData { coordinate = coord };
            regions.Add(newRegion);
            return newRegion;
        }

        public RegionSaveData GetRegion(Vector2Int coord)
        {
            foreach (var region in regions)
            {
                if (region.coordinate == coord)
                    return region;
            }

            return null;
        }
    }

    [Serializable]
    public class RegionSaveData
    {
        public Vector2Int coordinate;
        public List<SaveRecord> structures = new List<SaveRecord>();
        public List<SaveRecord> creatures = new List<SaveRecord>();
        public List<SaveRecord> zones = new List<SaveRecord>();
    }

    [Serializable]
    public class SaveRecord
    {
        public string id;
        public string type;
        public SaveCategory category;
        public Vector3 position;
        public SaveData data = new SaveData();
    }

    [Serializable]
    public class SaveSlotData
    {
        public string slotId;
        public string savedAtIsoUtc;
        public WorldSaveData world;
    }

    [Serializable]
    public class SaveSlotInfo
    {
        public string slotId;
        public DateTime savedAtUtc;

        public override string ToString()
        {
            return $"{slotId} ({savedAtUtc:O})";
        }
    }
}
