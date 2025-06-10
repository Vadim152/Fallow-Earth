using UnityEngine;

public static class ColonistInfoCardBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<ColonistInfoCard>() == null)
            new GameObject("ColonistInfoCard").AddComponent<ColonistInfoCard>();
    }
}
