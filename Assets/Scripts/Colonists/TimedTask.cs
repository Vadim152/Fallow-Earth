using UnityEngine;

public class TimedTask : Task
{
    public float duration;

    public TimedTask(Vector2 target, float duration, System.Action<Colonist> onComplete = null) : base(target, onComplete)
    {
        this.duration = duration;
    }
}
