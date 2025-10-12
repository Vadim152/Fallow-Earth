using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.ResourcesSystem
{
    /// <summary>
    /// Coordinates hauling routes between loose resources and storage zones.
    /// </summary>
    public class ResourceLogisticsManager : MonoBehaviour
    {
        public static ResourceLogisticsManager Instance { get; private set; }

        private readonly ResourceRouteGraph routeGraph = new ResourceRouteGraph();
        private readonly HashSet<ResourceItem> trackedItems = new HashSet<ResourceItem>();
        private readonly List<ResourceItem> snapshotBuffer = new List<ResourceItem>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static ResourceRouteGraph RouteGraph => Instance?.routeGraph;

        public static void RegisterZone(StockpileZone zone)
        {
            if (Instance == null || zone == null)
                return;
            Instance.routeGraph.RegisterZone(zone, zone.Priority);
        }

        public static void UnregisterZone(StockpileZone zone)
        {
            if (Instance == null || zone == null)
                return;
            Instance.routeGraph.UnregisterZone(zone);
        }

        public static void RegisterItem(ResourceItem item)
        {
            if (Instance == null || item == null)
                return;
            Instance.trackedItems.Add(item);
        }

        public static void UnregisterItem(ResourceItem item)
        {
            if (Instance == null || item == null)
                return;
            Instance.trackedItems.Remove(item);
        }

        public static IReadOnlyCollection<ResourceItem> GetTrackedItems()
        {
            if (Instance == null)
                return System.Array.Empty<ResourceItem>();
            Instance.snapshotBuffer.Clear();
            Instance.snapshotBuffer.AddRange(Instance.trackedItems);
            return Instance.snapshotBuffer;
        }
    }
}
