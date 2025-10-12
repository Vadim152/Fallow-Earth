using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SocialRelationship
{
    [Serializable]
    public struct SocialEvent
    {
        public string description;
        public float impact;
        public float timestamp;
    }

    [SerializeField] private float affinity;
    [SerializeField] private List<SocialEvent> history = new List<SocialEvent>();

    public float Affinity => Mathf.Clamp(affinity, -1f, 1f);
    public IReadOnlyList<SocialEvent> History => history;

    public void AddEvent(string description, float impact)
    {
        affinity = Mathf.Clamp(affinity + impact, -1f, 1f);
        history.Add(new SocialEvent { description = description, impact = impact, timestamp = Time.time });
        if (history.Count > 20)
            history.RemoveAt(0);
    }

    public float GetMoodModifier()
    {
        return affinity * 0.1f;
    }
}
