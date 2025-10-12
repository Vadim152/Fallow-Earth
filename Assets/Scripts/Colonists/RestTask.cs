using UnityEngine;

/// <summary>
/// Task for moving to a bed and resting to recover fatigue.
/// </summary>
public class RestTask : Task
{
    public enum Stage { MoveToBed, Rest }

    public Bed bed;
    public Stage stage = Stage.MoveToBed;
    public float restTime;

    public RestTask(Bed bed, float restTime) : base(Vector2.zero, null, JobType.Rest, TaskPriority.High,
        ColonistScheduleActivityMask.Sleep | ColonistScheduleActivityMask.Medical)
    {
        this.bed = bed;
        this.restTime = restTime;
        if (bed != null)
            bed.Reserved = true;
    }

    public void ReleaseReservation()
    {
        if (bed != null)
        {
            bed.Reserved = false;
        }
        bed = null;
    }
}
