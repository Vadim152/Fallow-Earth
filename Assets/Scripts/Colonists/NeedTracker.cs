using System;
using System.Collections.Generic;
using UnityEngine;

public class NeedTracker
{
    private readonly Dictionary<NeedType, NeedState> needs = new Dictionary<NeedType, NeedState>();
    private readonly Dictionary<NeedType, float> traitMultipliers = new Dictionary<NeedType, float>();
    private readonly Dictionary<NeedType, float> expectationModifiers = new Dictionary<NeedType, float>();

    public event Action<NeedType, float> OnNeedChanged;

    public void RegisterNeed(NeedDefinition definition, float initialValue)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        needs[definition.Type] = new NeedState(definition, initialValue);
        traitMultipliers[definition.Type] = 1f;
        expectationModifiers[definition.Type] = 0f;
    }

    public void SetTraitMultiplier(NeedType type, float multiplier)
    {
        traitMultipliers[type] = Mathf.Max(0f, multiplier);
    }

    public void MultiplyTraitMultiplier(NeedType type, float multiplier)
    {
        if (!traitMultipliers.TryGetValue(type, out float current))
            current = 1f;
        traitMultipliers[type] = Mathf.Max(0f, current * Mathf.Max(0f, multiplier));
    }

    public void ResetModifiers()
    {
        foreach (var type in new List<NeedType>(needs.Keys))
        {
            traitMultipliers[type] = 1f;
            expectationModifiers[type] = 0f;
        }
    }

    public void AddExpectationModifier(NeedType type, float offset)
    {
        expectationModifiers[type] = offset;
    }

    public void Tick(float deltaTime)
    {
        foreach (var kvp in needs)
        {
            float traitMultiplier = traitMultipliers.TryGetValue(kvp.Key, out float m) ? m : 1f;
            float prev = kvp.Value.Value;
            kvp.Value.Tick(deltaTime, traitMultiplier);
            if (!Mathf.Approximately(prev, kvp.Value.Value))
                OnNeedChanged?.Invoke(kvp.Key, kvp.Value.Value);
        }
    }

    public float GetValue(NeedType type)
    {
        return needs.TryGetValue(type, out NeedState state) ? state.Value : 0f;
    }

    public NeedState GetState(NeedType type)
    {
        needs.TryGetValue(type, out NeedState state);
        return state;
    }

    public void Satisfy(NeedType type, float amount)
    {
        if (needs.TryGetValue(type, out NeedState state))
        {
            float before = state.Value;
            state.Satisfy(amount);
            if (!Mathf.Approximately(before, state.Value))
                OnNeedChanged?.Invoke(type, state.Value);
        }
    }

    public void AddStress(NeedType type, float amount)
    {
        if (needs.TryGetValue(type, out NeedState state))
        {
            float before = state.Value;
            state.AddStress(amount);
            if (!Mathf.Approximately(before, state.Value))
                OnNeedChanged?.Invoke(type, state.Value);
        }
    }

    public NeedType GetMostPressingNeed(out float value)
    {
        NeedType result = NeedType.Hunger;
        float bestValue = float.MinValue;
        foreach (var kvp in needs)
        {
            if (kvp.Value.Value > bestValue)
            {
                result = kvp.Key;
                bestValue = kvp.Value.Value;
            }
        }
        value = Mathf.Max(0f, bestValue);
        return result;
    }

    public float GetMoodImpact()
    {
        float total = 0f;
        foreach (var kvp in needs)
        {
            float offset = expectationModifiers.TryGetValue(kvp.Key, out float o) ? o : 0f;
            total += kvp.Value.GetMoodImpact(offset);
        }
        return total;
    }

    public Dictionary<NeedType, float> CreateSnapshot()
    {
        var snapshot = new Dictionary<NeedType, float>();
        foreach (var kvp in needs)
            snapshot[kvp.Key] = kvp.Value.Value;
        return snapshot;
    }

    public void RestoreSnapshot(Dictionary<NeedType, float> snapshot)
    {
        if (snapshot == null)
            return;
        foreach (var kvp in snapshot)
        {
            if (needs.TryGetValue(kvp.Key, out NeedState state))
            {
                state.Satisfy(state.Value - Mathf.Clamp01(kvp.Value));
                OnNeedChanged?.Invoke(kvp.Key, state.Value);
            }
        }
    }
}
