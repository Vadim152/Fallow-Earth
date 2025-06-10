using UnityEngine;

public class BuildWallTask : TimedTask
{
    public Vector2Int cell;

    public BuildWallTask(Vector2Int cell, float duration, System.Action<Colonist> onComplete = null)
        : base(new Vector2(cell.x + 0.5f, cell.y + 0.5f), duration, onComplete)
    {
        this.cell = cell;
    }
}
