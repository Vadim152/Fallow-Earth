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
        if (tasks.Count == 0)
            return null;
        return tasks.Dequeue();
    }
}
