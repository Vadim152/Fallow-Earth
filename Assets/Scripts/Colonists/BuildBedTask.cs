using UnityEngine;

/// <summary>
/// Task identical to BuildWallTask but results in a bed instead of a wall.
/// </summary>
public class BuildBedTask : BuildWallTask
{
    public BuildBedTask(Vector2Int cell, float buildTime, int woodNeeded,
        System.Action<Colonist> onComplete = null) : base(cell, buildTime, woodNeeded, onComplete)
    {
    }
}
