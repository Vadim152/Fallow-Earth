using UnityEngine;
using UnityEngine.EventSystems;

public class BuildWallController : MonoBehaviour
{
    private MapGenerator map;
    private bool placing;

    void Start()
    {
        map = FindObjectOfType<MapGenerator>();
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

        if (clicked && !AreaSelectionController.IsSelecting && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(screenPos);
            int x = Mathf.FloorToInt(world.x);
            int y = Mathf.FloorToInt(world.y);
            if (map.IsPassable(x, y))
            {
                map.PlaceWall(x, y);
            }
        }
    }
}
