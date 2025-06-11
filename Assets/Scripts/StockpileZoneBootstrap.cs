using UnityEngine;

public static class StockpileZoneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<StockpileZoneController>() == null)
            new GameObject("StockpileZoneController").AddComponent<StockpileZoneController>();
    }
}
