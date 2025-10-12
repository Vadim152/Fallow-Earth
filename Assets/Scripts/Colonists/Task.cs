using UnityEngine;

public class Task
{
    public Vector2 target;
    public System.Action<Colonist> onComplete;
    public JobType? RequiredJob { get; private set; }
    public TaskPriority Priority { get; private set; }
    public ColonistScheduleActivityMask AllowedSchedule { get; private set; }
    public ColonistRoleProfile PreferredRole { get; private set; }

    public Task(Vector2 target, System.Action<Colonist> onComplete = null, JobType? requiredJob = null,
        TaskPriority priority = TaskPriority.Normal,
        ColonistScheduleActivityMask allowedSchedule = ColonistScheduleActivityMask.Any,
        ColonistRoleProfile preferredRole = null)
    {
        this.target = target;
        this.onComplete = onComplete;
        RequiredJob = requiredJob;
        Priority = priority;
        AllowedSchedule = allowedSchedule;
        PreferredRole = preferredRole;
    }

    public void Complete(Colonist c)
    {
        if (onComplete != null)
            onComplete(c);
    }

    public Task WithPriority(TaskPriority priority)
    {
        Priority = priority;
        return this;
    }

    public Task WithScheduleMask(ColonistScheduleActivityMask mask)
    {
        AllowedSchedule = mask;
        return this;
    }

    public Task WithPreferredRole(ColonistRoleProfile profile)
    {
        PreferredRole = profile;
        return this;
    }
}
