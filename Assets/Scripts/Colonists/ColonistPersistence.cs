using System;
using System.Collections.Generic;
using FallowEarth.Colonists.Persistence;
using FallowEarth.Saving;
using UnityEngine;

[RequireComponent(typeof(Colonist))]
public class ColonistPersistence : MonoBehaviour
{
    private Colonist owner;
    private ColonistNeeds needsModule;
    private ColonistSocial socialModule;

    void Awake()
    {
        owner = GetComponent<Colonist>();
        needsModule = GetComponent<ColonistNeeds>();
        socialModule = GetComponent<ColonistSocial>();
    }

    public void PopulateSaveData(SaveData saveData, HashSet<JobType> jobPriorities, ColonistSchedule schedule, ColonistRoleProfile roleProfile)
    {
        var needSnapshots = new List<NeedSnapshot>();
        if (needsModule != null)
        {
            needsModule.InitializeIfNeeded();
            foreach (var kvp in needsModule.CreateSnapshot())
                needSnapshots.Add(new NeedSnapshot { type = kvp.Key, value = kvp.Value });
        }

        var traitNames = needsModule != null ? needsModule.GetTraitNames() : new List<string>();
        var relationshipData = socialModule != null ? socialModule.CreateSnapshot() : new List<RelationshipSnapshot>();

        var state = new ColonistSaveState
        {
            position = owner.transform.position,
            mood = owner.mood,
            health = owner.health,
            hunger = owner.hunger,
            fatigue = owner.fatigue,
            stress = owner.stress,
            social = owner.social,
            activity = owner.activity,
            allowedJobs = new List<JobType>(jobPriorities),
            needs = needSnapshots,
            role = roleProfile != null ? roleProfile.RoleName : null,
            schedule = schedule != null ? schedule.ToArray() : null,
            traits = traitNames,
            relationships = relationshipData
        };

        saveData.Set("colonist", state);
    }

    public bool LoadFromSaveData(SaveData saveData, HashSet<JobType> jobPriorities, ref ColonistSchedule schedule, ref ColonistRoleProfile roleProfile)
    {
        if (!saveData.TryGet("colonist", out ColonistSaveState state))
            return false;

        owner.transform.position = state.position;
        owner.mood = state.mood;
        owner.health = state.health;
        owner.hunger = state.hunger;
        owner.fatigue = state.fatigue;
        owner.stress = state.stress;
        owner.social = state.social;

        jobPriorities.Clear();
        if (state.allowedJobs != null)
        {
            foreach (var job in state.allowedJobs)
                jobPriorities.Add(job);
        }
        else
        {
            foreach (JobType jt in System.Enum.GetValues(typeof(JobType)))
                jobPriorities.Add(jt);
        }

        if (needsModule != null)
        {
            needsModule.InitializeIfNeeded();
            if (state.needs != null)
            {
                var snapshot = new Dictionary<NeedType, float>();
                foreach (var need in state.needs)
                    snapshot[need.type] = need.value;
                needsModule.RestoreSnapshot(snapshot);
            }
            needsModule.SetTraitNames(state.traits);
        }

        if (!string.IsNullOrEmpty(state.role))
        {
            roleProfile = null;
            var defaults = ColonistRoleLibrary.DefaultRoles;
            if (defaults != null)
            {
                foreach (var profile in defaults)
                {
                    if (profile.RoleName == state.role)
                    {
                        roleProfile = profile;
                        break;
                    }
                }
            }
        }

        schedule = roleProfile != null ? roleProfile.CreateSchedule() : schedule ?? new ColonistSchedule();
        if (state.schedule != null)
            schedule.LoadFrom(state.schedule);

        socialModule?.SetPendingRelationships(state.relationships);

        owner.activity = state.activity ?? "Idle";

        return true;
    }
}
