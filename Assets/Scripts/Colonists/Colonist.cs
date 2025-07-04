using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Colonist : MonoBehaviour
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

    public HashSet<JobType> jobPriorities = new HashSet<JobType>();

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

    public bool IsBusy => currentTask != null;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        baseMoveSpeed = moveSpeed;

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

        foreach (JobType jt in System.Enum.GetValues(typeof(JobType)))
            jobPriorities.Add(jt);

        // initialize stats with random values so the info card has data
        hunger = UnityEngine.Random.Range(0f, 1f);
        fatigue = UnityEngine.Random.Range(0f, 1f);
        stress = UnityEngine.Random.Range(0f, 1f);
        social = UnityEngine.Random.Range(0f, 1f);
    }

    void Start()
    {
        if (map == null)
            map = FindObjectOfType<MapGenerator>();
        if (taskManager == null)
            taskManager = FindObjectOfType<TaskManager>();
    }

    public void SetJobAllowed(JobType job, bool allowed)
    {
        if (allowed)
            jobPriorities.Add(job);
        else
            jobPriorities.Remove(job);
    }

    public bool IsJobAllowed(JobType job) => jobPriorities.Contains(job);

    public void SetTask(Task task)
    {
        currentTask = task;
        if (currentTask != null)
        {
            if (currentTask is BuildWallTask)
            {
                path = null;
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
                currentTask = null;
                StartWander();
                return;
            }

            pathIndex = 0;
            wandering = false;
            activity = "Moving";
        }
    }

    public void CancelTasks()
    {
        currentTask = null;
        path = null;
        activity = "Idle";
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
            currentTask = taskManager != null ? taskManager.GetNextTask() : null;

            if (currentTask != null)
            {
                if (currentTask is BuildWallTask)
                {
                    path = null;
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
                if (hunger > 0.6f && map != null && map.TryFindClosestBerryCell(transform.position, out var berry))
                {
                    SetTask(new EatBerryTask(berry, 2f, c =>
                    {
                        map.RemoveBerries(berry.x, berry.y);
                        c.hunger = Mathf.Clamp01(c.hunger - 0.5f);
                    }));
                }
                else if (fatigue > 0.6f)
                {
                    Bed bed = Bed.FindAvailable(transform.position);
                    if (bed != null)
                    {
                        SetTask(new RestTask(bed, 5f));
                    }
                    else
                    {
                        StartWander();
                    }
                }
                else if (social < 0.4f && TryStartSocialize())
                {
                }
                else if (stress > 0.8f)
                {
                    if (!TryStartSocialize())
                    {
                        Bed bed = Bed.FindAvailable(transform.position);
                        if (bed != null)
                            SetTask(new RestTask(bed, 4f));
                        else
                            StartWander();
                    }
                }
                else
                {
                    StartWander();
                }
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
                        path = null;
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
                fatigue = Mathf.Clamp01(fatigue - Time.deltaTime / task.restTime);
                if (actionTimer <= 0f)
                {
                    if (task.bed != null)
                        task.bed.Reserved = false;
                    currentTask = null;
                    activity = "Idle";
                }
                break;
        }
    }

    List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (map == null)
        {
            return new List<Vector2Int> { goal };
        }

        var open = new List<Node>();
        var closed = new HashSet<Vector2Int>();
        Node startNode = new Node(start, 0, Heuristic(start, goal));
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
                return Reconstruct(current);

            for (int i = 0; i < 4; i++)
            {
                Vector2Int next = current.pos + new Vector2Int(dirs[i,0], dirs[i,1]);
                if (closed.Contains(next) || (!map.IsPassable(next.x, next.y) && next != goal))
                    continue;

                int g = current.g + 1;
                Node existing = open.Find(n => n.pos == next);
                if (existing == null)
                {
                    Node node = new Node(next, g, Heuristic(next, goal));
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

        // no path found
        return null;
    }

    int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    List<Vector2Int> Reconstruct(Node node)
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
        foreach (var c in all)
        {
            if (c == this || c.IsBusy)
                continue;

            float dist = Vector2.Distance(transform.position, c.transform.position);
            if (dist > 4f)
                continue; // too far to bother

            // Prefer partners that also need social interaction
            float score = (1f - c.social) - dist * 0.25f;
            if (score > bestScore && c.social < 0.6f)
            {
                bestScore = score;
                best = c;
            }
        }

        if (best != null && (social < 0.6f || best.social < 0.6f))
        {
            float dur = UnityEngine.Random.Range(1.5f, 3.5f);
            Vector2 meetPoint = (transform.position + best.transform.position) * 0.5f;
            var taskA = new SocializeTask(best, meetPoint, dur,
                col => {
                    col.social = Mathf.Clamp01(col.social + 0.5f);
                    col.stress = Mathf.Clamp01(col.stress - 0.3f);
                });
            var taskB = new SocializeTask(this, meetPoint, dur,
                col => {
                    col.social = Mathf.Clamp01(col.social + 0.5f);
                    col.stress = Mathf.Clamp01(col.stress - 0.3f);
                });
            SetTask(taskA);
            best.SetTask(taskB);
            return true;
        }
        return false;
    }

    void UpdateNeeds()
    {
        float dt = Time.deltaTime / 60f;
        hunger = Mathf.Clamp01(hunger + dt);
        fatigue = Mathf.Clamp01(fatigue + dt * 0.5f);
        social = Mathf.Clamp01(social - dt * 0.1f);
        float stressChange = dt * 0.05f;
        if (currentTask != null && !(currentTask is RestTask) && !(currentTask is SocializeTask))
            stressChange += dt * 0.1f;
        if (activity == "Resting" || activity == "Talking")
            stressChange -= dt * 0.2f;
        stress = Mathf.Clamp01(stress + stressChange);
        mood = Mathf.Clamp01(1f - (hunger + fatigue) * 0.5f - (1f - social) * 0.2f - stress * 0.3f);
    }

    Sprite CreateColoredSprite(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
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
