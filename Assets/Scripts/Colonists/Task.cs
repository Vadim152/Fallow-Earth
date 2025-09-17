using UnityEngine;

public class Task
{
    public Vector2 target;
    public System.Action<Colonist> onComplete;
    public JobType? RequiredJob { get; private set; }

    public Task(Vector2 target, System.Action<Colonist> onComplete = null, JobType? requiredJob = null)
    {
        this.target = target;
        this.onComplete = onComplete;
        RequiredJob = requiredJob;
    }

    public void Complete(Colonist c)
    {
        if (onComplete != null)
            onComplete(c);
    }
}
