using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAutoFitter : MonoBehaviour
{
    private Camera cam;
    private int lastWidth;
    private int lastHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        UpdateCamera();
    }

    void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            UpdateCamera();
        }
    }

    void UpdateCamera()
    {
        if (cam == null)
            return;

        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.aspect = (float)Screen.width / Screen.height;
    }
}
