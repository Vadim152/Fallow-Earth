using UnityEngine;

public static class CameraBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            if (cam.GetComponent<CameraAutoFitter>() == null)
                cam.gameObject.AddComponent<CameraAutoFitter>();
            if (cam.GetComponent<CameraPanZoomController>() == null)
                cam.gameObject.AddComponent<CameraPanZoomController>();
        }
    }
}
