using System;
using System.Collections.Generic;

public static class ColonistRoleLibrary
{
    public static IReadOnlyList<ColonistRoleProfile> DefaultRoles { get; private set; }

    static ColonistRoleLibrary()
    {
        var roles = new List<ColonistRoleProfile>();

        var generalist = new ColonistRoleProfile("Generalist");
        foreach (JobType job in Enum.GetValues(typeof(JobType)))
            generalist.AllowJob(job);
        generalist.SetPriority(JobType.Haul, TaskPriority.High);
        generalist.ConfigureSchedule(schedule =>
        {
            schedule.SetRange(0, 6, ColonistScheduleActivity.Sleep);
            schedule.SetRange(6, 22, ColonistScheduleActivity.Work);
            schedule.SetRange(22, 24, ColonistScheduleActivity.Recreation);
        });
        roles.Add(generalist);

        var builder = new ColonistRoleProfile("Builder");
        builder.AllowJob(JobType.Build);
        builder.AllowJob(JobType.Haul);
        builder.AllowJob(JobType.Doctor);
        builder.SetPriority(JobType.Build, TaskPriority.Critical);
        builder.SetPriority(JobType.Haul, TaskPriority.High);
        builder.ConfigureSchedule(schedule =>
        {
            schedule.SetRange(0, 7, ColonistScheduleActivity.Sleep);
            schedule.SetRange(7, 19, ColonistScheduleActivity.Work);
            schedule.SetRange(19, 24, ColonistScheduleActivity.Recreation);
        });
        roles.Add(builder);

        var laborer = new ColonistRoleProfile("Laborer");
        laborer.AllowJob(JobType.Haul);
        laborer.AllowJob(JobType.Chop);
        laborer.SetPriority(JobType.Chop, TaskPriority.High);
        laborer.ConfigureSchedule(schedule =>
        {
            schedule.SetRange(0, 8, ColonistScheduleActivity.Sleep);
            schedule.SetRange(8, 20, ColonistScheduleActivity.Work);
            schedule.SetRange(20, 24, ColonistScheduleActivity.Recreation);
        });
        roles.Add(laborer);

        DefaultRoles = roles;
    }
}
