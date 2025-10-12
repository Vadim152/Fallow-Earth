using UnityEngine;

namespace FallowEarth.MapGeneration
{
    /// <summary>
    /// Selects the biome to apply for each cell of the generated world.
    /// </summary>
    public interface IBiomePainter
    {
        BiomeDefinition[,] PaintBiomes(float[,] heightMap, float[,] temperatureMap, float[,] humidityMap);
    }

    public abstract class BiomePainterBase : ScriptableObject, IBiomePainter
    {
        public abstract BiomeDefinition[,] PaintBiomes(float[,] heightMap, float[,] temperatureMap, float[,] humidityMap);
    }
}
