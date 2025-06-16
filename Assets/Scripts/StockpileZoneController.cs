using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles placing stockpile zones using the area selection controller.
/// </summary>
public class StockpileZoneController : MonoBehaviour
{
    private bool placing;

    public bool IsPlacing => placing;
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

        // The AreaSelectionController already created the visible zone
        // overlays when the area was selected. Here we simply register
        // the list of cells as a stockpile zone so colonists know where
        // to haul resources.
        StockpileZone.Create(cells);
        placing = false;
        global::CancelActionUI.Hide();
    }
}
