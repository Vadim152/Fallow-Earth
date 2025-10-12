using System;
using System.Collections.Generic;
using FallowEarth.Balance;
using FallowEarth.Saving;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ColonistNeeds))]
[RequireComponent(typeof(ColonistAIModule))]
[RequireComponent(typeof(ColonistSocial))]
[RequireComponent(typeof(ColonistPersistence))]
public class Colonist : SaveableMonoBehaviour
{
    public float moveSpeed = 10f;
    private float baseMoveSpeed;

    [Range(0f, 1f)] public float mood = 0.75f;
    [Range(0f, 1f)] public float health = 1f;
    [HideInInspector] public string activity = "Idle";
    [Range(0f, 1f)] public float hunger;
    [Range(0f, 1f)] public float fatigue;
    [Range(0f, 1f)] public float stress;
    [Range(0f, 1f)] public float social;

    [SerializeField] private ColonistRoleProfile roleProfile;

    public HashSet<JobType> jobPriorities = new HashSet<JobType>();

    private ColonistNeeds needsModule;
    private ColonistAIModule aiModule;
    private ColonistSocial socialModule;
    private ColonistPersistence persistenceModule;
    private ColonistSchedule schedule;
    private Rigidbody2D rb;

    private bool mentalBreak;
    private float breakTimer;
    public float breakDuration = 8f;

    public bool IsBusy => aiModule != null && aiModule.IsBusy;
    public ColonistRoleProfile RoleProfile => roleProfile;
    public ColonistSchedule Schedule => schedule;
    public NeedTracker Needs => needsModule?.Tracker;

    public override SaveCategory Category => SaveCategory.Creature;

    public void SatisfyNeed(NeedType type, float amount)
    {
        needsModule?.Satisfy(type, amount);
        SyncNeedFields();
    }

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        baseMoveSpeed = moveSpeed;

        needsModule = GetComponent<ColonistNeeds>();
        aiModule = GetComponent<ColonistAIModule>();
        socialModule = GetComponent<ColonistSocial>();
        persistenceModule = GetComponent<ColonistPersistence>();

        aiModule.Configure(rb, baseMoveSpeed);

        if (GetComponent<SpriteRenderer>() == null)
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateColoredSprite(Color.yellow);
        }

        mentalBreak = false;
        breakTimer = 0f;

        needsModule.InitializeIfNeeded();
        needsModule.NeedValueChanged += HandleNeedChanged;

        InitializeRoleProfile();
        SyncNeedFields();
        UpdateMoodAndHealth();
        health = Mathf.Clamp01(needsModule.HealthSystem.OverallHealth);
    }

    void Start()
    {
        if (schedule == null)
            schedule = roleProfile != null ? roleProfile.CreateSchedule() : new ColonistSchedule();
    }

    void OnDestroy()
    {
        if (needsModule != null)
            needsModule.NeedValueChanged -= HandleNeedChanged;
    }

    void HandleNeedChanged(NeedType type, float value)
    {
        switch (type)
        {
            case NeedType.Hunger:
                hunger = value;
                break;
            case NeedType.Rest:
                fatigue = value;
                break;
            case NeedType.Stress:
                stress = value;
                break;
            case NeedType.Social:
                social = Mathf.Clamp01(1f - value);
                break;
        }
    }

    void SyncNeedFields()
    {
        if (needsModule == null)
            return;
        hunger = needsModule.GetValue(NeedType.Hunger);
        fatigue = needsModule.GetValue(NeedType.Rest);
        stress = needsModule.GetValue(NeedType.Stress);
        social = Mathf.Clamp01(1f - needsModule.GetValue(NeedType.Social));
    }

    void InitializeRoleProfile()
    {
        jobPriorities.Clear();
        if (roleProfile == null)
        {
            var defaults = ColonistRoleLibrary.DefaultRoles;
            if (defaults != null && defaults.Count > 0)
                roleProfile = defaults[UnityEngine.Random.Range(0, defaults.Count)];
        }

        schedule = roleProfile != null ? roleProfile.CreateSchedule() : new ColonistSchedule();

        if (roleProfile != null)
        {
            foreach (var job in roleProfile.AllowedJobs)
                jobPriorities.Add(job);
        }
        else
        {
            foreach (JobType jt in Enum.GetValues(typeof(JobType)))
                jobPriorities.Add(jt);
        }
        EnsureEssentialJobs();
    }

    void EnsureEssentialJobs()
    {
        jobPriorities.Add(JobType.Rest);
        jobPriorities.Add(JobType.Social);
    }

    void UpdateMoodAndHealth()
    {
        float moodPenalty = needsModule != null && needsModule.Tracker != null ? needsModule.Tracker.GetMoodImpact() : 0f;
        float traitModifier = needsModule != null ? needsModule.TraitMoodModifier : 0f;
        float relationshipMood = socialModule != null ? socialModule.GetAverageMoodModifier() : 0f;
        mood = Mathf.Clamp01(0.65f - moodPenalty + traitModifier + relationshipMood);
        health = needsModule != null ? Mathf.Clamp01(needsModule.HealthSystem.OverallHealth) : health;
    }

    public void SetJobAllowed(JobType job, bool allowed)
    {
        if (allowed)
            jobPriorities.Add(job);
        else
            jobPriorities.Remove(job);
    }

    public bool IsJobAllowed(JobType job) => jobPriorities.Contains(job);

    public ColonistScheduleActivityMask GetCurrentScheduleMask()
    {
        int hour = 0;
        if (DayNightCycle.Instance != null)
            hour = DayNightCycle.Instance.CurrentHourInt;
        else
            hour = Mathf.FloorToInt((Time.time / 60f) % 24f);

        if (schedule == null)
            schedule = roleProfile != null ? roleProfile.CreateSchedule() : new ColonistSchedule();
        return schedule.ToMask(hour);
    }

    public void AssignRoleProfile(ColonistRoleProfile profile, bool overrideJobs = true)
    {
        roleProfile = profile;
        schedule = roleProfile != null ? roleProfile.CreateSchedule() : schedule ?? new ColonistSchedule();
        if (!overrideJobs)
            return;

        jobPriorities.Clear();
        if (roleProfile != null)
        {
            foreach (var job in roleProfile.AllowedJobs)
                jobPriorities.Add(job);
        }
        else
        {
            foreach (JobType jt in Enum.GetValues(typeof(JobType)))
                jobPriorities.Add(jt);
        }
        EnsureEssentialJobs();
    }

    public bool HealthSystemNeedsMedicalAttention()
    {
        return needsModule != null && needsModule.NeedsMedicalAttention();
    }

    public void SetTask(Task task) => aiModule?.SetTask(task);

    public bool TryAssignTask(Task task) => aiModule != null && aiModule.TryAssignTask(task);

    public void CancelTasks() => aiModule?.CancelTasks();

    public SocialRelationship EnsureRelationship(Colonist other)
    {
        return socialModule != null ? socialModule.EnsureRelationship(other) : null;
    }

    void BeginMentalBreak(string reason)
    {
        if (mentalBreak)
            return;
        mentalBreak = true;
        breakTimer = breakDuration;
        CancelTasks();
        aiModule?.StartWander();
        activity = "Panicking";
        needsModule?.Tracker?.AddStress(NeedType.Stress, 0.2f);
        EventConsole.Log("Mental", string.IsNullOrEmpty(reason) ? $"{name} впадает в паническое состояние." : $"{name} впадает в паническое состояние ({reason}).");
    }

    void Update()
    {
        float dt = Time.deltaTime;
        needsModule?.Tick(dt, aiModule != null ? aiModule.CurrentTask : null, activity);
        UpdateMoodAndHealth();

        if (!mentalBreak)
        {
            bool triggerBreak = false;
            string reason = string.Empty;
            var balance = GameBalanceManager.Instance;
            if (balance != null && balance.ShouldEnterMentalBreak(mood, stress, dt))
            {
                triggerBreak = true;
                reason = $"баланс: настроение {mood:0.00}, стресс {stress:0.00}";
            }
            else if (mood < 0.2f || stress > 0.95f)
            {
                triggerBreak = true;
                reason = $"критические показатели: настроение {mood:0.00}, стресс {stress:0.00}";
            }

            if (triggerBreak)
                BeginMentalBreak(reason);
        }

        if (mentalBreak)
        {
            breakTimer -= dt;
            stress = Mathf.Clamp01(stress - dt / breakDuration);
            if (aiModule != null && !aiModule.HasPath)
                aiModule.StartWander();
            if (breakTimer <= 0f && mood >= 0.3f && stress <= 0.8f)
            {
                mentalBreak = false;
                CancelTasks();
                activity = "Idle";
                EventConsole.Log("Mental", $"{name} восстанавливает контроль над собой.");
            }
            aiModule?.MoveAlongPath(dt);
            return;
        }

        aiModule?.Tick(dt, true);
    }

    public override void PopulateSaveData(SaveData saveData)
    {
        persistenceModule?.PopulateSaveData(saveData, jobPriorities, schedule, roleProfile);
    }

    public override void LoadFromSaveData(SaveData saveData)
    {
        var previousSchedule = schedule;
        var previousRole = roleProfile;
        if (persistenceModule != null && persistenceModule.LoadFromSaveData(saveData, jobPriorities, ref previousSchedule, ref previousRole))
        {
            schedule = previousSchedule;
            roleProfile = previousRole;
            EnsureEssentialJobs();
            SyncNeedFields();
            UpdateMoodAndHealth();
        }

        CancelTasks();
        mentalBreak = false;
        breakTimer = 0f;
        activity = activity ?? "Idle";
    }

    Sprite CreateColoredSprite(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }
}
