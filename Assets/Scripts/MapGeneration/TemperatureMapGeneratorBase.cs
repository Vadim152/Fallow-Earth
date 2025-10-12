using UnityEngine;

namespace FallowEarth.MapGeneration
{
    /// <summary>
    /// Produces a temperature field that can be fed to biome selection systems.
    /// </summary>
    public interface ITemperatureMapGenerator
    {
        float[,] GenerateTemperatureMap(float[,] heightMap, Vector2 offset);
    }

    public abstract class TemperatureMapGeneratorBase : ScriptableObject, ITemperatureMapGenerator
    {
        public abstract float[,] GenerateTemperatureMap(float[,] heightMap, Vector2 offset);
    }
}
