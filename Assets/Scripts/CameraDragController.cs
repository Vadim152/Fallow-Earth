using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraDragController : MonoBehaviour
{
    public float moveSpeed = 1f;

    private Camera cam;
    private Vector3 lastPanPosition;
    private int panFingerId;
    private bool isPanning;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (cam == null)
            return;

        if (Input.touchSupported)
            HandleTouch();
        else
            HandleMouse();
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
    }
}
