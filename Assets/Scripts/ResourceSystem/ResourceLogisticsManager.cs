using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.ResourcesSystem
{
    /// <summary>
    /// Coordinates hauling routes between loose resources and storage zones.
    /// </summary>
    public class ResourceLogisticsManager : MonoBehaviour, IResourceLogisticsService
    {
        private readonly ResourceRouteGraph routeGraph = new ResourceRouteGraph();
        private readonly HashSet<ResourceItem> trackedItems = new HashSet<ResourceItem>();
        private readonly List<ResourceItem> snapshotBuffer = new List<ResourceItem>();

        public ResourceRouteGraph RouteGraph => routeGraph;

        public void RegisterZone(StockpileZone zone)
        {
            if (zone == null)
                return;
            routeGraph.RegisterZone(zone, zone.Priority);
        }

        public void UnregisterZone(StockpileZone zone)
        {
            if (zone == null)
                return;
            routeGraph.UnregisterZone(zone);
        }

        public void RegisterItem(ResourceItem item)
        {
            if (item == null)
                return;
            trackedItems.Add(item);
        }

        public void UnregisterItem(ResourceItem item)
        {
            if (item == null)
                return;
            trackedItems.Remove(item);
        }

        public IReadOnlyCollection<ResourceItem> GetTrackedItems()
        {
            snapshotBuffer.Clear();
            snapshotBuffer.AddRange(trackedItems);
            return snapshotBuffer;
        }
    }
}
