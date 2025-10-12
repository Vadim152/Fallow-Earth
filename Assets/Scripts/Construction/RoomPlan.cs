using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.Construction
{
    /// <summary>
    /// Describes a planned room layout including cells, level and desired usage.
    /// </summary>
    public class RoomPlan
    {
        public string Name { get; }
        public int Level { get; }
        public HashSet<Vector2Int> Cells { get; } = new HashSet<Vector2Int>();
        public Dictionary<string, int> RequiredResources { get; } = new Dictionary<string, int>();

        public RoomPlan(string name, int level, IEnumerable<Vector2Int> cells)
        {
            Name = name;
            Level = level;
            if (cells != null)
            {
                foreach (var cell in cells)
                    Cells.Add(cell);
            }
        }

        public void RequireResource(string resourceId, int amount)
        {
            if (RequiredResources.ContainsKey(resourceId))
                RequiredResources[resourceId] += amount;
            else
                RequiredResources[resourceId] = amount;
        }
    }
}
