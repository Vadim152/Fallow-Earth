using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FallowEarth.ResourcesSystem
{
    /// <summary>
    /// Lightweight graph describing routes between storage zones to support prioritised hauling.
    /// </summary>
    public class ResourceRouteGraph
    {
        private readonly Dictionary<StockpileZone, RouteNode> nodes = new Dictionary<StockpileZone, RouteNode>();

        public void RegisterZone(StockpileZone zone, int priority)
        {
            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (!nodes.TryGetValue(zone, out RouteNode node))
            {
                node = new RouteNode(zone);
                nodes.Add(zone, node);
            }
            node.Priority = priority;
        }

        public void UnregisterZone(StockpileZone zone)
        {
            if (zone == null)
                return;
            nodes.Remove(zone);
        }

        public void ConnectZones(StockpileZone a, StockpileZone b, float cost = 1f)
        {
            if (a == null || b == null)
                return;
            if (!nodes.TryGetValue(a, out var nodeA) || !nodes.TryGetValue(b, out var nodeB))
                return;
            nodeA.Edges[b] = cost;
            nodeB.Edges[a] = cost;
        }

        /// <summary>
        /// Finds the best destination zone for the provided stack considering filters, priorities and distance.
        /// </summary>
        public bool TryFindBestZone(ResourceStack stack, Vector2 origin, out StockpileZone zone)
        {
            zone = null;
            float bestScore = float.MinValue;
            foreach (var node in nodes.Values)
            {
                if (!node.ZoneFilter.Allows(stack))
                    continue;
                Vector2Int closest = node.Zone.GetClosestCellTo(origin);
                float distance = Vector2.Distance(origin, (Vector2)closest);
                float score = node.Priority - distance;
                if (score > bestScore)
                {
                    bestScore = score;
                    zone = node.Zone;
                }
            }
            return zone != null;
        }

        class RouteNode
        {
            public StockpileZone Zone { get; }
            public Dictionary<StockpileZone, float> Edges { get; } = new Dictionary<StockpileZone, float>();
            public int Priority { get; set; }
            public ResourceFilter ZoneFilter => Zone?.Filter;

            public RouteNode(StockpileZone zone)
            {
                Zone = zone;
            }
        }
    }
}
