using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles placing stockpile zones using the area selection controller.
/// </summary>
public class StockpileZoneController : MonoBehaviour
{
    private bool placing;
    private MapGenerator map;

    void Start()
    {
        map = FindObjectOfType<MapGenerator>();
        AreaSelectionController.AreaSelected += OnAreaSelected;
    }

    void OnDestroy()
    {
        AreaSelectionController.AreaSelected -= OnAreaSelected;
    }

    public void TogglePlacing()
    {
        placing = !placing;
    }

    private void OnAreaSelected(List<Vector2Int> cells)
    {
        if (!placing || cells == null || cells.Count == 0)
            return;

        if (map != null)
        {
            map.BeginNewZone();
            foreach (var c in cells)
                map.SetZone(c.x, c.y);
        }

        StockpileZone.Create(cells);
        placing = false;
    }
}
