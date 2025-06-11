using UnityEngine;

public static class DayNightCycleBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<DayNightCycle>() == null)
            new GameObject("DayNightCycle").AddComponent<DayNightCycle>();
    }
}
