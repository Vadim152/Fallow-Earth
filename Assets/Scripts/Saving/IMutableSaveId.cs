namespace FallowEarth.Saving
{
    /// <summary>
    /// Optional helper interface for saveable objects that allow their identifier to be reassigned by the manager.
    /// </summary>
    public interface IMutableSaveId
    {
        void SetSaveId(string newId);
    }
}
