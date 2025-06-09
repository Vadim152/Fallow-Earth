using UnityEngine;

public static class MapBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<MapGenerator>() != null)
            return;

        var go = new GameObject("MapGenerator");
        go.AddComponent<MapGenerator>();
    }
}
