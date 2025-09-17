using UnityEngine;

public class TimedTask : Task
{
    public float duration;

    public TimedTask(Vector2 target, float duration, System.Action<Colonist> onComplete = null, JobType? requiredJob = null)
        : base(target, onComplete, requiredJob)
    {
        this.duration = duration;
    }
}
