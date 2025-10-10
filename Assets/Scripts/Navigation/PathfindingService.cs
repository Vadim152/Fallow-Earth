using System;
using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.Navigation
{
    /// <summary>
    /// Centralised navigation system that exposes pathfinding (A* and Dijkstra),
    /// tile reservations and hazard information to the rest of the simulation.
    /// The service keeps track of a grid representing the map so it can react to
    /// dynamic obstacles such as freshly placed structures or temporary
    /// blockages.
    /// </summary>
    public class PathfindingService : MonoBehaviour
    {
        public enum PathfindingAlgorithm
        {
            AStar,
            Dijkstra
        }

        public enum HazardType
        {
            None,
            Fire,
            Poison
        }

        private class Node
        {
            public Vector2Int Position;
            public float G;
            public float H;
            public Node Parent;
            public float F => G + H;
        }

        private struct HazardInfo
        {
            public HazardType Type;
            public float Intensity;
        }

        private struct CellData
        {
            public bool Walkable;
            public float BaseCost;
        }

        private class Reservation
        {
            public object Owner;
        }

        private static readonly Dictionary<HazardType, float> HazardPenalties = new Dictionary<HazardType, float>
        {
            { HazardType.None, 0f },
            { HazardType.Fire, 50f },
            { HazardType.Poison, 15f }
        };

        private static PathfindingService instance;

        private CellData[,] grid;
        private readonly Dictionary<Vector2Int, HazardInfo> hazardMap = new Dictionary<Vector2Int, HazardInfo>();
        private readonly Dictionary<Vector2Int, Reservation> reservations = new Dictionary<Vector2Int, Reservation>();
        private readonly Dictionary<object, HashSet<Vector2Int>> reservationsByOwner = new Dictionary<object, HashSet<Vector2Int>>();
        private readonly HashSet<Vector2Int> dynamicObstacles = new HashSet<Vector2Int>();

        private int width;
        private int height;

        public static PathfindingService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<PathfindingService>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public void Initialize(int newWidth, int newHeight, bool[,] walkable)
        {
            width = newWidth;
            height = newHeight;
            grid = new CellData[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool canWalk = walkable != null && x < walkable.GetLength(0) && y < walkable.GetLength(1) && walkable[x, y];
                    grid[x, y] = new CellData
                    {
                        Walkable = canWalk,
                        BaseCost = 1f
                    };
                }
            }

            hazardMap.Clear();
            dynamicObstacles.Clear();
            ClearAllReservations();
        }

        public bool InBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
        }

        public bool IsWalkable(Vector2Int cell)
        {
            if (!InBounds(cell) || grid == null)
                return false;
            return grid[cell.x, cell.y].Walkable && !dynamicObstacles.Contains(cell);
        }

        public void SetWalkable(Vector2Int cell, bool walkable)
        {
            if (!InBounds(cell) || grid == null)
                return;
            var data = grid[cell.x, cell.y];
            data.Walkable = walkable;
            grid[cell.x, cell.y] = data;
            if (!walkable)
                ClearHazard(cell);
        }

        public void SetDynamicObstacle(Vector2Int cell, bool blocked)
        {
            if (!InBounds(cell))
                return;
            if (blocked)
                dynamicObstacles.Add(cell);
            else
                dynamicObstacles.Remove(cell);
        }

        public void MarkHazard(Vector2Int cell, HazardType type, float intensity = 1f)
        {
            if (!InBounds(cell))
                return;
            if (type == HazardType.None || intensity <= 0f)
            {
                ClearHazard(cell);
                return;
            }

            hazardMap[cell] = new HazardInfo
            {
                Type = type,
                Intensity = Mathf.Max(0.01f, intensity)
            };
        }

        public void ClearHazard(Vector2Int cell)
        {
            hazardMap.Remove(cell);
        }

        public bool TryReserve(Vector2Int cell, object owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (!InBounds(cell))
                return false;

            if (reservations.TryGetValue(cell, out var existing))
            {
                if (!ReferenceEquals(existing.Owner, owner))
                    return false;
                return true;
            }

            if (!IsWalkable(cell))
                return false;

            var reservation = new Reservation { Owner = owner };
            reservations[cell] = reservation;
            if (!reservationsByOwner.TryGetValue(owner, out var set))
            {
                set = new HashSet<Vector2Int>();
                reservationsByOwner[owner] = set;
            }
            set.Add(cell);
            return true;
        }

        public void ReleaseReservation(Vector2Int cell, object owner)
        {
            if (owner == null)
                return;
            if (reservations.TryGetValue(cell, out var existing) && ReferenceEquals(existing.Owner, owner))
            {
                reservations.Remove(cell);
                if (reservationsByOwner.TryGetValue(owner, out var set))
                {
                    set.Remove(cell);
                    if (set.Count == 0)
                        reservationsByOwner.Remove(owner);
                }
            }
        }

        public void ReleaseAllReservations(object owner)
        {
            if (owner == null)
                return;
            if (!reservationsByOwner.TryGetValue(owner, out var cells))
                return;

            foreach (var cell in cells)
            {
                reservations.Remove(cell);
            }
            reservationsByOwner.Remove(owner);
        }

        private void ClearAllReservations()
        {
            reservations.Clear();
            reservationsByOwner.Clear();
        }

        public bool IsReserved(Vector2Int cell)
        {
            return reservations.ContainsKey(cell);
        }

        private float GetTraversalCost(Vector2Int cell, bool avoidHazards)
        {
            float cost = 1f;
            if (grid != null && InBounds(cell))
                cost = grid[cell.x, cell.y].BaseCost;

            if (hazardMap.TryGetValue(cell, out var hazard))
            {
                float penalty = HazardPenalties.TryGetValue(hazard.Type, out float basePenalty) ? basePenalty : 0f;
                penalty *= Mathf.Max(0.01f, hazard.Intensity);
                if (avoidHazards && penalty >= 50f)
                {
                    // treat high danger tiles as blocked when avoidance requested
                    return float.PositiveInfinity;
                }
                cost += penalty;
            }

            return cost;
        }

        private float Heuristic(Vector2Int a, Vector2Int b, PathfindingAlgorithm algorithm)
        {
            if (algorithm == PathfindingAlgorithm.Dijkstra)
                return 0f;
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private bool CanTraverse(Vector2Int cell, object requester)
        {
            if (!IsWalkable(cell))
                return false;

            if (reservations.TryGetValue(cell, out var reservation))
            {
                if (!ReferenceEquals(reservation.Owner, requester))
                    return false;
            }

            return true;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, object requester = null,
            PathfindingAlgorithm algorithm = PathfindingAlgorithm.AStar, bool avoidHazards = true)
        {
            if (grid == null || !InBounds(start) || !InBounds(goal))
                return null;

            if (start == goal)
                return new List<Vector2Int> { start };

            var open = new List<Node>();
            var nodes = new Dictionary<Vector2Int, Node>();
            var closed = new HashSet<Vector2Int>();

            Node startNode = new Node
            {
                Position = start,
                G = 0f,
                H = Heuristic(start, goal, algorithm),
                Parent = null
            };

            open.Add(startNode);
            nodes[start] = startNode;

            Vector2Int[] neighbours =
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            while (open.Count > 0)
            {
                Node current = open[0];
                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].F < current.F)
                        current = open[i];
                }

                open.Remove(current);
                closed.Add(current.Position);

                if (current.Position == goal)
                    return ReconstructPath(current);

                foreach (var dir in neighbours)
                {
                    Vector2Int next = current.Position + dir;
                    if (!InBounds(next) || closed.Contains(next))
                        continue;

                    bool traversable = CanTraverse(next, requester) || next == goal;
                    if (!traversable)
                        continue;

                    float traversalCost = GetTraversalCost(next, avoidHazards);
                    if (float.IsPositiveInfinity(traversalCost))
                        continue;

                    float tentativeG = current.G + traversalCost;

                    if (!nodes.TryGetValue(next, out var node))
                    {
                        node = new Node
                        {
                            Position = next,
                            G = tentativeG,
                            H = Heuristic(next, goal, algorithm),
                            Parent = current
                        };
                        nodes[next] = node;
                        open.Add(node);
                    }
                    else if (tentativeG < node.G)
                    {
                        node.G = tentativeG;
                        node.Parent = current;
                    }
                }
            }

            return null;
        }

        public List<Vector2Int> FindPathDijkstra(Vector2Int start, Vector2Int goal, object requester = null,
            bool avoidHazards = true)
        {
            return FindPath(start, goal, requester, PathfindingAlgorithm.Dijkstra, avoidHazards);
        }

        private List<Vector2Int> ReconstructPath(Node node)
        {
            var path = new List<Vector2Int>();
            while (node != null)
            {
                path.Add(node.Position);
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }
    }
}
