using UnityEngine;
using UnityEngine.EventSystems;

using System.Collections.Generic;

public class BuildWallController : MonoBehaviour
{
    private MapGenerator map;
    private TaskManager taskManager;
    private bool placing;
    private List<Vector2Int> pendingWalls = new List<Vector2Int>();

    void Start()
    {
        map = FindObjectOfType<MapGenerator>();
        taskManager = FindObjectOfType<TaskManager>();
    }

    public void TogglePlacing()
    {
        placing = !placing;
    }

    void Update()
    {
        if (!placing || map == null)
            return;

        bool clicked = false;
        Vector3 screenPos = Vector3.zero;

        if (Input.touchSupported && Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                clicked = true;
                screenPos = t.position;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            clicked = true;
            screenPos = Input.mousePosition;
        }

        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (clicked && !AreaSelectionController.IsSelecting && !pointerOverUI)
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(screenPos);
            int x = Mathf.FloorToInt(world.x);
            int y = Mathf.FloorToInt(world.y);
            if (map.IsPassable(x, y) && !map.HasWallFrame(x, y) && !map.HasWall(x, y))
            {
                map.PlaceWallFrame(x, y);
                if (taskManager != null)
                {
                    var cell = new Vector2Int(x, y);
                    if (ResourceManager.Instance != null && ResourceManager.Instance.Wood > 0)
                    {
                        QueueBuildTask(cell);
                    }
                    else
                    {
                        pendingWalls.Add(cell);
                    }
                }
            }
        }

        TryQueuePendingWalls();
    }

    void QueueBuildTask(Vector2Int cell)
    {
        taskManager.AddTask(new BuildWallTask(cell, 1f, c =>
        {
            if (!ResourceManager.UseWood(1))
            {
                pendingWalls.Add(cell);
                return;
            }
            map.BuildWallFromFrame(cell.x, cell.y);
        }));
    }

    void TryQueuePendingWalls()
    {
        if (pendingWalls.Count == 0 || taskManager == null)
            return;
        if (ResourceManager.Instance == null || ResourceManager.Instance.Wood <= 0)
            return;

        for (int i = pendingWalls.Count - 1; i >= 0; i--)
        {
            var cell = pendingWalls[i];
            QueueBuildTask(cell);
            pendingWalls.RemoveAt(i);
        }
    }
}
