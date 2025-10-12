using System;
using System.Collections.Generic;
using System.Linq;
using FallowEarth.ResourcesSystem;
using UnityEngine;

/// <summary>
/// Global resource tracker capable of handling multiple resource types, qualities and mass.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private readonly Dictionary<ResourceKey, ResourceLedgerEntry> ledger = new Dictionary<ResourceKey, ResourceLedgerEntry>();

    public event Action<ResourceLedgerSnapshot> LedgerChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResourceRegistry.EnsureInitialized();
        BroadcastLedger();
    }

    /// <summary>
    /// Adds resources to the global stockpile.
    /// </summary>
    public static void Add(ResourceStack stack)
    {
        if (Instance == null || stack.IsEmpty)
            return;

        var key = new ResourceKey(stack.Definition.Id, stack.Quality);
        if (!Instance.ledger.TryGetValue(key, out var entry))
        {
            entry = new ResourceLedgerEntry(stack.Definition, stack.Quality, 0);
        }
        entry = entry.WithAmount(entry.Amount + stack.Amount);
        Instance.ledger[key] = entry;
        Instance.BroadcastLedger();
    }

    /// <summary>
    /// Attempts to consume the requested resources, prioritising higher qualities.
    /// </summary>
    public static bool TryConsume(IEnumerable<ResourceRequest> requests)
    {
        if (Instance == null)
            return false;
        if (requests == null)
            return true;

        var tempLedger = Instance.ledger.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        foreach (var request in requests)
        {
            if (!Instance.TryConsumeInternal(tempLedger, request))
                return false;
        }

        Instance.ledger.Clear();
        foreach (var kvp in tempLedger)
            Instance.ledger[kvp.Key] = kvp.Value;

        Instance.BroadcastLedger();
        return true;
    }

    bool TryConsumeInternal(Dictionary<ResourceKey, ResourceLedgerEntry> tempLedger, ResourceRequest request)
    {
        int remaining = request.Amount;
        var matching = tempLedger
            .Where(kvp => kvp.Key.ResourceId == request.Definition.Id && kvp.Value.Quality >= request.MinimumQuality)
            .OrderByDescending(kvp => kvp.Value.Quality)
            .ToList();

        foreach (var kvp in matching)
        {
            if (remaining <= 0)
                break;

            var entry = kvp.Value;
            int take = Mathf.Min(entry.Amount, remaining);
            remaining -= take;
            var newEntry = entry.WithAmount(entry.Amount - take);
            if (newEntry.Amount > 0)
                tempLedger[kvp.Key] = newEntry;
            else
                tempLedger.Remove(kvp.Key);
        }

        return remaining <= 0;
    }

    /// <summary>
    /// Convenience helper for legacy systems that only track total wood regardless of quality.
    /// </summary>
    public int Wood
    {
        get
        {
            if (!ResourceRegistry.TryGet(DefaultResourceIds.Wood, out var def))
                return 0;
            return GetTotalAmount(def.Id);
        }
    }

    public ResourceLedgerSnapshot GetSnapshot()
    {
        return new ResourceLedgerSnapshot(ledger.Values.ToList());
    }

    public float GetTotalMass()
    {
        float total = 0f;
        foreach (var entry in ledger.Values)
            total += entry.TotalMass;
        return total;
    }

    public int GetTotalAmount(string resourceId)
    {
        int total = 0;
        foreach (var entry in ledger.Values)
        {
            if (entry.Definition.Id == resourceId)
                total += entry.Amount;
        }
        return total;
    }

    public void BroadcastLedger()
    {
        LedgerChanged?.Invoke(GetSnapshot());
    }

    public static void AddWood(int amount)
    {
        if (amount <= 0)
            return;
        ResourceRegistry.EnsureInitialized();
        if (ResourceRegistry.TryGet(DefaultResourceIds.Wood, out var def))
        {
            Add(new ResourceStack(def, ResourceQuality.Common, amount));
        }
    }

    public static bool UseWood(int amount)
    {
        if (amount <= 0)
            return true;
        ResourceRegistry.EnsureInitialized();
        if (!ResourceRegistry.TryGet(DefaultResourceIds.Wood, out var def))
            return false;
        return TryConsume(new[] { new ResourceRequest(def, amount) });
    }

    struct ResourceKey : IEquatable<ResourceKey>
    {
        public readonly string ResourceId;
        public readonly ResourceQuality Quality;

        public ResourceKey(string resourceId, ResourceQuality quality)
        {
            ResourceId = resourceId;
            Quality = quality;
        }

        public bool Equals(ResourceKey other)
        {
            return ResourceId == other.ResourceId && Quality == other.Quality;
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ResourceId != null ? ResourceId.GetHashCode() : 0) * 397) ^ (int)Quality;
            }
        }
    }

    public readonly struct ResourceLedgerEntry
    {
        public readonly ResourceDefinition Definition;
        public readonly ResourceQuality Quality;
        public readonly int Amount;

        public ResourceLedgerEntry(ResourceDefinition definition, ResourceQuality quality, int amount)
        {
            Definition = definition;
            Quality = quality;
            Amount = amount;
        }

        public float TotalMass => Amount * Definition.MassPerUnit * Quality.GetMassMultiplier();

        public ResourceLedgerEntry WithAmount(int newAmount)
        {
            return new ResourceLedgerEntry(Definition, Quality, Mathf.Max(0, newAmount));
        }
    }

    public readonly struct ResourceLedgerSnapshot
    {
        public readonly IReadOnlyList<ResourceLedgerEntry> Entries;

        public ResourceLedgerSnapshot(IReadOnlyList<ResourceLedgerEntry> entries)
        {
            Entries = entries ?? Array.Empty<ResourceLedgerEntry>();
        }
    }
}
