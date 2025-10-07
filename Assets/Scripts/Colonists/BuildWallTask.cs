using UnityEngine;

/// <summary>
/// Task that handles collecting wood logs and building a wall frame.
/// </summary>
public class BuildWallTask : Task
{
    public enum Stage { CollectWood, MoveToSite, Build }

    public Vector2Int cell;
    public int woodNeeded;
    public float buildTime;
    public Stage stage = Stage.CollectWood;
    public WoodLog targetLog;

    public BuildWallTask(Vector2Int cell, float buildTime, int woodNeeded,
        System.Action<Colonist> onComplete = null) : base(Vector2.zero, onComplete, JobType.Build)
    {
        this.cell = cell;
        this.buildTime = buildTime;
        this.woodNeeded = woodNeeded;
    }

    public void ReleaseReservation()
    {
        if (targetLog != null)
        {
            targetLog.Reserved = false;
        }
        targetLog = null;
    }
}
