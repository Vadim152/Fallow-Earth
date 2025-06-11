using UnityEngine;

public static class TimeOfDayUIBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<TimeOfDayUI>() == null)
            new GameObject("TimeOfDayUI").AddComponent<TimeOfDayUI>();
    }
}
