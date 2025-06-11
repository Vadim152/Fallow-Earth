using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    private Queue<Task> tasks = new Queue<Task>();

    public void AddTask(Task task)
    {
        tasks.Enqueue(task);
    }

    public Task GetNextTask()
    {
        if (tasks.Count > 0)
            return tasks.Dequeue();

        if (StockpileZone.HasAny)
        {
            var logs = GameObject.FindObjectsOfType<WoodLog>();
            foreach (var l in logs)
            {
                if (l != null && !l.Reserved)
                {
                    Vector2Int target = StockpileZone.GetClosestCell(l.transform.position);
                    return new HaulLogTask(l, target);
                }
            }
        }

        return null;
    }
}
