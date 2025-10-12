using FallowEarth.Construction;
using FallowEarth.ResourcesSystem;
using UnityEngine;

/// <summary>
/// Task that handles collecting building materials and completing a wall project.
/// </summary>
public class BuildWallTask : Task
{
    public enum Stage { AcquireMaterials, MoveToSite, Build }

    public Vector2Int cell;
    public float buildTime;
    public Stage stage = Stage.AcquireMaterials;
    public ResourceItem targetItem;
    public ConstructionProject project;

    public BuildWallTask(Vector2Int cell, float buildTime, ConstructionProject project,
        System.Action<Colonist> onComplete = null)
        : base(Vector2.zero, onComplete, JobType.Build,
            TaskPriority.High, ColonistScheduleActivityMask.Work)
    {
        this.cell = cell;
        this.buildTime = buildTime;
        this.project = project;
    }

    public void ReleaseReservation()
    {
        if (targetItem != null)
        {
            targetItem.Reserved = false;
        }
        targetItem = null;
    }
}
