using UnityEngine;

public static class TreeChopBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<TreeChopController>() != null)
            return;

        new GameObject("TreeChopController").AddComponent<TreeChopController>();
    }
}
