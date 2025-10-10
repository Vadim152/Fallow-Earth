using FallowEarth.Saving;
using UnityEngine;

public static class RegionLoaderBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (Object.FindObjectOfType<RegionLoader>() != null)
            return;

        var go = new GameObject("RegionLoader");
        go.AddComponent<RegionLoader>();
    }
}
