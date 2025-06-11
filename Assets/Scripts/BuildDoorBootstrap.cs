using UnityEngine;

public static class BuildDoorBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<BuildDoorController>() == null)
            new GameObject("BuildDoorController").AddComponent<BuildDoorController>();
    }
}
