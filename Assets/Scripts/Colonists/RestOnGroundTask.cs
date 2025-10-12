using System;
using UnityEngine;

/// <summary>
/// Timed rest action that lets a colonist recover fatigue without a bed.
/// </summary>
public class RestOnGroundTask : TimedTask
{
    public float fatigueRecoveryAmount { get; }
    public float stressReliefAmount { get; }

    public RestOnGroundTask(Vector2 target, float duration, float fatigueRecoveryAmount,
        float stressReliefAmount = 0f, Action<Colonist> onComplete = null)
        : base(target, duration, onComplete, JobType.Rest, TaskPriority.Low,
            ColonistScheduleActivityMask.Sleep | ColonistScheduleActivityMask.Recreation | ColonistScheduleActivityMask.Medical)
    {
        this.fatigueRecoveryAmount = Mathf.Max(0f, fatigueRecoveryAmount);
        this.stressReliefAmount = Mathf.Max(0f, stressReliefAmount);
    }
}
