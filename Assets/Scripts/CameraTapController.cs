using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraTapController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Camera cam;
    private Vector3 targetPos;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam.GetComponent<CameraAutoFitter>() == null)
            cam.gameObject.AddComponent<CameraAutoFitter>();
        targetPos = cam.transform.position;
    }

    void Update()
    {
        bool tapped = false;
        Vector3 screenPos = Vector3.zero;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            tapped = true;
            screenPos = Input.GetTouch(0).position;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            tapped = true;
            screenPos = Input.mousePosition;
        }

        if (tapped)
        {
            Vector3 world = cam.ScreenToWorldPoint(screenPos);
            world.z = targetPos.z;
            targetPos = world;
        }

        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, moveSpeed * Time.deltaTime);
    }
}
