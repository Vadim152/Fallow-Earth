using UnityEngine;

public static class ResourceManagerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<ResourceManager>() == null)
            new GameObject("ResourceManager").AddComponent<ResourceManager>();
    }
}
