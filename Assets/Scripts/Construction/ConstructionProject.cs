using System;
using System.Collections.Generic;
using FallowEarth.Infrastructure;
using FallowEarth.ResourcesSystem;
using UnityEngine;

namespace FallowEarth.Construction
{
    /// <summary>
    /// Represents an individual construction job on the map.
    /// </summary>
    [Serializable]
    public class ConstructionProject
    {
        public Vector2Int Cell { get; }
        public int Level { get; }
        public ConstructionMaterialDefinition Material { get; }
        public ConstructionType Type { get; }
        public float RequiredWork { get; }
        public float Progress { get; private set; }
        public bool IsCompleted => Progress >= RequiredWork;

        private readonly Queue<ResourceRequest> pendingResources = new Queue<ResourceRequest>();

        public ConstructionProject(Vector2Int cell, int level, ConstructionType type, ConstructionMaterialDefinition material, float requiredWork)
        {
            Cell = cell;
            Level = level;
            Type = type;
            Material = material;
            RequiredWork = requiredWork;

            if (material != null)
            {
                foreach (var request in material.Cost)
                    pendingResources.Enqueue(request);
            }
        }

        public bool HasPendingResources => pendingResources.Count > 0;

        public ResourceRequest? PeekNextRequirement()
        {
            if (pendingResources.Count == 0)
                return null;
            return pendingResources.Peek();
        }

        public bool TryConsumeNextRequirement()
        {
            if (pendingResources.Count == 0)
                return true;
            var request = pendingResources.Peek();
            if (GameServices.TryResolve(out IResourceManager resourceManager) &&
                resourceManager.TryConsume(new[] { request }))
            {
                pendingResources.Dequeue();
                return true;
            }
            return false;
        }

        public bool TryConsumeResources()
        {
            while (pendingResources.Count > 0)
            {
                if (!TryConsumeNextRequirement())
                    break;
            }
            return pendingResources.Count == 0;
        }

        public void AddWork(float amount)
        {
            Progress = Mathf.Clamp(Progress + amount, 0f, RequiredWork);
        }
    }

    public enum ConstructionType
    {
        Wall,
        Door,
        Floor,
        RoomPlan,
        Bed
    }
}
