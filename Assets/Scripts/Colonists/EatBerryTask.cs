using UnityEngine;

/// <summary>
/// Task for moving to a berry bush and eating to reduce hunger.
/// </summary>
public class EatBerryTask : TimedTask
{
    public Vector2Int cell;

    public EatBerryTask(Vector2Int cell, float duration, System.Action<Colonist> onComplete = null)
        : base(new Vector2(cell.x + 0.5f, cell.y + 0.5f), duration, onComplete)
    {
        this.cell = cell;
    }
}
