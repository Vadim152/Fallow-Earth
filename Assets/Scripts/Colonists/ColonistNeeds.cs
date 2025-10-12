using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Colonist))]
public class ColonistNeeds : MonoBehaviour
{
    [SerializeField]
    private List<ColonistTrait> traits = new List<ColonistTrait>();

    private NeedTracker tracker;
    private float traitMoodModifier;
    private ColonistHealth healthSystem = new ColonistHealth();

    public event Action<NeedType, float> NeedValueChanged;

    public NeedTracker Tracker => tracker;
    public IReadOnlyList<ColonistTrait> Traits => traits;
    public float TraitMoodModifier => traitMoodModifier;
    public ColonistHealth HealthSystem => healthSystem;

    public void InitializeIfNeeded()
    {
        if (tracker != null)
            return;

        tracker = new NeedTracker();
        RegisterDefaultNeeds();
        tracker.OnNeedChanged += HandleNeedChanged;
        healthSystem.InitializeDefaults();
        ApplyTraits();
    }

    void OnEnable()
    {
        InitializeIfNeeded();
    }

    void OnDisable()
    {
        if (tracker != null)
            tracker.OnNeedChanged -= HandleNeedChanged;
    }

    public void ResetTraitEffects()
    {
        ApplyTraits();
    }

    public void Tick(float deltaTime, Task currentTask, string activity)
    {
        InitializeIfNeeded();
        if (tracker == null)
            return;

        tracker.Tick(deltaTime);
        healthSystem.Tick(deltaTime);

        if (healthSystem.IsBleeding)
        {
            tracker.AddStress(NeedType.Medical, healthSystem.BleedSeverity * deltaTime * 0.08f);
            tracker.AddStress(NeedType.Stress, healthSystem.BleedSeverity * deltaTime * 0.05f);
        }

        if (currentTask != null && !(currentTask is RestTask) && !(currentTask is SocializeTask) && !(currentTask is RestOnGroundTask))
            tracker.AddStress(NeedType.Stress, deltaTime * 0.12f);
        else
            Satisfy(NeedType.Stress, deltaTime * 0.08f);

        if (activity == "Resting")
        {
            tracker.Satisfy(NeedType.Rest, deltaTime * 0.12f);
            tracker.Satisfy(NeedType.Stress, deltaTime * 0.06f);
        }
        else if (activity == "Talking")
        {
            tracker.Satisfy(NeedType.Social, deltaTime * 0.15f);
            tracker.Satisfy(NeedType.Stress, deltaTime * 0.04f);
        }

        if (NeedsMedicalAttention())
            tracker.AddStress(NeedType.Medical, deltaTime * 0.05f);

    }

    public bool NeedsMedicalAttention()
    {
        if (healthSystem == null)
            return false;
        if (healthSystem.IsBleeding || healthSystem.OverallHealth < 0.6f)
            return true;
        return tracker != null && tracker.GetValue(NeedType.Medical) > 0.5f;
    }

    public void ApplyTreatment(float restEffect, float medicationPotency)
    {
        healthSystem.ApplyTreatment(restEffect, medicationPotency);
    }

    public void Satisfy(NeedType type, float amount)
    {
        tracker?.Satisfy(type, amount);
    }

    public float GetValue(NeedType type)
    {
        if (tracker == null)
            return 0f;
        return tracker.GetValue(type);
    }

    public void RestoreSnapshot(Dictionary<NeedType, float> snapshot)
    {
        InitializeIfNeeded();
        tracker.RestoreSnapshot(snapshot);
        foreach (var kvp in snapshot)
            NeedValueChanged?.Invoke(kvp.Key, kvp.Value);
    }

    public Dictionary<NeedType, float> CreateSnapshot()
    {
        InitializeIfNeeded();
        return tracker.CreateSnapshot();
    }

    public void SetTraitNames(IEnumerable<string> traitNames)
    {
        traits.Clear();
        if (traitNames != null)
        {
            foreach (var traitName in traitNames)
            {
                var trait = ColonistTraitLibrary.CreateTrait(traitName);
                if (trait != null)
                    traits.Add(trait);
            }
        }
        ApplyTraits();
    }

    public List<string> GetTraitNames()
    {
        var result = new List<string>();
        foreach (var trait in traits)
        {
            if (trait != null)
                result.Add(trait.Name);
        }
        return result;
    }

    void RegisterDefaultNeeds()
    {
        tracker.RegisterNeed(new NeedDefinition(NeedType.Hunger, 0.45f, 0.6f, 0.55f), UnityEngine.Random.Range(0.2f, 0.6f));
        tracker.RegisterNeed(new NeedDefinition(NeedType.Rest, 0.35f, 0.5f, 0.45f), UnityEngine.Random.Range(0.1f, 0.5f));
        tracker.RegisterNeed(new NeedDefinition(NeedType.Recreation, 0.3f, 0.25f, 0.25f), UnityEngine.Random.Range(0.2f, 0.5f));
        tracker.RegisterNeed(new NeedDefinition(NeedType.Social, 0.25f, 0.3f, 0.2f), UnityEngine.Random.Range(0.15f, 0.4f));
        tracker.RegisterNeed(new NeedDefinition(NeedType.Stress, 0.3f, 0.4f, 0.2f, 0.05f), UnityEngine.Random.Range(0.1f, 0.3f));
        tracker.RegisterNeed(new NeedDefinition(NeedType.Comfort, 0.25f, 0.2f, 0.15f), UnityEngine.Random.Range(0.2f, 0.6f));
        tracker.RegisterNeed(new NeedDefinition(NeedType.Medical, 0.1f, 0.5f, 0.02f), 0f);
    }

    void ApplyTraits()
    {
        traitMoodModifier = 0f;
        if (tracker == null)
            return;
        tracker.ResetModifiers();

        if (traits.Count == 0)
        {
            var randomTrait = ColonistTraitLibrary.GetRandomTrait();
            if (randomTrait != null)
                traits.Add(randomTrait);
        }

        foreach (var trait in traits)
        {
            if (trait == null)
                continue;
            traitMoodModifier += trait.MoodModifier;
            foreach (var effect in trait.NeedEffects)
            {
                if (effect.multiplier > 0f && !Mathf.Approximately(effect.multiplier, 1f))
                    tracker.MultiplyTraitMultiplier(effect.type, effect.multiplier);
                if (Mathf.Abs(effect.expectationOffset) > 0.001f)
                    tracker.AddExpectationModifier(effect.type, effect.expectationOffset);
            }
        }
    }

    void HandleNeedChanged(NeedType type, float value)
    {
        NeedValueChanged?.Invoke(type, value);
    }
}
