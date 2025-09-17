using UnityEngine;

public class ChopTreeTask : TimedTask
{
    public ChopTreeTask(Vector2 target, float duration, System.Action<Colonist> onComplete = null)
        : base(target, duration, onComplete, JobType.Chop)
    {
    }
}
