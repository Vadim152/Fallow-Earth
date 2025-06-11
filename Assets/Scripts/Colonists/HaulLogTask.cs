using UnityEngine;

/// <summary>
/// Task for hauling a wood log to a stockpile zone.
/// </summary>
public class HaulLogTask : Task
{
    public enum Stage { MoveToLog, MoveToZone }

    public WoodLog log;
    public Vector2Int targetCell;
    public Stage stage = Stage.MoveToLog;

    public HaulLogTask(WoodLog log, Vector2Int targetCell)
        : base(Vector2.zero)
    {
        this.log = log;
        this.targetCell = targetCell;
        if (log != null)
            log.Reserved = true;
    }
}
