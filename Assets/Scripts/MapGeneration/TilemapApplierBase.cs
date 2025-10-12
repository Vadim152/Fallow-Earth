using UnityEngine;

namespace FallowEarth.MapGeneration
{
    /// <summary>
    /// Applies generated map data to the visual tilemaps and returns walkability information.
    /// </summary>
    public interface ITilemapApplier
    {
        void Apply(TilemapApplierContext context);
    }

    public abstract class TilemapApplierBase : ScriptableObject, ITilemapApplier
    {
        public abstract void Apply(TilemapApplierContext context);
    }
}
