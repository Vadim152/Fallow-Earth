using FallowEarth.Saving;
using UnityEngine;

public static class WorldDataManagerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (WorldDataManager.HasInstance)
            return;

        var go = new GameObject("WorldDataManager");
        go.AddComponent<WorldDataManager>();
    }
}
