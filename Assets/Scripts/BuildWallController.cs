using UnityEngine;
using UnityEngine.EventSystems;

using System.Collections.Generic;

public class BuildWallController : MonoBehaviour
{
    private MapGenerator map;
    private TaskManager taskManager;
    private bool placing;

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

        bool pointerOverUI = false;
        if (EventSystem.current != null)
        {
            if (Input.touchSupported && Input.touchCount > 0)
                pointerOverUI = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            else
                pointerOverUI = EventSystem.current.IsPointerOverGameObject();
        }

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
                    QueueBuildTask(cell);
                }
            }
        }
    }

    void QueueBuildTask(Vector2Int cell)
    {
        taskManager.AddTask(new BuildWallTask(cell, 1f, 10, c =>
        {
            map.BuildWallFromFrame(cell.x, cell.y);
        }));
    }
}
