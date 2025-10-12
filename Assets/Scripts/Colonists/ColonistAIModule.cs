using System;
using System.Collections.Generic;
using FallowEarth.Infrastructure;
using FallowEarth.Navigation;
using FallowEarth.ResourcesSystem;
using UnityEngine;

[RequireComponent(typeof(Colonist))]
public class ColonistAIModule : MonoBehaviour
{
    private Colonist owner;
    private ColonistNeeds needsModule;
    private ColonistSocial socialModule;
    private Rigidbody2D body;

    private float baseMoveSpeed = 10f;
    private Task currentTask;
    private TaskManager taskManager;
    private MapGenerator map;
    private PathfindingService pathfinding;
    private List<Vector2Int> path;
    private int pathIndex;
    private bool wandering;
    private float actionTimer;
    private readonly List<Vector2Int> reservedPath = new List<Vector2Int>();
    private readonly Dictionary<Vector2Int, int> reservedPathSteps = new Dictionary<Vector2Int, int>();
    private IResourceManager resourceManager;
    private IResourceLogisticsService resourceLogistics;

    public Task CurrentTask => currentTask;
    public bool IsBusy => currentTask != null;
    public bool IsWandering => wandering;
    public bool HasPath => path != null && pathIndex < path.Count;

    void Awake()
    {
        EnsureOwnerModules();
        taskManager = FindObjectOfType<TaskManager>();
        map = FindObjectOfType<MapGenerator>();
        pathfinding = PathfindingService.Instance;
        GameServices.TryResolve(out resourceManager);
        GameServices.TryResolve(out resourceLogistics);
    }

    public void Configure(Rigidbody2D rb, float moveSpeed)
    {
        body = rb;
        baseMoveSpeed = moveSpeed;
    }

    void Start()
    {
        if (taskManager == null)
            taskManager = FindObjectOfType<TaskManager>();
        if (map == null)
            map = FindObjectOfType<MapGenerator>();
    }

    void OnDestroy()
    {
        ReleaseReservedPath();
    }

    public void RefreshWorldReferences()
    {
        EnsureOwnerModules();
        if (taskManager == null)
            taskManager = FindObjectOfType<TaskManager>();
        if (map == null)
            map = FindObjectOfType<MapGenerator>();
    }

    public void CancelTasks()
    {
        if (currentTask != null)
            ReleaseCurrentTaskReservation();

        currentTask = null;
        ClearPath();
        wandering = false;
        owner.activity = "Idle";
    }

    public bool TryAssignTask(Task task)
    {
        if (task == null)
            return false;

        SetTask(task);
        return ReferenceEquals(currentTask, task);
    }

    public void SetTask(Task task)
    {
        if (currentTask == task)
            return;

        if (currentTask != null)
            ReleaseCurrentTaskReservation();

        ClearPath();

        currentTask = task;
        if (currentTask == null)
            return;

        AssignPathForCurrentTask();
    }

    public void Tick(float deltaTime, bool canAcquireTasks)
    {
        RefreshWorldReferences();

        if (canAcquireTasks && currentTask == null)
        {
            currentTask = taskManager != null ? taskManager.GetNextTask(owner) : null;
            if (currentTask != null)
                AssignPathForCurrentTask();
            else if ((path == null || pathIndex >= path.Count) && !EvaluateNeeds())
                StartWander();
        }

        if (currentTask is BuildWallTask buildTask)
        {
            HandleBuildWallTask(buildTask, deltaTime);
            return;
        }
        if (currentTask is HaulLogTask haulTask)
        {
            HandleHaulLogTask(haulTask, deltaTime);
            return;
        }
        if (currentTask is RestTask restTask)
        {
            HandleRestTask(restTask, deltaTime);
            return;
        }

        if (path == null || pathIndex >= path.Count)
        {
            if (path != null && pathIndex >= path.Count && currentTask == null)
                ClearPath();

            if (!wandering && currentTask != null)
            {
                if (currentTask is TimedTask timed)
                {
                    if (actionTimer <= 0f)
                        actionTimer = timed.duration;
                    if (currentTask is EatBerryTask)
                        owner.activity = "Eating";
                    else if (currentTask is SocializeTask)
                        owner.activity = "Talking";
                    else if (currentTask is RestOnGroundTask groundRest)
                    {
                        owner.activity = "Resting";
                        if (groundRest.duration > 0f)
                        {
                            float fatiguePerSecond = groundRest.fatigueRecoveryAmount / groundRest.duration;
                            float stressPerSecond = groundRest.stressReliefAmount / groundRest.duration;
                            if (fatiguePerSecond > 0f)
                                owner.SatisfyNeed(NeedType.Rest, fatiguePerSecond * deltaTime);
                            if (stressPerSecond > 0f)
                                owner.SatisfyNeed(NeedType.Stress, stressPerSecond * deltaTime);
                        }
                    }
                    actionTimer -= deltaTime;
                    if (actionTimer <= 0f)
                    {
                        actionTimer = 0f;
                        currentTask.Complete(owner);
                        currentTask = null;
                        owner.activity = "Idle";
                    }
                    return;
                }
                else
                {
                    currentTask.Complete(owner);
                    currentTask = null;
                    owner.activity = "Idle";
                }
            }

            if (wandering)
                wandering = false;
            return;
        }

        MoveAlongPath(deltaTime);
    }

    public void MoveAlongPath(float deltaTime)
    {
        if (path == null)
            return;

        if (pathIndex >= path.Count)
        {
            wandering = false;
            ReleaseReservedPath();
            return;
        }

        Vector2 targetPos = path[pathIndex];
        Vector2 dir = targetPos - body.position;
        float dist = dir.magnitude;
        if (dist < 0.05f)
        {
            body.position = targetPos;
            pathIndex++;
            ReleaseVisitedReservations();
        }
        else
        {
            float speedMod = 1f;
            float hunger = needsModule != null ? needsModule.GetValue(NeedType.Hunger) : 0f;
            float fatigue = needsModule != null ? needsModule.GetValue(NeedType.Rest) : 0f;
            speedMod -= Mathf.Clamp01((hunger + fatigue) * 0.5f);
            if (WeatherSystem.Instance != null)
                speedMod *= WeatherSystem.Instance.GetMoveSpeedMultiplier();
            body.MovePosition(body.position + dir.normalized * baseMoveSpeed * speedMod * deltaTime);
        }
    }

    public void StartWander()
    {
        EnsureOwnerModules();
        if (owner == null || map == null)
            return;

        Vector2Int start = Vector2Int.FloorToInt(owner.transform.position);
        for (int i = 0; i < 20; i++)
        {
            int x = UnityEngine.Random.Range(0, map.width);
            int y = UnityEngine.Random.Range(0, map.height);
            if (map.IsPassable(x, y))
            {
                path = FindPath(start, new Vector2Int(x, y));
                if (path != null && path.Count > 0)
                {
                    pathIndex = 0;
                    wandering = true;
                    owner.activity = "Wandering";
                    return;
                }
            }
        }
    }

    public void ClearPath()
    {
        ReleaseReservedPath();
        path = null;
        pathIndex = 0;
    }

    public bool EvaluateNeeds()
    {
        EnsureOwnerModules();
        var options = new List<NeedAction>();
        var needs = needsModule;
        if (needs == null)
            return false;

        float hungerNeed = needs.GetValue(NeedType.Hunger);
        if (hungerNeed > 0.45f)
        {
            options.Add(new NeedAction(hungerNeed, () =>
            {
                if (map != null && map.TryFindClosestBerryCell(owner.transform.position, out var berry))
                {
                    var eatTask = new EatBerryTask(berry, 2.5f, c =>
                    {
                        map.RemoveBerries(berry.x, berry.y);
                        c.SatisfyNeed(NeedType.Hunger, 0.6f);
                        c.mood = Mathf.Clamp01(c.mood + 0.05f);
                    });
                    if (TryAssignTask(eatTask))
                        return true;
                }
                return false;
            }));
        }

        float restNeed = needs.GetValue(NeedType.Rest);
        if (restNeed > 0.55f)
        {
            options.Add(new NeedAction(restNeed, () =>
            {
                float restDuration = Mathf.Lerp(5f, 9f, Mathf.Clamp01(restNeed));
                if (TryBeginBedRest(restDuration))
                    return true;

                float fatigueRecovery = Mathf.Lerp(0.25f, 0.45f, Mathf.Clamp01(restNeed));
                float stressRelief = Mathf.Lerp(0.1f, 0.2f, Mathf.Clamp01(needs.GetValue(NeedType.Stress)));
                return BeginGroundRest(restDuration, fatigueRecovery, stressRelief);
            }));
        }

        float socialNeed = needs.GetValue(NeedType.Social);
        if (socialNeed > 0.5f)
        {
            options.Add(new NeedAction(socialNeed, () => socialModule != null && socialModule.TryStartSocialize()));
        }

        float stressNeed = needs.GetValue(NeedType.Stress);
        if (stressNeed > 0.7f)
        {
            options.Add(new NeedAction(stressNeed, () =>
            {
                if (socialModule != null && socialModule.TryStartSocialize())
                    return true;

                float calmDuration = Mathf.Lerp(4f, 7f, Mathf.Clamp01(stressNeed));
                if (TryBeginBedRest(Mathf.Max(4f, calmDuration - 1f)))
                    return true;

                float fatigueRecovery = Mathf.Lerp(0.15f, 0.3f, Mathf.Clamp01(restNeed));
                float stressRelief = Mathf.Lerp(0.2f, 0.35f, Mathf.Clamp01(stressNeed));
                return BeginGroundRest(calmDuration, fatigueRecovery, stressRelief);
            }));
        }

        float recreationNeed = needs.GetValue(NeedType.Recreation);
        if (recreationNeed > 0.6f)
        {
            options.Add(new NeedAction(recreationNeed, () =>
            {
                if (socialModule != null && socialModule.TryStartSocialize())
                    return true;
                float relaxDuration = Mathf.Lerp(3f, 6f, Mathf.Clamp01(recreationNeed));
                return BeginGroundRest(relaxDuration, 0.15f, Mathf.Lerp(0.1f, 0.25f, recreationNeed));
            }));
        }

        float comfortNeed = needs.GetValue(NeedType.Comfort);
        if (comfortNeed > 0.5f)
        {
            options.Add(new NeedAction(comfortNeed, () =>
            {
                float restDuration = Mathf.Lerp(3f, 6f, Mathf.Clamp01(comfortNeed));
                if (TryBeginBedRest(restDuration))
                    return true;
                return BeginGroundRest(restDuration, 0.1f, 0.1f);
            }));
        }

        float medicalNeed = Mathf.Max(needs.GetValue(NeedType.Medical), needs.HealthSystem.IsBleeding ? 0.8f : 0f);
        if (medicalNeed > 0.4f)
        {
            options.Add(new NeedAction(medicalNeed, () =>
            {
                if (TryBeginBedRest(Mathf.Lerp(6f, 10f, Mathf.Clamp01(medicalNeed))))
                    return true;
                return BeginGroundRest(Mathf.Lerp(5f, 9f, Mathf.Clamp01(medicalNeed)), 0.2f, 0.3f);
            }));
        }

        if (options.Count == 0)
            return false;

        options.Sort((a, b) => b.priority.CompareTo(a.priority));
        foreach (var option in options)
        {
            if (option.action())
                return true;
        }
        return false;
    }

    public void ReleaseReservedPath()
    {
        var service = EnsurePathfindingService();
        service?.ReleaseAllReservations(owner);
        reservedPath.Clear();
        reservedPathSteps.Clear();
    }

    public void ReleaseCurrentTaskReservation()
    {
        ReleaseTaskReservation(currentTask);
    }

    private void EnsureOwnerModules()
    {
        if (owner == null)
            owner = GetComponent<Colonist>();
        if (needsModule == null)
            needsModule = GetComponent<ColonistNeeds>();
        if (socialModule == null)
            socialModule = GetComponent<ColonistSocial>();
    }

    void AssignPathForCurrentTask()
    {
        if (currentTask == null)
            return;

        if (currentTask is BuildWallTask)
        {
            ClearPath();
        }
        else if (currentTask is RestTask rest)
        {
            path = FindPath(Vector2Int.FloorToInt(owner.transform.position), Vector2Int.FloorToInt(rest.bed.transform.position));
        }
        else if (currentTask is HaulLogTask haul && haul.stage == HaulLogTask.Stage.MoveToItem && haul.item != null)
        {
            path = FindPath(Vector2Int.FloorToInt(owner.transform.position), Vector2Int.FloorToInt(haul.item.transform.position));
        }
        else
        {
            path = FindPath(Vector2Int.FloorToInt(owner.transform.position), Vector2Int.FloorToInt(currentTask.target));
        }

        if (path == null)
        {
            ReleaseTaskReservation(currentTask);
            currentTask = null;
            StartWander();
            return;
        }

        pathIndex = 0;
        wandering = false;
        owner.activity = "Moving";
    }

    void HandleBuildWallTask(BuildWallTask task, float deltaTime)
    {
        switch (task.stage)
        {
            case BuildWallTask.Stage.AcquireMaterials:
                if (task.project == null || !task.project.HasPendingResources)
                {
                    if (task.project == null || task.project.TryConsumeResources())
                    {
                        task.stage = BuildWallTask.Stage.MoveToSite;
                        path = FindPath(Vector2Int.FloorToInt(owner.transform.position), task.cell);
                        pathIndex = 0;
                        break;
                    }
                }

                if (task.project != null && task.project.HasPendingResources)
                {
                    var requirement = task.project.PeekNextRequirement();
                    if (requirement.HasValue)
                    {
                        if (task.targetItem == null)
                        {
                            ResourceItem chosen = null;
                            float best = float.MaxValue;
                            if (!TryGetResourceLogistics(out var logistics))
                            {
                                owner.activity = "Idle";
                                return;
                            }

                            foreach (var item in logistics.GetTrackedItems())
                            {
                                if (item == null || item.Reserved)
                                    continue;
                                if (item.Stack.Definition.Id != requirement.Value.Definition.Id)
                                    continue;
                                if (item.Stack.Quality < requirement.Value.MinimumQuality)
                                    continue;
                                float d = Vector2.Distance(owner.transform.position, item.transform.position);
                                if (d < best)
                                {
                                    best = d;
                                    chosen = item;
                                }
                            }
                            if (chosen != null)
                            {
                                chosen.Reserved = true;
                                task.targetItem = chosen;
                                path = FindPath(Vector2Int.FloorToInt(owner.transform.position), Vector2Int.FloorToInt(chosen.transform.position));
                                pathIndex = 0;
                                if (path == null)
                                {
                                    chosen.Reserved = false;
                                    task.targetItem = null;
                                    owner.activity = "Idle";
                                    return;
                                }
                            }
                            else
                            {
                                owner.activity = "Idle";
                                return;
                            }
                        }
                        else if (path == null || pathIndex >= path.Count)
                        {
                            if (TryGetResourceManager(out var manager))
                                manager.Add(task.targetItem.Stack);
                            Destroy(task.targetItem.gameObject);
                            task.targetItem = null;
                            task.project.TryConsumeResources();
                        }
                    }
                }
                MoveAlongPath(deltaTime);
                break;

            case BuildWallTask.Stage.MoveToSite:
                if (path == null || pathIndex >= path.Count)
                {
                    if (task.project == null || !task.project.HasPendingResources)
                    {
                        task.stage = BuildWallTask.Stage.Build;
                        actionTimer = task.buildTime;
                    }
                    else
                    {
                        task.stage = BuildWallTask.Stage.AcquireMaterials;
                        ClearPath();
                    }
                }
                else
                {
                    MoveAlongPath(deltaTime);
                }
                break;

            case BuildWallTask.Stage.Build:
                actionTimer -= deltaTime;
                if (actionTimer <= 0f)
                {
                    task.project?.AddWork(1f);
                    if (task.project == null || task.project.IsCompleted)
                    {
                        task.onComplete?.Invoke(owner);
                        currentTask = null;
                        owner.activity = "Idle";
                    }
                    else
                    {
                        task.stage = BuildWallTask.Stage.Build;
                        actionTimer = task.buildTime;
                    }
                }
                break;
        }
    }

    void HandleHaulLogTask(HaulLogTask task, float deltaTime)
    {
        switch (task.stage)
        {
            case HaulLogTask.Stage.MoveToItem:
                if (task.item == null)
                {
                    currentTask = null;
                    owner.activity = "Idle";
                    return;
                }

                if (path == null || pathIndex >= path.Count)
                {
                    path = FindPath(Vector2Int.FloorToInt(owner.transform.position), task.targetCell);
                    pathIndex = 0;
                    task.stage = HaulLogTask.Stage.MoveToZone;
                }
                else
                {
                    MoveAlongPath(deltaTime);
                }
                break;

            case HaulLogTask.Stage.MoveToZone:
                if (path == null || pathIndex >= path.Count)
                {
                    if (task.item != null)
                    {
                        if (TryGetResourceManager(out var manager))
                            manager.Add(task.item.Stack);
                        Destroy(task.item.gameObject);
                        task.ReleaseReservation();
                    }
                    currentTask = null;
                    owner.activity = "Idle";
                }
                else
                {
                    MoveAlongPath(deltaTime);
                }
                break;
        }
    }

    void HandleRestTask(RestTask task, float deltaTime)
    {
        switch (task.stage)
        {
            case RestTask.Stage.MoveToBed:
                if (task.bed == null)
                {
                    currentTask = null;
                    owner.activity = "Idle";
                    return;
                }

                if (path == null || pathIndex >= path.Count)
                {
                    task.stage = RestTask.Stage.Rest;
                    actionTimer = task.restTime;
                    owner.activity = "Resting";
                }
                else
                {
                    MoveAlongPath(deltaTime);
                }
                break;

            case RestTask.Stage.Rest:
                actionTimer -= deltaTime;
                owner.SatisfyNeed(NeedType.Rest, deltaTime / Mathf.Max(1f, task.restTime));
                owner.SatisfyNeed(NeedType.Stress, deltaTime / (task.restTime * 2f));
                owner.SatisfyNeed(NeedType.Comfort, deltaTime / (task.restTime * 3f));
                owner.SatisfyNeed(NeedType.Medical, deltaTime / (task.restTime * 3f));
                needsModule?.ApplyTreatment(deltaTime * 2f, 0f);
                if (actionTimer <= 0f)
                {
                    task.ReleaseReservation();
                    currentTask = null;
                    owner.activity = "Idle";
                }
                break;
        }
    }

    void ReleaseTaskReservation(Task task)
    {
        if (task == null)
            return;

        if (task is BuildWallTask buildTask)
            buildTask.ReleaseReservation();
        else if (task is HaulLogTask haulTask)
            haulTask.ReleaseReservation();
        else if (task is RestTask restTask)
            restTask.ReleaseReservation();
    }

    bool ReservePathFor(List<Vector2Int> candidatePath)
    {
        reservedPath.Clear();
        reservedPathSteps.Clear();

        if (candidatePath == null || candidatePath.Count == 0)
            return false;

        var service = EnsurePathfindingService();
        if (service == null)
            return true;

        var acquired = new List<Vector2Int>();
        var acquiredSteps = new Dictionary<Vector2Int, int>();

        for (int i = 0; i < candidatePath.Count; i++)
        {
            Vector2Int cell = candidatePath[i];
            bool isFinalStep = i == candidatePath.Count - 1;
            bool walkable = service.IsWalkable(cell);
            if (!walkable)
            {
                if (isFinalStep)
                    continue;

                foreach (var taken in acquired)
                    service.ReleaseReservation(taken, owner);
                return false;
            }

            if (!service.TryReserve(cell, owner))
            {
                foreach (var taken in acquired)
                    service.ReleaseReservation(taken, owner);
                return false;
            }

            acquired.Add(cell);
            acquiredSteps[cell] = i;
        }

        reservedPath.AddRange(acquired);
        foreach (var kvp in acquiredSteps)
            reservedPathSteps[kvp.Key] = kvp.Value;
        return true;
    }

    void ReleaseVisitedReservations()
    {
        var service = EnsurePathfindingService();
        if (service == null || reservedPath.Count == 0)
            return;

        for (int i = reservedPath.Count - 1; i >= 0; i--)
        {
            Vector2Int cell = reservedPath[i];
            if (!reservedPathSteps.TryGetValue(cell, out int step))
                continue;
            if (step < pathIndex - 1)
            {
                service.ReleaseReservation(cell, owner);
                reservedPath.RemoveAt(i);
                reservedPathSteps.Remove(cell);
            }
        }
    }

    List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        ReleaseReservedPath();

        var service = EnsurePathfindingService();
        if (service != null)
        {
            var result = service.FindPath(start, goal, owner, PathfindingService.PathfindingAlgorithm.AStar, true);
            if (result == null || result.Count == 0)
                return null;

            if (!ReservePathFor(result))
            {
                ReleaseReservedPath();
                return null;
            }

            return result;
        }

        var fallback = LegacyFindPath(start, goal);
        if (fallback == null || fallback.Count == 0)
            return null;
        return fallback;
    }

    List<Vector2Int> LegacyFindPath(Vector2Int start, Vector2Int goal)
    {
        if (map == null)
            return new List<Vector2Int> { goal };

        var open = new List<Node>();
        var closed = new HashSet<Vector2Int>();
        Node startNode = new Node(start, 0, LegacyHeuristic(start, goal));
        open.Add(startNode);

        int[,] dirs = new int[,] { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };

        while (open.Count > 0)
        {
            Node current = open[0];
            foreach (var n in open)
            {
                if (n.F < current.F)
                    current = n;
            }
            open.Remove(current);
            closed.Add(current.pos);

            if (current.pos == goal)
                return LegacyReconstruct(current);

            for (int i = 0; i < 4; i++)
            {
                Vector2Int next = current.pos + new Vector2Int(dirs[i, 0], dirs[i, 1]);
                if (closed.Contains(next) || (!map.IsPassable(next.x, next.y) && next != goal))
                    continue;

                int g = current.g + 1;
                Node existing = open.Find(n => n.pos == next);
                if (existing == null)
                {
                    Node node = new Node(next, g, LegacyHeuristic(next, goal));
                    node.parent = current;
                    open.Add(node);
                }
                else if (g < existing.g)
                {
                    existing.g = g;
                    existing.parent = current;
                }
            }
        }

        return null;
    }

    int LegacyHeuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    List<Vector2Int> LegacyReconstruct(Node node)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        while (node != null)
        {
            list.Add(node.pos);
            node = node.parent;
        }
        list.Reverse();
        return list;
    }

    PathfindingService EnsurePathfindingService()
    {
        if (pathfinding == null)
            pathfinding = PathfindingService.Instance;
        return pathfinding;
    }

    bool TryGetResourceManager(out IResourceManager manager)
    {
        if (resourceManager != null)
        {
            manager = resourceManager;
            return true;
        }

        if (GameServices.TryResolve(out manager))
        {
            resourceManager = manager;
            return true;
        }

        manager = null;
        return false;
    }

    bool TryGetResourceLogistics(out IResourceLogisticsService logistics)
    {
        if (resourceLogistics != null)
        {
            logistics = resourceLogistics;
            return true;
        }

        if (GameServices.TryResolve(out logistics))
        {
            resourceLogistics = logistics;
            return true;
        }

        logistics = null;
        return false;
    }

    bool TryBeginBedRest(float restTime)
    {
        Bed bed = Bed.FindAvailable(owner.transform.position);
        if (bed == null)
            return false;

        return TryAssignTask(new RestTask(bed, Mathf.Max(1f, restTime)));
    }

    bool BeginGroundRest(float duration, float fatigueRecovery, float stressRelief)
    {
        Vector2Int cell = Vector2Int.FloorToInt(owner.transform.position);
        Vector2 target = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
        var task = new RestOnGroundTask(target, Mathf.Max(2f, duration), Mathf.Max(0f, fatigueRecovery), Mathf.Max(0f, stressRelief));
        return TryAssignTask(task);
    }

    struct NeedAction
    {
        public float priority;
        public Func<bool> action;

        public NeedAction(float priority, Func<bool> action)
        {
            this.priority = priority;
            this.action = action;
        }
    }

    class Node
    {
        public Vector2Int pos;
        public int g;
        public int h;
        public Node parent;
        public int F => g + h;
        public Node(Vector2Int pos, int g, int h)
        {
            this.pos = pos;
            this.g = g;
            this.h = h;
        }
    }
}
