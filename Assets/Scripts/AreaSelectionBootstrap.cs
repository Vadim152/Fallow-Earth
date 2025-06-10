using UnityEngine;

public static class AreaSelectionBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<AreaSelectionController>() == null)
            new GameObject("AreaSelectionController").AddComponent<AreaSelectionController>();
    }
}
