using System.Collections.Generic;

namespace FallowEarth.ResourcesSystem
{
    public interface IResourceLogisticsService
    {
        ResourceRouteGraph RouteGraph { get; }

        void RegisterZone(StockpileZone zone);

        void UnregisterZone(StockpileZone zone);

        void RegisterItem(ResourceItem item);

        void UnregisterItem(ResourceItem item);

        IReadOnlyCollection<ResourceItem> GetTrackedItems();
    }
}
