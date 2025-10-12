using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ColonistHealth
{
    [Serializable]
    public class BodyPart
    {
        public string name;
        public float maxHealth;
        public float currentHealth;
        public bool vital;
        public List<Injury> injuries = new List<Injury>();

        public BodyPart(string name, float maxHealth, bool vital)
        {
            this.name = name;
            this.maxHealth = Mathf.Max(1f, maxHealth);
            this.currentHealth = this.maxHealth;
            this.vital = vital;
        }

        public float HealthPercent => Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
    }

    [Serializable]
    public class Injury
    {
        public string name;
        public float severity;
        public float bleedRate;
        public float pain;
        public bool permanent;

        public bool CausesBleeding => bleedRate > 0f && severity > 0f;
    }

    [Serializable]
    public class Disease
    {
        public string name;
        public float progress;
        public float lethality;
        public float recoveryRate;
    }

    [SerializeField] private List<BodyPart> bodyParts = new List<BodyPart>();
    [SerializeField] private List<Disease> diseases = new List<Disease>();

    public IReadOnlyList<BodyPart> BodyParts => bodyParts;
    public IReadOnlyList<Disease> Diseases => diseases;

    public float BleedSeverity { get; private set; }
    public bool IsBleeding => BleedSeverity > 0f;

    public float OverallHealth
    {
        get
        {
            if (bodyParts.Count == 0)
                return 1f;
            float total = 0f;
            foreach (var part in bodyParts)
                total += part.HealthPercent * (part.vital ? 2f : 1f);
            return Mathf.Clamp01(total / (bodyParts.Count + VitalCount));
        }
    }

    private int VitalCount
    {
        get
        {
            int count = 0;
            foreach (var part in bodyParts)
                if (part.vital)
                    count++;
            return Mathf.Max(1, count);
        }
    }

    public void InitializeDefaults()
    {
        bodyParts.Clear();
        bodyParts.Add(new BodyPart("Head", 50f, true));
        bodyParts.Add(new BodyPart("Torso", 70f, true));
        bodyParts.Add(new BodyPart("Left Arm", 40f, false));
        bodyParts.Add(new BodyPart("Right Arm", 40f, false));
        bodyParts.Add(new BodyPart("Left Leg", 45f, false));
        bodyParts.Add(new BodyPart("Right Leg", 45f, false));
    }

    public void ApplyDamage(string partName, float amount, float bleedRate = 0f)
    {
        BodyPart part = bodyParts.Find(p => p.name == partName);
        if (part == null)
            return;

        part.currentHealth = Mathf.Max(0f, part.currentHealth - Mathf.Max(0f, amount));
        if (bleedRate > 0f)
        {
            var injury = new Injury { name = "Wound", severity = Mathf.Clamp01(amount / Mathf.Max(1f, part.maxHealth)), bleedRate = bleedRate, pain = Mathf.Clamp01(amount / 40f) };
            part.injuries.Add(injury);
        }
        RecalculateBleeding();
    }

    public void TreatInjury(BodyPart part, Injury injury, float quality)
    {
        if (part == null || injury == null)
            return;
        injury.severity = Mathf.Max(0f, injury.severity - quality);
        injury.bleedRate = Mathf.Max(0f, injury.bleedRate - quality * 0.5f);
        if (injury.severity <= 0.01f)
            part.injuries.Remove(injury);
        RecalculateBleeding();
    }

    public void ApplyTreatment(float restEffect, float medicationPotency)
    {
        foreach (var part in bodyParts)
        {
            float heal = restEffect * (part.vital ? 0.6f : 0.4f);
            part.currentHealth = Mathf.Min(part.maxHealth, part.currentHealth + heal);
            for (int i = part.injuries.Count - 1; i >= 0; i--)
            {
                part.injuries[i].severity = Mathf.Max(0f, part.injuries[i].severity - medicationPotency * 0.5f);
                part.injuries[i].bleedRate = Mathf.Max(0f, part.injuries[i].bleedRate - medicationPotency * 0.2f);
                if (part.injuries[i].severity <= 0.01f)
                    part.injuries.RemoveAt(i);
            }
        }
        RecalculateBleeding();
    }

    public void Tick(float deltaTime)
    {
        float bleed = 0f;
        foreach (var part in bodyParts)
        {
            for (int i = part.injuries.Count - 1; i >= 0; i--)
            {
                var injury = part.injuries[i];
                if (!injury.permanent)
                    injury.severity = Mathf.Max(0f, injury.severity - deltaTime / 600f);
                bleed += injury.bleedRate;
            }
        }

        BleedSeverity = Mathf.Max(0f, bleed);
    }

    private void RecalculateBleeding()
    {
        float bleed = 0f;
        foreach (var part in bodyParts)
            foreach (var injury in part.injuries)
                bleed += injury.bleedRate;
        BleedSeverity = bleed;
    }
}
