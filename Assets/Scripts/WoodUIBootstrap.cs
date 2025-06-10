using UnityEngine;

public static class WoodUIBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<WoodResourceUI>() == null)
            new GameObject("WoodResourceUI").AddComponent<WoodResourceUI>();
    }
}
