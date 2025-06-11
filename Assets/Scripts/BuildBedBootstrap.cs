using UnityEngine;

public static class BuildBedBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<BuildBedController>() == null)
            new GameObject("BuildBedController").AddComponent<BuildBedController>();
    }
}
