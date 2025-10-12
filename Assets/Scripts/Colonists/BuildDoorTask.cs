using FallowEarth.Construction;
using UnityEngine;

/// <summary>
/// Task identical to BuildWallTask but results in a door instead of a wall.
/// </summary>
public class BuildDoorTask : BuildWallTask
{
    public BuildDoorTask(Vector2Int cell, float buildTime, ConstructionProject project,
        System.Action<Colonist> onComplete = null) : base(cell, buildTime, project, onComplete)
    {
    }
}
