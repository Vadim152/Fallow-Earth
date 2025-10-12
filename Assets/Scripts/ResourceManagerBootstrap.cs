using UnityEngine;

public static class ResourceManagerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<ResourceManager>() == null)
            new GameObject("ResourceManager").AddComponent<ResourceManager>();
        if (Object.FindObjectOfType<FallowEarth.ResourcesSystem.ResourceLogisticsManager>() == null)
            new GameObject("ResourceLogisticsManager").AddComponent<FallowEarth.ResourcesSystem.ResourceLogisticsManager>();
    }
}
