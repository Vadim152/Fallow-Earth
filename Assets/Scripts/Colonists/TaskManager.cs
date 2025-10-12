using System;
using System.Collections.Generic;
using UnityEngine;
using FallowEarth.ResourcesSystem;

public class TaskManager : MonoBehaviour
{
    private readonly Dictionary<TaskPriority, Queue<Task>> priorityQueues = new Dictionary<TaskPriority, Queue<Task>>();
    private readonly List<TaskPriority> orderedPriorities = new List<TaskPriority>();

    [SerializeField]
    private List<ColonistRoleProfile> availableRoles = new List<ColonistRoleProfile>();

    void Awake()
    {
        foreach (TaskPriority priority in Enum.GetValues(typeof(TaskPriority)))
        {
            priorityQueues[priority] = new Queue<Task>();
            orderedPriorities.Add(priority);
        }
        orderedPriorities.Sort((a, b) => a.CompareTo(b));

        if (availableRoles == null || availableRoles.Count == 0)
        {
            availableRoles = new List<ColonistRoleProfile>(ColonistRoleLibrary.DefaultRoles);
        }
    }

    public IReadOnlyList<ColonistRoleProfile> AvailableRoles => availableRoles;

    public void AddTask(Task task)
    {
        if (task == null)
            return;

        if (!priorityQueues.TryGetValue(task.Priority, out Queue<Task> queue))
        {
            queue = new Queue<Task>();
            priorityQueues[task.Priority] = queue;
        }
        queue.Enqueue(task);
    }

    public void AddTasks(IEnumerable<Task> tasks)
    {
        if (tasks == null)
            return;
        foreach (var task in tasks)
            AddTask(task);
    }

    public Task GetNextTask(Colonist colonist)
    {
        ColonistScheduleActivityMask activityMask = ColonistScheduleActivityMask.Work;
        if (colonist != null)
        {
            activityMask = colonist.GetCurrentScheduleMask();
        }

        foreach (var priority in orderedPriorities)
        {
            if (!priorityQueues.TryGetValue(priority, out Queue<Task> queue) || queue.Count == 0)
                continue;

            int count = queue.Count;
            for (int i = 0; i < count; i++)
            {
                var task = queue.Dequeue();
                if (TaskMatchesColonist(task, colonist, activityMask))
                    return task;
                queue.Enqueue(task);
            }
        }

        if (StockpileZone.HasAny)
        {
            var items = ResourceLogisticsManager.GetTrackedItems();
            foreach (var item in items)
            {
                if (item == null || item.Reserved)
                    continue;

                if (ResourceLogisticsManager.RouteGraph != null &&
                    ResourceLogisticsManager.RouteGraph.TryFindBestZone(item.Stack, item.transform.position, out var zone))
                {
                    Vector2Int target = zone.GetClosestCellTo(item.transform.position);
                    bool canWorkNow = colonist == null || (activityMask & ColonistScheduleActivityMask.Work) != 0;
                    if (canWorkNow && (colonist == null || colonist.IsJobAllowed(JobType.Haul)))
                    {
                        var haul = new HaulLogTask(item, target);
                        haul.WithPriority(TaskPriority.High);
                        return haul;
                    }
                }
            }
        }

        return null;
    }

    bool TaskMatchesColonist(Task task, Colonist colonist, ColonistScheduleActivityMask activityMask)
    {
        if (task == null)
            return false;

        if ((task.AllowedSchedule & activityMask) == 0)
            return false;

        if (colonist == null)
            return true;

        if (task.RequiredJob.HasValue && !colonist.IsJobAllowed(task.RequiredJob.Value))
            return false;

        if (task.PreferredRole != null && colonist.RoleProfile != null && task.PreferredRole.RoleName != colonist.RoleProfile.RoleName)
            return false;

        if (task.RequiredJob == JobType.Doctor && !colonist.HealthSystemNeedsMedicalAttention())
            return false;

        return true;
    }
}
