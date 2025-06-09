using UnityEngine;

public static class CameraBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.aspect = 9f / 16f;
            if (cam.GetComponent<CameraPanZoomController>() == null)
                cam.gameObject.AddComponent<CameraPanZoomController>();
        }
    }
}
