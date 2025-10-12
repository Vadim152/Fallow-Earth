using System;
using System.Collections.Generic;
using FallowEarth.Navigation;
using FallowEarth.Saving;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Colonist : SaveableMonoBehaviour
{
    public float moveSpeed = 10f;
    private float baseMoveSpeed;

    // basic stats for UI display
    [Range(0f,1f)] public float mood = 0.75f;
    [Range(0f,1f)] public float health = 1f;
    [HideInInspector] public string activity = "Idle";
    [Range(0f,1f)] public float hunger;
    [Range(0f,1f)] public float fatigue;
    [Range(0f,1f)] public float stress;
    [Range(0f,1f)] public float social;

    [SerializeField] private ColonistRoleProfile roleProfile;
    [SerializeField] private List<ColonistTrait> traits = new List<ColonistTrait>();

    public HashSet<JobType> jobPriorities = new HashSet<JobType>();

    private NeedTracker needs;
    private ColonistHealth healthSystem = new ColonistHealth();
    private ColonistSchedule schedule;
    private readonly Dictionary<Colonist, SocialRelationship> socialRelationships = new Dictionary<Colonist, SocialRelationship>();
    private readonly List<RelationshipSnapshot> pendingRelationshipData = new List<RelationshipSnapshot>();
    private float traitMoodModifier;

    private Task currentTask;
    private TaskManager taskManager;
    private MapGenerator map;
    private List<Vector2Int> path;
    private int pathIndex;
    private Rigidbody2D rb;
    private bool wandering;
    private float actionTimer;
    private bool mentalBreak;
    private float breakTimer;
    public float breakDuration = 8f;
    private PathfindingService pathfinding;
    private readonly List<Vector2Int> reservedPath = new List<Vector2Int>();
    private readonly Dictionary<Vector2Int, int> reservedPathSteps = new Dictionary<Vector2Int, int>();

    public bool IsBusy => currentTask != null;
    public ColonistRoleProfile RoleProfile => roleProfile;
    public ColonistSchedule Schedule => schedule;
    public NeedTracker Needs => needs;

    public void SatisfyNeed(NeedType type, float amount)
    {
        needs?.Satisfy(type, amount);
        SyncNeedFields();
    }

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        baseMoveSpeed = moveSpeed;
        pathfinding = PathfindingService.Instance;

        actionTimer = 0f;
        mentalBreak = false;
        breakTimer = 0f;

        if (GetComponent<SpriteRenderer>() == null)
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateColoredSprite(Color.yellow);
        }

        map = FindObjectOfType<MapGenerator>();
        taskManager = FindObjectOfType<TaskManager>();

        InitializeNeedSystem();
        InitializeRoleProfile();
        InitializeTraits();
        SyncNeedFields();
        health = Mathf.Clamp01(healthSystem.OverallHealth);
    }

    void InitializeNeedSystem()
    {
        needs = new NeedTracker();
        needs.RegisterNeed(new NeedDefinition(NeedType.Hunger, 0.45f, 0.6f, 0.55f), UnityEngine.Random.Range(0.2f, 0.6f));
        needs.RegisterNeed(new NeedDefinition(NeedType.Rest, 0.35f, 0.5f, 0.45f), UnityEngine.Random.Range(0.1f, 0.5f));
        needs.RegisterNeed(new NeedDefinition(NeedType.Recreation, 0.3f, 0.25f, 0.25f), UnityEngine.Random.Range(0.2f, 0.5f));
        needs.RegisterNeed(new NeedDefinition(NeedType.Social, 0.25f, 0.3f, 0.2f), UnityEngine.Random.Range(0.15f, 0.4f));
        needs.RegisterNeed(new NeedDefinition(NeedType.Stress, 0.3f, 0.4f, 0.2f, 0.05f), UnityEngine.Random.Range(0.1f, 0.3f));
        needs.RegisterNeed(new NeedDefinition(NeedType.Comfort, 0.25f, 0.2f, 0.15f), UnityEngine.Random.Range(0.2f, 0.6f));
        needs.RegisterNeed(new NeedDefinition(NeedType.Medical, 0.1f, 0.5f, 0.02f), 0f);
        needs.OnNeedChanged += HandleNeedChanged;
        healthSystem.InitializeDefaults();
    }

    void InitializeRoleProfile()
    {
        jobPriorities.Clear();
        if (roleProfile == null)
        {
            var defaults = ColonistRoleLibrary.DefaultRoles;
            if (defaults != null && defaults.Count > 0)
                roleProfile = defaults[UnityEngine.Random.Range(0, defaults.Count)];
        }

        schedule = roleProfile != null ? roleProfile.CreateSchedule() : new ColonistSchedule();

        if (roleProfile != null)
        {
            foreach (var job in roleProfile.AllowedJobs)
                jobPriorities.Add(job);
        }
        else
        {
            foreach (JobType jt in Enum.GetValues(typeof(JobType)))
                jobPriorities.Add(jt);
        }
        EnsureEssentialJobs();
    }

    void InitializeTraits()
    {
        traitMoodModifier = 0f;
        if (traits == null)
            traits = new List<ColonistTrait>();

        needs?.ResetModifiers();

        if (traits.Count == 0)
        {
            var randomTrait = ColonistTraitLibrary.GetRandomTrait();
            if (randomTrait != null)
                traits.Add(randomTrait);
        }

        foreach (var trait in traits)
        {
            traitMoodModifier += trait.MoodModifier;
            foreach (var effect in trait.NeedEffects)
            {
                if (effect.multiplier > 0f && !Mathf.Approximately(effect.multiplier, 1f))
                    needs.MultiplyTraitMultiplier(effect.type, effect.multiplier);
                if (Mathf.Abs(effect.expectationOffset) > 0.001f)
                    needs.AddExpectationModifier(effect.type, effect.expectationOffset);
            }
        }
    }

    void HandleNeedChanged(NeedType type, float value)
    {
        switch (type)
        {
            case NeedType.Hunger:
                hunger = value;
                break;
            case NeedType.Rest:
                fatigue = value;
                break;
            case NeedType.Stress:
                stress = value;
                break;
            case NeedType.Social:
                social = Mathf.Clamp01(1f - value);
                break;
        }
    }

    void SyncNeedFields()
    {
        hunger = needs.GetValue(NeedType.Hunger);
        fatigue = needs.GetValue(NeedType.Rest);
        stress = needs.GetValue(NeedType.Stress);
        social = Mathf.Clamp01(1f - needs.GetValue(NeedType.Social));
    }

    void RestorePendingRelationships()
    {
        if (pendingRelationshipData.Count == 0)
            return;

        var colonists = FindObjectsOfType<Colonist>();
        foreach (var snapshot in pendingRelationshipData)
        {
            if (string.IsNullOrEmpty(snapshot.colonistName))
                continue;

            foreach (var other in colonists)
            {
                if (other == this)
                    continue;
                if (other.name == snapshot.colonistName)
                {
                    var relationship = EnsureRelationship(other);
                    if (relationship != null)
                    {
                        float impact = snapshot.affinity - relationship.Affinity;
                        if (Mathf.Abs(impact) > 0.001f)
                            relationship.AddEvent("Rekindled bond", impact);
                    }
                    break;
                }
            }
        }

        pendingRelationshipData.Clear();
    }

    public SocialRelationship EnsureRelationship(Colonist other)
    {
        if (other == null)
            return null;
        if (!socialRelationships.TryGetValue(other, out SocialRelationship relationship))
        {
            relationship = new SocialRelationship();
            socialRelationships[other] = relationship;
        }
        return relationship;
    }

    void Start()
    {
        if (map == null)
            map = FindObjectOfType<MapGenerator>();
        if (taskManager == null)
            taskManager = FindObjectOfType<TaskManager>();
        if (schedule == null)
            schedule = roleProfile != null ? roleProfile.CreateSchedule() : new ColonistSchedule();
        RestorePendingRelationships();
    }

    void OnDestroy()
    {
        if (needs != null)
            needs.OnNeedChanged -= HandleNeedChanged;
        ReleaseReservedPath();
    }

    public override SaveCategory Category => SaveCategory.Creature;

    [Serializable]
    private struct ColonistSaveState
    {
        public Vector3 position;
        public float mood;
        public float health;
        public float hunger;
        public float fatigue;
        public float stress;
        public float social;
        public string activity;
        public List<JobType> allowedJobs;
        public List<NeedSnapshot> needs;
        public string role;
        public ColonistScheduleActivity[] schedule;
        public List<string> traits;
        public List<RelationshipSnapshot> relationships;
    }

    [Serializable]
    private struct NeedSnapshot
    {
        public NeedType type;
        public float value;
    }

    [Serializable]
    private struct RelationshipSnapshot
    {
        public string colonistName;
        public float affinity;
    }

    public override void PopulateSaveData(SaveData saveData)
    {
        var needSnapshots = new List<NeedSnapshot>();
        if (needs != null)
        {
            foreach (var kvp in needs.CreateSnapshot())
                needSnapshots.Add(new NeedSnapshot { type = kvp.Key, value = kvp.Value });
        }

        var traitNames = new List<string>();
        if (traits != null)
        {
            foreach (var trait in traits)
                traitNames.Add(trait.Name);
        }

        var relationshipData = new List<RelationshipSnapshot>();
        foreach (var kvp in socialRelationships)
        {
            if (kvp.Key == null || kvp.Value == null)
                continue;
            relationshipData.Add(new RelationshipSnapshot
            {
                colonistName = kvp.Key.name,
                affinity = kvp.Value.Affinity
            });
        }

        var state = new ColonistSaveState
        {
            position = transform.position,
            mood = mood,
            health = health,
            hunger = hunger,
            fatigue = fatigue,
            stress = stress,
            social = social,
            activity = activity,
            allowedJobs = new List<JobType>(jobPriorities),
            needs = needSnapshots,
            role = roleProfile != null ? roleProfile.RoleName : null,
            schedule = schedule != null ? schedule.ToArray() : null,
            traits = traitNames,
            relationships = relationshipData
        };
        saveData.Set("colonist", state);
    }

    public override void LoadFromSaveData(SaveData saveData)
    {
        if (saveData.TryGet("colonist", out ColonistSaveState state))
        {
            transform.position = state.position;
            mood = state.mood;
            health = state.health;
            hunger = state.hunger;
            fatigue = state.fatigue;
            stress = state.stress;
            social = state.social;

            jobPriorities.Clear();
            if (state.allowedJobs != null)
            {
                foreach (var job in state.allowedJobs)
                    jobPriorities.Add(job);
            }
            else
            {
                foreach (JobType jt in Enum.GetValues(typeof(JobType)))
                    jobPriorities.Add(jt);
            }

            if (needs == null)
                InitializeNeedSystem();

            if (state.needs != null)
            {
                var snapshot = new Dictionary<NeedType, float>();
                foreach (var need in state.needs)
                    snapshot[need.type] = need.value;
                needs.RestoreSnapshot(snapshot);
            }

            if (!string.IsNullOrEmpty(state.role))
            {
                roleProfile = null;
                var defaults = ColonistRoleLibrary.DefaultRoles;
                if (defaults != null)
                {
                    foreach (var profile in defaults)
                    {
                        if (profile.RoleName == state.role)
                        {
                            roleProfile = profile;
                            break;
                        }
                    }
                }
            }

            schedule = roleProfile != null ? roleProfile.CreateSchedule() : new ColonistSchedule();
            if (state.schedule != null)
                schedule.LoadFrom(state.schedule);

            traits.Clear();
            if (state.traits != null)
            {
                foreach (var traitName in state.traits)
                {
                    var trait = ColonistTraitLibrary.CreateTrait(traitName);
                    if (trait != null)
                        traits.Add(trait);
                }
            }
            InitializeTraits();
            SyncNeedFields();

            pendingRelationshipData.Clear();
            if (state.relationships != null)
                pendingRelationshipData.AddRange(state.relationships);

            CancelTasks();
            mentalBreak = false;
            breakTimer = 0f;
            wandering = false;
            ClearPath();
            actionTimer = 0f;
            activity = state.activity ?? "Idle";
        }
    }

    public void SetJobAllowed(JobType job, bool allowed)
    {
        if (allowed)
            jobPriorities.Add(job);
        else
            jobPriorities.Remove(job);
    }

    public bool IsJobAllowed(JobType job) => jobPriorities.Contains(job);

    public ColonistScheduleActivityMask GetCurrentScheduleMask()
    {
        int hour = 0;
        if (DayNightCycle.Instance != null)
            hour = DayNightCycle.Instance.CurrentHourInt;
        else
            hour = Mathf.FloorToInt((Time.time / 60f) % 24f);

        if (schedule == null)
            schedule = roleProfile != null ? roleProfile.CreateSchedule() : new ColonistSchedule();
        return schedule.ToMask(hour);
    }

    public void AssignRoleProfile(ColonistRoleProfile profile, bool overrideJobs = true)
    {
        roleProfile = profile;
        schedule = roleProfile != null ? roleProfile.CreateSchedule() : schedule ?? new ColonistSchedule();
        if (!overrideJobs)
            return;

        jobPriorities.Clear();
        if (roleProfile != null)
        {
            foreach (var job in roleProfile.AllowedJobs)
                jobPriorities.Add(job);
        }
        else
        {
            foreach (JobType jt in Enum.GetValues(typeof(JobType)))
                jobPriorities.Add(jt);
        }
        EnsureEssentialJobs();
    }

    public bool HealthSystemNeedsMedicalAttention()
    {
        if (healthSystem == null)
            return false;
        if (healthSystem.IsBleeding || healthSystem.OverallHealth < 0.6f)
            return true;
        return needs != null && needs.GetValue(NeedType.Medical) > 0.5f;
    }

    void EnsureEssentialJobs()
    {
        jobPriorities.Add(JobType.Rest);
        jobPriorities.Add(JobType.Social);
    }

    public void SetTask(Task task)
    {
        if (currentTask == task)
            return;

        if (currentTask != null)
        {
            ReleaseCurrentTaskReservation();
        }

        ClearPath();

        currentTask = task;
        if (currentTask != null)
        {
            if (currentTask is BuildWallTask)
            {
                ClearPath();
            }
            else if (currentTask is RestTask rest)
            {
                path = FindPath(Vector2Int.FloorToInt(transform.position), Vector2Int.FloorToInt(rest.bed.transform.position));
            }
            else if (currentTask is HaulLogTask haul && haul.stage == HaulLogTask.Stage.MoveToLog)
            {
                path = FindPath(Vector2Int.FloorToInt(transform.position), Vector2Int.FloorToInt(haul.log.transform.position));
            }
            else
            {
                path = FindPath(Vector2Int.FloorToInt(transform.position), Vector2Int.FloorToInt(currentTask.target));
            }

            if (path == null)
            {
                ReleaseTaskReservation(task);
                currentTask = null;
                StartWander();
                return;
            }

            pathIndex = 0;
            wandering = false;
            activity = "Moving";
        }
    }

    public bool TryAssignTask(Task task)
    {
        if (task == null)
            return false;

        SetTask(task);
        return ReferenceEquals(currentTask, task);
    }

    public void CancelTasks()
    {
        if (currentTask != null)
        {
            ReleaseCurrentTaskReservation();
        }

        currentTask = null;
        ClearPath();
        activity = "Idle";
    }

    void ReleaseCurrentTaskReservation()
    {
        ReleaseTaskReservation(currentTask);
    }

    void ReleaseTaskReservation(Task task)
    {
        if (task == null)
            return;

        if (task is BuildWallTask buildTask)
        {
            buildTask.ReleaseReservation();
        }
        else if (task is HaulLogTask haulTask)
        {
            haulTask.ReleaseReservation();
        }
        else if (task is RestTask restTask)
        {
            restTask.ReleaseReservation();
        }
    }

    PathfindingService EnsurePathfindingService()
    {
        if (pathfinding == null)
            pathfinding = PathfindingService.Instance;
        return pathfinding;
    }

    void ReleaseReservedPath()
    {
        var service = EnsurePathfindingService();
        service?.ReleaseAllReservations(this);
        reservedPath.Clear();
        reservedPathSteps.Clear();
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
                    service.ReleaseReservation(taken, this);
                return false;
            }

            if (!service.TryReserve(cell, this))
            {
                foreach (var taken in acquired)
                    service.ReleaseReservation(taken, this);
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
                service.ReleaseReservation(cell, this);
                reservedPath.RemoveAt(i);
                reservedPathSteps.Remove(cell);
            }
        }
    }

    void ClearPath()
    {
        ReleaseReservedPath();
        path = null;
        pathIndex = 0;
    }

    void Update()
    {
        UpdateNeeds();
        if (!mentalBreak && (mood < 0.2f || stress > 0.95f))
        {
            mentalBreak = true;
            breakTimer = breakDuration;
            CancelTasks();
            StartWander();
            activity = "Panicking";
        }

        if (mentalBreak)
        {
            breakTimer -= Time.deltaTime;
            stress = Mathf.Clamp01(stress - Time.deltaTime / breakDuration);
            if (path == null || pathIndex >= path.Count)
                StartWander();
            if (breakTimer <= 0f && mood >= 0.3f && stress <= 0.8f)
            {
                mentalBreak = false;
                CancelTasks();
            }
            MoveAlongPath();
            return;
        }

        if (currentTask == null)
        {
            currentTask = taskManager != null ? taskManager.GetNextTask(this) : null;

            if (currentTask != null)
            {
                if (currentTask is BuildWallTask)
                {
                    ClearPath();
                }
                else if (currentTask is HaulLogTask haul && haul.stage == HaulLogTask.Stage.MoveToLog)
                {
                    path = FindPath(Vector2Int.FloorToInt(transform.position), Vector2Int.FloorToInt(haul.log.transform.position));
                }
                else
                {
                    path = FindPath(Vector2Int.FloorToInt(transform.position), Vector2Int.FloorToInt(currentTask.target));
                }

                if (path == null)
                {
                    currentTask = null;
                    StartWander();
                }
                else
                {
                    pathIndex = 0;
                    wandering = false;
                }
            }
            else if (path == null || pathIndex >= path.Count)
            {
                if (!EvaluateNeeds())
                    StartWander();
            }
        }
        if (currentTask is BuildWallTask bw)
        {
            HandleBuildWallTask(bw);
            return;
        }
        else if (currentTask is HaulLogTask hl)
        {
            HandleHaulLogTask(hl);
            return;
        }
        else if (currentTask is RestTask rt)
        {
            HandleRestTask(rt);
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
                        activity = "Eating";
                    else if (currentTask is SocializeTask)
                        activity = "Talking";
                    else if (currentTask is RestOnGroundTask groundRest)
                    {
                        activity = "Resting";
                        if (groundRest.duration > 0f)
                        {
                            float fatiguePerSecond = groundRest.fatigueRecoveryAmount / groundRest.duration;
                            float stressPerSecond = groundRest.stressReliefAmount / groundRest.duration;
                            if (fatiguePerSecond > 0f)
                                SatisfyNeed(NeedType.Rest, fatiguePerSecond * Time.deltaTime);
                            if (stressPerSecond > 0f)
                                SatisfyNeed(NeedType.Stress, stressPerSecond * Time.deltaTime);
                        }
                    }
                    actionTimer -= Time.deltaTime;
                    if (actionTimer <= 0f)
                    {
                        actionTimer = 0f;
                        currentTask.Complete(this);
                        currentTask = null;
                        activity = "Idle";
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    currentTask.Complete(this);
                    currentTask = null;
                    activity = "Idle";
                }
            }

            if (wandering)
                wandering = false;
            return;
        }

        MoveAlongPath();
    }

    void MoveAlongPath()
    {
        if (path == null || pathIndex >= path.Count)
            return;
        Vector2 targetPos = path[pathIndex];
        Vector2 dir = targetPos - rb.position;
        float dist = dir.magnitude;
        if (dist < 0.05f)
        {
            rb.position = targetPos;
            pathIndex++;
            ReleaseVisitedReservations();
        }
        else
        {
            float speedMod = 1f - Mathf.Clamp01((hunger + fatigue) * 0.5f);
            if (WeatherSystem.Instance != null)
                speedMod *= WeatherSystem.Instance.GetMoveSpeedMultiplier();
            rb.MovePosition(rb.position + dir.normalized * baseMoveSpeed * speedMod * Time.deltaTime);
        }
    }

    void HandleBuildWallTask(BuildWallTask task)
    {
        switch (task.stage)
        {
            case BuildWallTask.Stage.CollectWood:
                if (ResourceManager.Instance != null && ResourceManager.Instance.Wood >= task.woodNeeded)
                {
                    task.stage = BuildWallTask.Stage.MoveToSite;
                    path = FindPath(Vector2Int.FloorToInt(transform.position), task.cell);
                    pathIndex = 0;
                }
                else
                {
                    if (task.targetLog == null)
                    {
                        var logs = GameObject.FindObjectsOfType<WoodLog>();
                        float best = float.MaxValue;
                        WoodLog chosen = null;
                        foreach (var l in logs)
                        {
                            if (l.Reserved)
                                continue;
                            float d = Vector2.Distance(transform.position, l.transform.position);
                            if (d < best)
                            {
                                best = d;
                                chosen = l;
                            }
                        }
                        if (chosen == null)
                        {
                            activity = "Idle";
                            return;
                        }
                        chosen.Reserved = true;
                        task.targetLog = chosen;
                        path = FindPath(Vector2Int.FloorToInt(transform.position), Vector2Int.FloorToInt(chosen.transform.position));
                        pathIndex = 0;
                    }
                    else if (path == null || pathIndex >= path.Count)
                    {
                        ResourceManager.AddWood(task.targetLog.Amount);
                        UnityEngine.Object.Destroy(task.targetLog.gameObject);
                        task.targetLog = null;
                        if (ResourceManager.Instance != null && ResourceManager.Instance.Wood >= task.woodNeeded)
                        {
                            task.stage = BuildWallTask.Stage.MoveToSite;
                            path = FindPath(Vector2Int.FloorToInt(transform.position), task.cell);
                            pathIndex = 0;
                        }
                        else
                        {
                            task.targetLog = null;
                        }
                    }
                }
                MoveAlongPath();
                break;

            case BuildWallTask.Stage.MoveToSite:
                if (path == null || pathIndex >= path.Count)
                {
                    if (ResourceManager.Instance != null && ResourceManager.Instance.Wood >= task.woodNeeded)
                    {
                        task.stage = BuildWallTask.Stage.Build;
                        actionTimer = task.buildTime;
                    }
                    else
                    {
                        task.stage = BuildWallTask.Stage.CollectWood;
                        ClearPath();
                    }
                }
                else
                {
                    MoveAlongPath();
                }
                break;

            case BuildWallTask.Stage.Build:
                actionTimer -= Time.deltaTime;
                if (actionTimer <= 0f)
                {
                    if (ResourceManager.UseWood(task.woodNeeded))
                    {
                        task.onComplete?.Invoke(this);
                        currentTask = null;
                        activity = "Idle";
                    }
                    else
                    {
                        task.stage = BuildWallTask.Stage.CollectWood;
                    }
                }
                break;
        }
    }

    void HandleHaulLogTask(HaulLogTask task)
    {
        switch (task.stage)
        {
            case HaulLogTask.Stage.MoveToLog:
                if (task.log == null)
                {
                    currentTask = null;
                    activity = "Idle";
                    return;
                }

                if (path == null || pathIndex >= path.Count)
                {
                    path = FindPath(Vector2Int.FloorToInt(transform.position), task.targetCell);
                    pathIndex = 0;
                    task.stage = HaulLogTask.Stage.MoveToZone;
                }
                else
                {
                    MoveAlongPath();
                }
                break;

            case HaulLogTask.Stage.MoveToZone:
                if (path == null || pathIndex >= path.Count)
                {
                    if (task.log != null)
                    {
                        ResourceManager.AddWood(task.log.Amount);
                        UnityEngine.Object.Destroy(task.log.gameObject);
                    }
                    currentTask = null;
                    activity = "Idle";
                }
                else
                {
                    MoveAlongPath();
                }
                break;
        }
    }

    void HandleRestTask(RestTask task)
    {
        switch (task.stage)
        {
            case RestTask.Stage.MoveToBed:
                if (task.bed == null)
                {
                    currentTask = null;
                    activity = "Idle";
                    return;
                }

                if (path == null || pathIndex >= path.Count)
                {
                    task.stage = RestTask.Stage.Rest;
                    actionTimer = task.restTime;
                    activity = "Resting";
                }
                else
                {
                    MoveAlongPath();
                }
                break;

            case RestTask.Stage.Rest:
                actionTimer -= Time.deltaTime;
                SatisfyNeed(NeedType.Rest, Time.deltaTime / Mathf.Max(1f, task.restTime));
                SatisfyNeed(NeedType.Stress, Time.deltaTime / (task.restTime * 2f));
                SatisfyNeed(NeedType.Comfort, Time.deltaTime / (task.restTime * 3f));
                SatisfyNeed(NeedType.Medical, Time.deltaTime / (task.restTime * 3f));
                healthSystem.ApplyTreatment(Time.deltaTime * 2f, 0f);
                if (actionTimer <= 0f)
                {
                    task.ReleaseReservation();
                    currentTask = null;
                    activity = "Idle";
                }
                break;
        }
    }

    List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        ReleaseReservedPath();

        var service = EnsurePathfindingService();
        if (service != null)
        {
            var result = service.FindPath(start, goal, this, PathfindingService.PathfindingAlgorithm.AStar, true);
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
        {
            return new List<Vector2Int> { goal };
        }

        var open = new List<Node>();
        var closed = new HashSet<Vector2Int>();
        Node startNode = new Node(start, 0, LegacyHeuristic(start, goal));
        open.Add(startNode);

        int[,] dirs = new int[,] { {1,0}, {-1,0}, {0,1}, {0,-1} };

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
                Vector2Int next = current.pos + new Vector2Int(dirs[i,0], dirs[i,1]);
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

    bool EvaluateNeeds()
    {
        var options = new List<NeedAction>();

        float hungerNeed = needs.GetValue(NeedType.Hunger);
        if (hungerNeed > 0.45f)
        {
            options.Add(new NeedAction(hungerNeed, () =>
            {
                if (map != null && map.TryFindClosestBerryCell(transform.position, out var berry))
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
            options.Add(new NeedAction(socialNeed, () => TryStartSocialize()));
        }

        float stressNeed = needs.GetValue(NeedType.Stress);
        if (stressNeed > 0.7f)
        {
            options.Add(new NeedAction(stressNeed, () =>
            {
                if (TryStartSocialize())
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
                if (TryStartSocialize())
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

        float medicalNeed = Mathf.Max(needs.GetValue(NeedType.Medical), healthSystem.IsBleeding ? 0.8f : 0f);
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
        foreach (var opt in options)
        {
            if (opt.action())
                return true;
        }
        return false;
    }

    bool TryBeginBedRest(float restTime)
    {
        Bed bed = Bed.FindAvailable(transform.position);
        if (bed == null)
            return false;

        return TryAssignTask(new RestTask(bed, Mathf.Max(1f, restTime)));
    }

    bool BeginGroundRest(float duration, float fatigueRecovery, float stressRelief)
    {
        Vector2Int cell = Vector2Int.FloorToInt(transform.position);
        Vector2 target = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
        var task = new RestOnGroundTask(target, Mathf.Max(2f, duration),
            Mathf.Max(0f, fatigueRecovery), Mathf.Max(0f, stressRelief));
        return TryAssignTask(task);
    }

    void StartWander()
    {
        if (map == null)
            return;

        Vector2Int start = Vector2Int.FloorToInt(transform.position);
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
                    activity = "Wandering";
                    return;
                }
            }
        }
    }

    bool TryStartSocialize()
    {
        Colonist[] all = FindObjectsOfType<Colonist>();
        Colonist best = null;
        float bestScore = float.MinValue;
        float ownSocialNeed = needs.GetValue(NeedType.Social);
        foreach (var c in all)
        {
            if (c == this || c.IsBusy)
                continue;

            float dist = Vector2.Distance(transform.position, c.transform.position);
            if (dist > 4f)
                continue; // too far to bother

            // Prefer partners that also need social interaction
            float partnerNeed = c.Needs != null ? c.Needs.GetValue(NeedType.Social) : (1f - c.social);
            float affinity = 0f;
            var relation = EnsureRelationship(c);
            if (relation != null)
                affinity = relation.Affinity;
            float score = partnerNeed - dist * 0.25f + affinity * 0.3f;
            if (score > bestScore && partnerNeed > 0.35f)
            {
                bestScore = score;
                best = c;
            }
        }

        if (best != null && (ownSocialNeed > 0.35f || (best.Needs != null && best.Needs.GetValue(NeedType.Social) > 0.35f)))
        {
            float dur = UnityEngine.Random.Range(1.5f, 3.5f);
            Vector2 meetPoint = (transform.position + best.transform.position) * 0.5f;
            var relationA = EnsureRelationship(best);
            var relationB = best.EnsureRelationship(this);
            var taskA = new SocializeTask(best, meetPoint, dur,
                col => {
                    col.SatisfyNeed(NeedType.Social, 0.5f);
                    col.SatisfyNeed(NeedType.Stress, 0.3f);
                    col.SatisfyNeed(NeedType.Recreation, 0.3f);
                    relationA?.AddEvent("Pleasant chat", 0.1f);
                });
            var taskB = new SocializeTask(this, meetPoint, dur,
                col => {
                    col.SatisfyNeed(NeedType.Social, 0.5f);
                    col.SatisfyNeed(NeedType.Stress, 0.3f);
                    col.SatisfyNeed(NeedType.Recreation, 0.3f);
                    relationB?.AddEvent("Pleasant chat", 0.1f);
                });
            if (TryAssignTask(taskA))
            {
                if (best.TryAssignTask(taskB))
                    return true;

                CancelTasks();
            }
        }
        return false;
    }

    void UpdateNeeds()
    {
        if (needs == null)
            return;

        float dt = Time.deltaTime;
        needs.Tick(dt);
        healthSystem.Tick(dt);

        if (healthSystem.IsBleeding)
        {
            needs.AddStress(NeedType.Medical, healthSystem.BleedSeverity * dt * 0.08f);
            needs.AddStress(NeedType.Stress, healthSystem.BleedSeverity * dt * 0.05f);
        }

        if (currentTask != null && !(currentTask is RestTask) && !(currentTask is SocializeTask) && !(currentTask is RestOnGroundTask))
            needs.AddStress(NeedType.Stress, dt * 0.12f);
        else
            SatisfyNeed(NeedType.Stress, dt * 0.08f);

        if (activity == "Resting")
        {
            needs.Satisfy(NeedType.Rest, dt * 0.12f);
            needs.Satisfy(NeedType.Stress, dt * 0.06f);
        }
        else if (activity == "Talking")
        {
            needs.Satisfy(NeedType.Social, dt * 0.15f);
            needs.Satisfy(NeedType.Stress, dt * 0.04f);
        }

        if (HealthSystemNeedsMedicalAttention())
            needs.AddStress(NeedType.Medical, dt * 0.05f);

        SyncNeedFields();

        float relationshipMood = 0f;
        if (socialRelationships.Count > 0)
        {
            foreach (var rel in socialRelationships.Values)
                relationshipMood += rel?.GetMoodModifier() ?? 0f;
            relationshipMood /= socialRelationships.Count;
        }

        float moodPenalty = needs.GetMoodImpact();
        mood = Mathf.Clamp01(0.65f - moodPenalty + traitMoodModifier + relationshipMood);
        health = Mathf.Clamp01(healthSystem.OverallHealth);
    }

    Sprite CreateColoredSprite(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
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
