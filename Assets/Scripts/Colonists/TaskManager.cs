using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    private Queue<Task> tasks = new Queue<Task>();

    public void AddTask(Task task)
    {
        tasks.Enqueue(task);
    }

    public Task GetNextTask(Colonist colonist)
    {
        if (colonist != null && tasks.Count > 0)
        {
            int count = tasks.Count;
            for (int i = 0; i < count; i++)
            {
                var task = tasks.Dequeue();
                if (!task.RequiredJob.HasValue || colonist.IsJobAllowed(task.RequiredJob.Value))
                    return task;
                tasks.Enqueue(task);
            }
        }
        else if (tasks.Count > 0)
        {
            return tasks.Dequeue();
        }

        if (StockpileZone.HasAny)
        {
            var logs = GameObject.FindObjectsOfType<WoodLog>();
            foreach (var l in logs)
            {
                if (l != null && !l.Reserved)
                {
                    Vector2Int target = StockpileZone.GetClosestCell(l.transform.position);
                    if (colonist == null || colonist.IsJobAllowed(JobType.Haul))
                        return new HaulLogTask(l, target);
                }
            }
        }

        return null;
    }
}
