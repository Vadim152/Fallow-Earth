using System;
using System.Collections.Generic;

namespace FallowEarth.ResourcesSystem
{
    public interface IResourceManager
    {
        event Action<ResourceManager.ResourceLedgerSnapshot> LedgerChanged;

        int Wood { get; }

        void Add(ResourceStack stack);

        bool TryConsume(IEnumerable<ResourceRequest> requests);

        ResourceManager.ResourceLedgerSnapshot GetSnapshot();

        float GetTotalMass();

        int GetTotalAmount(string resourceId);

        void BroadcastLedger();

        void AddWood(int amount);

        bool UseWood(int amount);
    }
}
