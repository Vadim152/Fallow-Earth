using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.Construction
{
    /// <summary>
    /// Centralised manager for construction projects including multi-level support and room planning.
    /// </summary>
    public class ConstructionPlanner : MonoBehaviour
    {
        public static ConstructionPlanner Instance { get; private set; }

        private readonly Dictionary<Vector3Int, ConstructionProject> activeProjects = new Dictionary<Vector3Int, ConstructionProject>();
        private readonly List<RoomPlan> plannedRooms = new List<RoomPlan>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ConstructionCatalog.EnsureInitialized();
        }

        public static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject("ConstructionPlanner").AddComponent<ConstructionPlanner>();
            }
        }

        public ConstructionProject GetOrCreateProject(Vector2Int cell, int level, ConstructionType type, string materialId, float requiredWork)
        {
            var key = new Vector3Int(cell.x, cell.y, level);
            if (activeProjects.TryGetValue(key, out var project))
                return project;

            var material = ConstructionCatalog.GetMaterial(materialId);
            project = new ConstructionProject(cell, level, type, material, requiredWork);
            activeProjects[key] = project;
            return project;
        }

        public void CompleteProject(ConstructionProject project)
        {
            if (project == null)
                return;
            var key = new Vector3Int(project.Cell.x, project.Cell.y, project.Level);
            activeProjects.Remove(key);
        }

        public IEnumerable<ConstructionProject> ActiveProjects => activeProjects.Values;

        public void RegisterRoomPlan(RoomPlan plan)
        {
            if (plan == null)
                return;
            plannedRooms.Add(plan);
        }

        public IReadOnlyList<RoomPlan> PlannedRooms => plannedRooms;

        public RoomPlan CreateRoomPlan(string name, int level, IEnumerable<Vector2Int> cells, Dictionary<string, int> requiredResources = null)
        {
            var plan = new RoomPlan(name, level, cells);
            if (requiredResources != null)
            {
                foreach (var pair in requiredResources)
                {
                    plan.RequireResource(pair.Key, pair.Value);
                }
            }
            plannedRooms.Add(plan);
            return plan;
        }
    }
}
