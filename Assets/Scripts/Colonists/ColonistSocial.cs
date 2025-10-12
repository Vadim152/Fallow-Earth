using System.Collections.Generic;
using FallowEarth.Colonists.Persistence;
using UnityEngine;

[RequireComponent(typeof(Colonist))]
public class ColonistSocial : MonoBehaviour
{
    private readonly Dictionary<Colonist, SocialRelationship> relationships = new Dictionary<Colonist, SocialRelationship>();
    private readonly List<RelationshipSnapshot> pendingRelationshipData = new List<RelationshipSnapshot>();

    private Colonist owner;
    private ColonistNeeds needsModule;

    void Awake()
    {
        owner = GetComponent<Colonist>();
        needsModule = GetComponent<ColonistNeeds>();
    }

    void Start()
    {
        RestorePendingRelationships();
    }

    public void RestorePendingRelationships()
    {
        if (pendingRelationshipData.Count == 0)
            return;

        var colonists = FindObjectsOfType<Colonist>();
        foreach (var snapshot in pendingRelationshipData)
        {
            if (string.IsNullOrEmpty(snapshot.colonistName))
                continue;

            foreach (var other in colonists)
            {
                if (other == owner)
                    continue;
                if (other.name == snapshot.colonistName)
                {
                    var relationship = EnsureRelationship(other);
                    if (relationship != null)
                    {
                        float impact = snapshot.affinity - relationship.Affinity;
                        if (Mathf.Abs(impact) > 0.001f)
                            relationship.AddEvent("Rekindled bond", impact);
                    }
                    break;
                }
            }
        }

        pendingRelationshipData.Clear();
    }

    public SocialRelationship EnsureRelationship(Colonist other)
    {
        if (other == null || other == owner)
            return null;

        if (!relationships.TryGetValue(other, out var relationship))
        {
            relationship = new SocialRelationship();
            relationships[other] = relationship;
        }

        return relationship;
    }

    public void SetPendingRelationships(IEnumerable<RelationshipSnapshot> data)
    {
        pendingRelationshipData.Clear();
        if (data != null)
            pendingRelationshipData.AddRange(data);
    }

    public List<RelationshipSnapshot> CreateSnapshot()
    {
        var snapshot = new List<RelationshipSnapshot>();
        foreach (var kvp in relationships)
        {
            if (kvp.Key == null || kvp.Value == null)
                continue;
            snapshot.Add(new RelationshipSnapshot
            {
                colonistName = kvp.Key.name,
                affinity = kvp.Value.Affinity
            });
        }
        return snapshot;
    }

    public float GetAverageMoodModifier()
    {
        if (relationships.Count == 0)
            return 0f;

        float total = 0f;
        foreach (var rel in relationships.Values)
            total += rel?.GetMoodModifier() ?? 0f;
        return total / relationships.Count;
    }

    public bool TryStartSocialize()
    {
        Colonist[] all = FindObjectsOfType<Colonist>();
        Colonist best = null;
        float bestScore = float.MinValue;
        float ownSocialNeed = needsModule != null ? needsModule.GetValue(NeedType.Social) : 0f;

        foreach (var colonist in all)
        {
            if (colonist == owner || colonist.IsBusy)
                continue;

            float dist = Vector2.Distance(owner.transform.position, colonist.transform.position);
            if (dist > 4f)
                continue;

            float partnerNeed = colonist.Needs != null ? colonist.Needs.GetValue(NeedType.Social) : (1f - colonist.social);
            float affinity = 0f;
            var relationship = EnsureRelationship(colonist);
            if (relationship != null)
                affinity = relationship.Affinity;
            float score = partnerNeed - dist * 0.25f + affinity * 0.3f;
            if (score > bestScore && partnerNeed > 0.35f)
            {
                bestScore = score;
                best = colonist;
            }
        }

        if (best != null && (ownSocialNeed > 0.35f || (best.Needs != null && best.Needs.GetValue(NeedType.Social) > 0.35f)))
        {
            float duration = Random.Range(1.5f, 3.5f);
            Vector2 meetPoint = (owner.transform.position + best.transform.position) * 0.5f;
            var relationA = EnsureRelationship(best);
            var relationB = best.GetComponent<ColonistSocial>()?.EnsureRelationship(owner);

            var taskA = new SocializeTask(best, meetPoint, duration, col =>
            {
                col.SatisfyNeed(NeedType.Social, 0.5f);
                col.SatisfyNeed(NeedType.Stress, 0.3f);
                col.SatisfyNeed(NeedType.Recreation, 0.3f);
                relationA?.AddEvent("Pleasant chat", 0.1f);
            });

            var taskB = new SocializeTask(owner, meetPoint, duration, col =>
            {
                col.SatisfyNeed(NeedType.Social, 0.5f);
                col.SatisfyNeed(NeedType.Stress, 0.3f);
                col.SatisfyNeed(NeedType.Recreation, 0.3f);
                relationB?.AddEvent("Pleasant chat", 0.1f);
            });

            if (owner.TryAssignTask(taskA))
            {
                if (best.TryAssignTask(taskB))
                    return true;

                owner.CancelTasks();
            }
        }

        return false;
    }
}
