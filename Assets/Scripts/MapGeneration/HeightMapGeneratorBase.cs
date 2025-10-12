using UnityEngine;

namespace FallowEarth.MapGeneration
{
    /// <summary>
    /// Generates a normalized height map used as the starting point for terrain creation.
    /// </summary>
    public interface IHeightMapGenerator
    {
        float[,] GenerateHeightMap(int width, int height, Vector2 offset);
    }

    /// <summary>
    /// Base class for height map generators that can be swapped at design time.
    /// </summary>
    public abstract class HeightMapGeneratorBase : ScriptableObject, IHeightMapGenerator
    {
        public abstract float[,] GenerateHeightMap(int width, int height, Vector2 offset);
    }
}
