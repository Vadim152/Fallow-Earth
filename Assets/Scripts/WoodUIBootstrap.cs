using UnityEngine;

public static class WoodUIBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<ResourceLedgerUI>() == null)
            new GameObject("ResourceLedgerUI").AddComponent<ResourceLedgerUI>();
    }
}
