using System;
using UnityEngine;

[Serializable]
public class NeedDefinition
{
    [SerializeField] private NeedType type;
    [SerializeField, Range(0f, 1f)] private float expectation = 0.4f;
    [SerializeField, Range(0f, 2f)] private float moodWeight = 0.5f;
    [SerializeField, Range(0f, 1f)] private float minValue;
    [SerializeField, Range(0f, 1f)] private float maxValue = 1f;
    [SerializeField] private float increasePerHour = 0.1f;
    [SerializeField] private float naturalRecoveryPerHour;

    public NeedType Type => type;
    public float Expectation => expectation;
    public float MoodWeight => moodWeight;
    public float IncreasePerHour => increasePerHour;
    public float NaturalRecoveryPerHour => naturalRecoveryPerHour;
    public float MinValue => minValue;
    public float MaxValue => Mathf.Max(maxValue, minValue);

    public NeedDefinition(NeedType type, float expectation, float moodWeight, float increasePerHour, float naturalRecoveryPerHour = 0f)
    {
        this.type = type;
        this.expectation = Mathf.Clamp01(expectation);
        this.moodWeight = Mathf.Max(0f, moodWeight);
        this.increasePerHour = Mathf.Max(0f, increasePerHour);
        this.naturalRecoveryPerHour = Mathf.Max(0f, naturalRecoveryPerHour);
        minValue = 0f;
        maxValue = 1f;
    }
}
