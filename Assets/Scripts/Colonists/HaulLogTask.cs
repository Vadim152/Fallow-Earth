using FallowEarth.ResourcesSystem;
using UnityEngine;

/// <summary>
/// Generic hauling task that moves a resource item into a storage cell.
/// </summary>
public class HaulLogTask : Task
{
    public enum Stage { MoveToItem, MoveToZone }

    public ResourceItem item;
    public Vector2Int targetCell;
    public Stage stage = Stage.MoveToItem;

    public HaulLogTask(ResourceItem item, Vector2Int targetCell)
        : base(Vector2.zero, null, JobType.Haul, TaskPriority.High, ColonistScheduleActivityMask.Work)
    {
        this.item = item;
        this.targetCell = targetCell;
        if (item != null)
            item.Reserved = true;
    }

    public void ReleaseReservation()
    {
        if (item != null)
        {
            item.Reserved = false;
        }
        item = null;
    }
}
