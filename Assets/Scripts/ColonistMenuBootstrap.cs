using UnityEngine;

public static class ColonistMenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<ColonistMenuController>() == null)
            new GameObject("ColonistMenuController").AddComponent<ColonistMenuController>();
    }
}
