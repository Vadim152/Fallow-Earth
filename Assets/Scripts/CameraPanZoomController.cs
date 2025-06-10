using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraPanZoomController : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float zoomSpeed = 0.25f;
    public float minSize = 5f;
    public float maxSize = 20f;

    private Camera cam;
    private MapGenerator map;
    private Vector3 lastPanPosition;
    private int panFingerId;
    private bool isPanning;

    void Start()
    {
        cam = GetComponent<Camera>();
        map = FindObjectOfType<MapGenerator>();
    }

    void Update()
    {
        if (cam == null)
            return;

        if (Input.touchSupported)
            HandleTouch();
        else
            HandleMouse();

        ClampPosition();
    }

    void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                lastPanPosition = cam.ScreenToWorldPoint(t.position);
                panFingerId = t.fingerId;
                isPanning = true;
            }
            else if (t.fingerId == panFingerId && t.phase == TouchPhase.Moved && isPanning)
            {
                Vector3 pos = cam.ScreenToWorldPoint(t.position);
                Vector3 delta = lastPanPosition - pos;
                cam.transform.position += delta * moveSpeed;
            }
            else if (t.fingerId == panFingerId && (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
            {
                isPanning = false;
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 prev0 = t0.position - t0.deltaPosition;
            Vector2 prev1 = t1.position - t1.deltaPosition;
            float prevDist = Vector2.Distance(prev0, prev1);
            float currDist = Vector2.Distance(t0.position, t1.position);
            float delta = currDist - prevDist;

            cam.orthographicSize -= delta * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minSize, maxSize);
        }
    }

    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastPanPosition = cam.ScreenToWorldPoint(Input.mousePosition);
            isPanning = true;
        }
        else if (Input.GetMouseButton(0) && isPanning)
        {
            Vector3 pos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta = lastPanPosition - pos;
            cam.transform.position += delta * moveSpeed;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isPanning = false;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize -= scroll * zoomSpeed * 100f;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minSize, maxSize);
        }
    }

    void ClampPosition()
    {
        if (map == null)
            return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = cam.orthographicSize * cam.aspect;

        float minX = halfWidth;
        float maxX = map.width - halfWidth;
        float minY = halfHeight;
        float maxY = map.height - halfHeight;

        Vector3 pos = cam.transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        cam.transform.position = pos;
    }
}
