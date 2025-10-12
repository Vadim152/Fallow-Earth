using System;
using System.Collections.Generic;
using FallowEarth.ResourcesSystem;
using FallowEarth.Saving;
using UnityEngine;

/// <summary>
/// Representation of a stockpile zone supporting resource filtering and logistics prioritisation.
/// </summary>
public class StockpileZone : ISaveable, IMutableSaveId, IDisposable
{
    public HashSet<Vector2Int> cells = new HashSet<Vector2Int>();

    public static List<StockpileZone> AllZones { get; } = new List<StockpileZone>();

    private string saveId;

    public ResourceFilter Filter { get; } = new ResourceFilter();
    public int Priority { get; set; }

    public StockpileZone()
    {
        AllZones.Add(this);
        Filter.AllowAllQualities();
    }

    public static StockpileZone Create(IEnumerable<Vector2Int> newCells, int priority = 0)
    {
        var zone = new StockpileZone();
        zone.cells = new HashSet<Vector2Int>(newCells);
        zone.saveId = Guid.NewGuid().ToString();
        zone.Priority = priority;
        if (WorldDataManager.HasInstance)
            WorldDataManager.Instance.Register(zone);
        ResourceLogisticsManager.RegisterZone(zone);
        return zone;
    }

    public static bool HasAny => AllZones.Count > 0;

    public static Vector2Int GetClosestCell(Vector2 pos)
    {
        float best = float.MaxValue;
        Vector2Int bestCell = Vector2Int.zero;
        foreach (var z in AllZones)
        {
            Vector2Int candidate = z.GetClosestCellTo(pos);
            float d = Vector2.Distance(pos, (Vector2)candidate);
            if (d < best)
            {
                best = d;
                bestCell = candidate;
            }
        }
        return bestCell;
    }

    public Vector2Int GetClosestCellTo(Vector2 pos)
    {
        float best = float.MaxValue;
        Vector2Int bestCell = Vector2Int.zero;
        foreach (var c in cells)
        {
            float d = Vector2.Distance(pos, (Vector2)c);
            if (d < best)
            {
                best = d;
                bestCell = c;
            }
        }
        return bestCell;
    }

    public void Dispose()
    {
        AllZones.Remove(this);
        cells.Clear();
        ResourceLogisticsManager.UnregisterZone(this);
    }

    public string SaveId => saveId;

    public SaveCategory Category => SaveCategory.Zone;

    public Vector3 SavePosition
    {
        get
        {
            if (cells == null || cells.Count == 0)
                return Vector3.zero;
            float sumX = 0f;
            float sumY = 0f;
            foreach (var cell in cells)
            {
                sumX += cell.x;
                sumY += cell.y;
            }
            return new Vector3(sumX / cells.Count, sumY / cells.Count, 0f);
        }
    }

    [Serializable]
    private struct ZoneSaveState
    {
        public List<Vector2Int> cells;
        public int priority;
    }

    public void PopulateSaveData(SaveData saveData)
    {
        saveData.Set("zone", new ZoneSaveState
        {
            cells = cells != null ? new List<Vector2Int>(cells) : new List<Vector2Int>(),
            priority = Priority
        });
    }

    public void LoadFromSaveData(SaveData saveData)
    {
        if (saveData.TryGet("zone", out ZoneSaveState state) && state.cells != null)
        {
            cells = new HashSet<Vector2Int>(state.cells);
            Priority = state.priority;
        }
        else
        {
            cells = new HashSet<Vector2Int>();
            Priority = 0;
        }
        ResourceLogisticsManager.RegisterZone(this);
    }

    public void SetSaveId(string newId)
    {
        if (string.IsNullOrEmpty(newId))
            throw new ArgumentException("Save id cannot be null or empty", nameof(newId));

        if (saveId == newId)
            return;

        string oldId = saveId;
        saveId = newId;

        if (WorldDataManager.HasInstance)
        {
            WorldDataManager.Instance.NotifyIdentifierChanged(this, oldId, newId);
        }
    }
}
