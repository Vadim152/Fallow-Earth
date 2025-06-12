using UnityEngine;

public static class ActionButtonsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<ActionButtonsUI>() == null)
            new GameObject("ActionButtonsUI").AddComponent<ActionButtonsUI>();
    }
}
