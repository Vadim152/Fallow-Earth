using UnityEngine;

public static class BottomLeftMenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<BottomLeftMenuController>() == null)
            new GameObject("BottomLeftMenuController").AddComponent<BottomLeftMenuController>();
    }
}
