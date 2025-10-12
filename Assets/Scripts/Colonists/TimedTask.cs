using UnityEngine;

public class TimedTask : Task
{
    public float duration;

    public TimedTask(Vector2 target, float duration, System.Action<Colonist> onComplete = null, JobType? requiredJob = null,
        TaskPriority priority = TaskPriority.Normal, ColonistScheduleActivityMask allowedSchedule = ColonistScheduleActivityMask
            .Any)
        : base(target, onComplete, requiredJob, priority, allowedSchedule)
    {
        this.duration = duration;
    }
}
