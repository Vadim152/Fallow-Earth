using UnityEngine;

public static class ColonistMassManagementBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<ColonistMassManagementUI>() == null)
            new GameObject("ColonistMassManagementUI").AddComponent<ColonistMassManagementUI>();
    }
}
