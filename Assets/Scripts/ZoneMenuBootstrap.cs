using UnityEngine;

public static class ZoneMenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<ZoneMenuController>() == null)
            new GameObject("ZoneMenuController").AddComponent<ZoneMenuController>();
    }
}
