using UnityEngine;

/// <summary>
/// Task identical to BuildWallTask but results in a door instead of a wall.
/// </summary>
public class BuildDoorTask : BuildWallTask
{
    public BuildDoorTask(Vector2Int cell, float buildTime, int woodNeeded,
        System.Action<Colonist> onComplete = null) : base(cell, buildTime, woodNeeded, onComplete)
    {
    }
}
