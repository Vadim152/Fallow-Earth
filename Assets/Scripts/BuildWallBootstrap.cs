using UnityEngine;

public static class BuildWallBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<BuildWallController>() == null)
            new GameObject("BuildWallController").AddComponent<BuildWallController>();
    }
}
