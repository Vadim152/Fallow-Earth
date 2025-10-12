using UnityEngine;

namespace FallowEarth.Construction
{
    public static class ConstructionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            ConstructionPlanner.EnsureInstance();
            FallowEarth.Research.ResearchManager.EnsureInstance();
        }
    }
}
