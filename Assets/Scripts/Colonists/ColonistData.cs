using System;
using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.Colonists.Persistence
{
    [Serializable]
    public struct ColonistSaveState
    {
        public Vector3 position;
        public float mood;
        public float health;
        public float hunger;
        public float fatigue;
        public float stress;
        public float social;
        public string activity;
        public List<global::JobType> allowedJobs;
        public List<NeedSnapshot> needs;
        public string role;
        public global::ColonistScheduleActivity[] schedule;
        public List<string> traits;
        public List<RelationshipSnapshot> relationships;
    }

    [Serializable]
    public struct NeedSnapshot
    {
        public global::NeedType type;
        public float value;
    }

    [Serializable]
    public struct RelationshipSnapshot
    {
        public string colonistName;
        public float affinity;
    }
}
