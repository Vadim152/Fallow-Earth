using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.MapGeneration
{
    /// <summary>
    /// Simulates water flow, carving rivers and lakes on an existing height map.
    /// </summary>
    public interface IWaterSimulator
    {
        HashSet<Vector2Int> SimulateWater(float[,] heightMap);
    }

    public abstract class WaterSimulatorBase : ScriptableObject, IWaterSimulator
    {
        public abstract HashSet<Vector2Int> SimulateWater(float[,] heightMap);
    }
}
