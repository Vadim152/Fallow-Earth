using UnityEngine;

namespace FallowEarth.MapGeneration
{
    [CreateAssetMenu(fileName = "PerlinHeightMap", menuName = "World/Map Generation/Height/Perlin")]
    public class PerlinHeightMapGenerator : HeightMapGeneratorBase
    {
        [SerializeField]
        private float noiseScale = 0.05f;

        [SerializeField]
        private int octaves = 4;

        [SerializeField]
        private float persistence = 0.5f;

        [SerializeField]
        private float lacunarity = 2.0f;

        public override float[,] GenerateHeightMap(int width, int height, Vector2 offset)
        {
            return GenerateFractalNoise(width, height, noiseScale, octaves, persistence, lacunarity, offset);
        }

        protected float[,] GenerateFractalNoise(int width, int height, float scale, int octaveCount, float persistenceValue, float lacunarityValue, Vector2 offset)
        {
            if (scale <= 0f)
            {
                scale = 0.0001f;
            }

            float[,] map = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float amplitude = 1f;
                    float frequency = 1f;
                    float noiseHeight = 0f;

                    for (int octave = 0; octave < octaveCount; octave++)
                    {
                        float sampleX = (x + offset.x) * frequency * scale;
                        float sampleY = (y + offset.y) * frequency * scale;
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistenceValue;
                        frequency *= lacunarityValue;
                    }

                    map[x, y] = Mathf.InverseLerp(-1f, 1f, noiseHeight);
                }
            }

            return map;
        }
    }
}
