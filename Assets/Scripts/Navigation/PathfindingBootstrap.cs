using UnityEngine;

namespace FallowEarth.Navigation
{
    /// <summary>
    /// Ensures a single instance of the <see cref="PathfindingService"/> exists
    /// in the scene so other systems can access it without having to create it
    /// manually.
    /// </summary>
    public static class PathfindingBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (Object.FindObjectOfType<PathfindingService>() != null)
                return;

            var go = new GameObject("PathfindingService");
            go.AddComponent<PathfindingService>();
        }
    }
}
