using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Colonist : MonoBehaviour
{
    public float moveSpeed = 2f;

    private Task currentTask;
    private TaskManager taskManager;
    private MapGenerator map;
    private List<Vector2Int> path;
    private int pathIndex;
    private Rigidbody2D rb;
    private bool wandering;
    private float actionTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        actionTimer = 0f;

        if (GetComponent<SpriteRenderer>() == null)
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateColoredSprite(Color.yellow);
        }

        map = FindObjectOfType<MapGenerator>();
        taskManager = FindObjectOfType<TaskManager>();
    }

    void Start()
    {
        if (map == null)
            map = FindObjectOfType<MapGenerator>();
        if (taskManager == null)
            taskManager = FindObjectOfType<TaskManager>();
    }

    public void SetTask(Task task)
    {
        currentTask = task;
        if (currentTask != null)
        {
            path = FindPath(Vector2Int.RoundToInt(transform.position), Vector2Int.RoundToInt(currentTask.target));
            pathIndex = 0;
            wandering = false;
        }
    }

    void Update()
    {
        if (currentTask == null)
        {
            currentTask = taskManager != null ? taskManager.GetNextTask() : null;

            if (currentTask != null)
            {
                path = FindPath(Vector2Int.RoundToInt(transform.position), Vector2Int.RoundToInt(currentTask.target));
                pathIndex = 0;
                wandering = false;
            }
            else if (path == null || pathIndex >= path.Count)
            {
                StartWander();
            }
        }

        if (path == null || pathIndex >= path.Count)
        {
            if (!wandering && currentTask != null)
            {
                if (currentTask is TimedTask timed)
                {
                    if (actionTimer <= 0f)
                        actionTimer = timed.duration;
                    actionTimer -= Time.deltaTime;
                    if (actionTimer <= 0f)
                    {
                        actionTimer = 0f;
                        currentTask.Complete(this);
                        currentTask = null;
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
                }
            }

            if (wandering)
                wandering = false;
            return;
        }

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
            rb.MovePosition(rb.position + dir.normalized * moveSpeed * Time.deltaTime);
        }
    }

    List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        var result = new List<Vector2Int>();
        if (map == null)
        {
            result.Add(goal);
            return result;
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
                if (closed.Contains(next) || !map.IsPassable(next.x, next.y))
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

        result.Add(goal);
        return result;
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

        Vector2Int start = Vector2Int.RoundToInt(transform.position);
        for (int i = 0; i < 20; i++)
        {
            int x = Random.Range(0, map.width);
            int y = Random.Range(0, map.height);
            if (map.IsPassable(x, y))
            {
                path = FindPath(start, new Vector2Int(x, y));
                if (path.Count > 0)
                {
                    pathIndex = 0;
                    wandering = true;
                    return;
                }
            }
        }
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
