using UnityEngine;

/// <summary>
/// Task for moving to another colonist and socializing to fill the social need.
/// </summary>
public class SocializeTask : TimedTask
{
    public Colonist partner;

    /// <summary>
    /// Create a socialize task with a partner. The colonist will move to the
    /// supplied meeting point and spend the given duration talking.
    /// </summary>
    public SocializeTask(Colonist partner, Vector2 meetingPoint, float duration,
        System.Action<Colonist> onComplete = null)
        : base(meetingPoint, duration, onComplete, JobType.Social, TaskPriority.Normal,
            ColonistScheduleActivityMask.Recreation)
    {
        this.partner = partner;
    }
}
