using UnityEngine;

public static class TimeControlBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<TimeControlUI>() == null)
            new GameObject("TimeControlUI").AddComponent<TimeControlUI>();
    }
}
