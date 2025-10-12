using UnityEngine;
using FallowEarth.Balance;

public class NeedState
{
    public NeedDefinition Definition { get; }
    public float Value { get; private set; }

    private float pendingDelta;

    public NeedState(NeedDefinition definition, float initialValue)
    {
        Definition = definition;
        Value = Mathf.Clamp(initialValue, definition.MinValue, definition.MaxValue);
    }

    public void Tick(float deltaTime, float traitMultiplier = 1f)
    {
        float perSecond = Definition.IncreasePerHour / 3600f;
        float naturalRecovery = Definition.NaturalRecoveryPerHour / 3600f;
        float hour = DayNightCycle.Instance != null ? DayNightCycle.Instance.CurrentHour : 12f;
        if (GameBalanceManager.Instance != null)
        {
            perSecond *= GameBalanceManager.Instance.EvaluateNeedPressure(Definition.Type, hour);
            naturalRecovery *= GameBalanceManager.Instance.EvaluateNeedRecovery(Definition.Type, hour);
        }
        float change = (perSecond * traitMultiplier - naturalRecovery) * deltaTime;
        ApplyDelta(change + pendingDelta);
        pendingDelta = 0f;
    }

    public void Satisfy(float amount)
    {
        if (amount <= 0f)
            return;
        ApplyDelta(-amount);
    }

    public void AddStress(float amount)
    {
        pendingDelta += amount;
    }

    public float GetMoodImpact(float expectationModifier = 0f)
    {
        float expectation = Mathf.Clamp01(Definition.Expectation + expectationModifier);
        return (Value - expectation) * Definition.MoodWeight;
    }

    private void ApplyDelta(float delta)
    {
        Value = Mathf.Clamp(Value + delta, Definition.MinValue, Definition.MaxValue);
    }
}
