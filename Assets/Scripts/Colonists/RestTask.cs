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

    public RestTask(Bed bed, float restTime) : base(Vector2.zero)
    {
        this.bed = bed;
        this.restTime = restTime;
        if (bed != null)
            bed.Reserved = true;
    }
}
