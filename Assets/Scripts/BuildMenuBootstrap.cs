using UnityEngine;

public static class BuildMenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<BuildMenuController>() == null)
            new GameObject("BuildMenuController").AddComponent<BuildMenuController>();
    }
}
