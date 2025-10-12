using System.Collections.Generic;
using FallowEarth.Infrastructure;
using FallowEarth.ResourcesSystem;
using UnityEngine;

namespace FallowEarth.Research
{
    /// <summary>
    /// Tracks colony research progress and handles unlocking technologies.
    /// </summary>
    public class ResearchManager : MonoBehaviour
    {
        public static ResearchManager Instance { get; private set; }

        private readonly Dictionary<string, int> progress = new Dictionary<string, int>();
        private readonly HashSet<string> unlocked = new HashSet<string>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            TechnologyTree.EnsureInitialized();
        }

        public static void EnsureInstance()
        {
            if (Instance == null)
                new GameObject("ResearchManager").AddComponent<ResearchManager>();
        }

        public bool IsUnlocked(string techId)
        {
            return unlocked.Contains(techId);
        }

        public void AddResearchPoints(string techId, int amount)
        {
            if (string.IsNullOrEmpty(techId) || amount <= 0)
                return;

            var tech = TechnologyTree.Get(techId);
            if (tech == null)
                return;

            if (!ArePrerequisitesMet(tech))
                return;

            if (!progress.TryGetValue(techId, out int current))
                current = 0;
            current += amount;
            progress[techId] = current;

            if (current >= tech.ResearchCost)
            {
                TryUnlock(tech);
            }
        }

        bool ArePrerequisitesMet(TechnologyDefinition tech)
        {
            if (tech.Prerequisites == null)
                return true;
            foreach (var pre in tech.Prerequisites)
            {
                if (!unlocked.Contains(pre))
                    return false;
            }
            return true;
        }

        void TryUnlock(TechnologyDefinition tech)
        {
            if (unlocked.Contains(tech.Id))
                return;

            if (!GameServices.TryResolve(out IResourceManager resourceManager) ||
                !resourceManager.TryConsume(tech.UnlockCosts))
                return;

            unlocked.Add(tech.Id);
        }
    }
}
