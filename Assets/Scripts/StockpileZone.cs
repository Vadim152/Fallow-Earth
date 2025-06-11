using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple representation of a stockpile zone.
/// Stores a set of map cells that belong to the zone.
/// </summary>
public class StockpileZone
{
    public HashSet<Vector2Int> cells = new HashSet<Vector2Int>();

    public static List<StockpileZone> AllZones { get; } = new List<StockpileZone>();

    public static StockpileZone Create(IEnumerable<Vector2Int> newCells)
    {
        var zone = new StockpileZone();
        zone.cells = new HashSet<Vector2Int>(newCells);
        AllZones.Add(zone);
        return zone;
    }

    public static bool HasAny => AllZones.Count > 0;

    public static Vector2Int GetClosestCell(Vector2 pos)
    {
        float best = float.MaxValue;
        Vector2Int bestCell = Vector2Int.zero;
        foreach (var z in AllZones)
        {
            foreach (var c in z.cells)
            {
                float d = Vector2.Distance(pos, c);
                if (d < best)
                {
                    best = d;
                    bestCell = c;
                }
            }
        }
        return bestCell;
    }
}
