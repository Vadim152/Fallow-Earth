using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FallowEarth.MapGeneration
{
    /// <summary>
    /// Describes operations that a tilemap applier can perform on a tile layer.
    /// </summary>
    public interface ITileLayer
    {
        void SetTile(Vector3Int position, TileBase tile);
        void SetTileFlags(Vector3Int position, TileFlags flags);
        void SetColor(Vector3Int position, Color color);
    }

    /// <summary>
    /// Provides all the data required by an <see cref="ITilemapApplier"/>.
    /// </summary>
    public class TilemapApplierContext
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public float[,] HeightMap { get; set; }
        public float[,] TemperatureMap { get; set; }
        public float[,] HumidityMap { get; set; }
        public BiomeDefinition[,] BiomeMap { get; set; }
        public HashSet<Vector2Int> WaterCells { get; set; } = new HashSet<Vector2Int>();
        public bool[,] Passable { get; set; }

        public ITileLayer GroundLayer { get; set; }
        public ITileLayer TreeLayer { get; set; }
        public ITileLayer BerryLayer { get; set; }
        public ITileLayer ResourceLayer { get; set; }

        public TileBase GroundTile { get; set; }
        public TileBase WaterTile { get; set; }
        public TileBase MountainTile { get; set; }
        public TileBase TreeTile { get; set; }
        public TileBase BerryTile { get; set; }
        public TileBase ResourceTile { get; set; }

        public Color GrassColor { get; set; }
        public Color WaterColor { get; set; }
        public Color MountainColor { get; set; }
        public Color TreeColor { get; set; }
        public Color BerryColor { get; set; }
        public Color ResourceColor { get; set; }
    }
}
