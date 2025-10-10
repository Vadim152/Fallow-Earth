using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.Saving
{
    /// <summary>
    /// Coordinates asynchronous streaming of world regions around a focus target.
    /// </summary>
    public class RegionLoader : MonoBehaviour
    {
        [Tooltip("How many regions in each direction should remain loaded around the focus target.")]
        public int viewDistance = 1;

        [Tooltip("Seconds between region refresh checks.")]
        public float refreshInterval = 0.5f;

        [Tooltip("Optional transform used as the streaming focus. Defaults to the main camera if not provided.")]
        public Transform focusTarget;

        private readonly HashSet<Vector2Int> activeRegions = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> loadingRegions = new HashSet<Vector2Int>();

        private float refreshTimer;
        private WorldDataManager dataManager;

        private void Awake()
        {
            dataManager = WorldDataManager.Instance;
        }

        private void OnEnable()
        {
            if (dataManager == null)
                dataManager = WorldDataManager.Instance;
            if (dataManager != null)
            {
                dataManager.WorldDataLoaded += OnWorldDataLoaded;
            }
        }

        private void OnDisable()
        {
            if (dataManager != null)
            {
                dataManager.WorldDataLoaded -= OnWorldDataLoaded;
            }
        }

        private void Update()
        {
            if (dataManager == null)
            {
                dataManager = WorldDataManager.Instance;
                if (dataManager == null)
                    return;
            }

            if (focusTarget == null && Camera.main != null)
            {
                focusTarget = Camera.main.transform;
            }

            if (focusTarget == null)
                return;

            refreshTimer += Time.unscaledDeltaTime;
            if (refreshTimer < refreshInterval)
                return;

            refreshTimer = 0f;

            Vector2Int centerRegion = dataManager.WorldToRegion(focusTarget.position);
            EnsureRegions(centerRegion);
        }

        private void OnWorldDataLoaded(WorldSaveData _)
        {
            StopAllCoroutines();
            activeRegions.Clear();
            loadingRegions.Clear();
            refreshTimer = refreshInterval; // trigger immediate refresh
        }

        private void EnsureRegions(Vector2Int center)
        {
            var targetRegions = new HashSet<Vector2Int>();
            for (int dx = -viewDistance; dx <= viewDistance; dx++)
            {
                for (int dy = -viewDistance; dy <= viewDistance; dy++)
                {
                    targetRegions.Add(new Vector2Int(center.x + dx, center.y + dy));
                }
            }

            foreach (var region in targetRegions)
            {
                if (!activeRegions.Contains(region) && !loadingRegions.Contains(region))
                {
                    RequestRegion(region);
                }
            }

            var toUnload = new List<Vector2Int>();
            foreach (var region in activeRegions)
            {
                if (!targetRegions.Contains(region))
                {
                    toUnload.Add(region);
                }
            }

            foreach (var region in toUnload)
            {
                UnloadRegion(region);
            }
        }

        private void RequestRegion(Vector2Int region)
        {
            if (dataManager == null)
                return;

            loadingRegions.Add(region);
            StartCoroutine(LoadRegionRoutine(region));
        }

        private IEnumerator LoadRegionRoutine(Vector2Int region)
        {
            if (dataManager != null)
            {
                yield return dataManager.LoadRegionAsync(region);
            }

            loadingRegions.Remove(region);
            activeRegions.Add(region);
        }

        private void UnloadRegion(Vector2Int region)
        {
            if (!activeRegions.Remove(region))
                return;

            if (dataManager != null)
            {
                dataManager.UnloadRegion(region);
            }
        }
    }
}
