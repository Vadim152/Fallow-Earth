using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.MapGeneration
{
    /// <summary>
    /// Produces a humidity map taking into account nearby bodies of water.
    /// </summary>
    public interface IHumidityMapGenerator
    {
        float[,] GenerateHumidityMap(float[,] heightMap, HashSet<Vector2Int> waterCells, Vector2 offset);
    }

    public abstract class HumidityMapGeneratorBase : ScriptableObject, IHumidityMapGenerator
    {
        public abstract float[,] GenerateHumidityMap(float[,] heightMap, HashSet<Vector2Int> waterCells, Vector2 offset);
    }
}
