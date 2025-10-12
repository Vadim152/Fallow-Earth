using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ColonistTrait
{
    [SerializeField] private string traitName;
    [SerializeField] private string description;
    [SerializeField] private float moodModifier;
    [SerializeField] private List<NeedEffect> needEffects = new List<NeedEffect>();

    [Serializable]
    public struct NeedEffect
    {
        public NeedType type;
        public float multiplier;
        public float expectationOffset;
    }

    public string Name => traitName;
    public string Description => description;
    public float MoodModifier => moodModifier;
    public IReadOnlyList<NeedEffect> NeedEffects => needEffects;

    public ColonistTrait(string name, string description, float moodModifier)
    {
        traitName = name;
        this.description = description;
        this.moodModifier = moodModifier;
    }

    public void AddNeedEffect(NeedType type, float multiplier, float expectationOffset)
    {
        needEffects.Add(new NeedEffect
        {
            type = type,
            multiplier = multiplier,
            expectationOffset = expectationOffset
        });
    }
}
