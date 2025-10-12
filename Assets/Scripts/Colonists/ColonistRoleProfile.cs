using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ColonistRoleProfile
{
    [SerializeField] private string roleName;
    [SerializeField] private List<JobType> allowedJobs = new List<JobType>();
    [SerializeField] private ColonistSchedule scheduleTemplate = new ColonistSchedule();
    [SerializeField] private TaskPriority defaultPriority = TaskPriority.Normal;
    [SerializeField] private Dictionary<JobType, TaskPriority> jobPriorityOverrides = new Dictionary<JobType, TaskPriority>();

    public string RoleName => roleName;
    public ColonistSchedule ScheduleTemplate => scheduleTemplate;
    public TaskPriority DefaultPriority => defaultPriority;
    public IReadOnlyCollection<JobType> AllowedJobs => allowedJobs;
    public IReadOnlyDictionary<JobType, TaskPriority> JobPriorityOverrides => jobPriorityOverrides;

    public ColonistRoleProfile(string name)
    {
        roleName = name;
    }

    public ColonistSchedule CreateSchedule()
    {
        return scheduleTemplate != null ? scheduleTemplate.Clone() : new ColonistSchedule();
    }

    public void ConfigureSchedule(Action<ColonistSchedule> configure)
    {
        if (configure == null)
            return;
        if (scheduleTemplate == null)
            scheduleTemplate = new ColonistSchedule();
        configure(scheduleTemplate);
    }

    public TaskPriority GetPriorityForJob(JobType job)
    {
        if (jobPriorityOverrides != null && jobPriorityOverrides.TryGetValue(job, out TaskPriority value))
            return value;
        return defaultPriority;
    }

    public void AllowJob(JobType job)
    {
        if (!allowedJobs.Contains(job))
            allowedJobs.Add(job);
    }

    public void SetPriority(JobType job, TaskPriority priority)
    {
        jobPriorityOverrides[job] = priority;
    }
}
