using UnityEngine;

namespace FallowEarth.MapGeneration
{
    [CreateAssetMenu(fileName = "PerlinTemperature", menuName = "World/Map Generation/Temperature/Perlin")]
    public class PerlinTemperatureMapGenerator : TemperatureMapGeneratorBase
    {
        [SerializeField]
        private float noiseScale = 0.03f;

        [SerializeField]
        private int octaves = 3;

        [SerializeField]
        private float persistence = 0.55f;

        [SerializeField]
        private float lacunarity = 2.2f;

        [SerializeField]
        private float minTemperature = -15f;

        [SerializeField]
        private float maxTemperature = 40f;

        [SerializeField]
        private float altitudeFactor = 15f;

        public override float[,] GenerateTemperatureMap(float[,] heightMap, Vector2 offset)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            float[,] noise = GenerateFractalNoise(width, height, noiseScale, octaves, persistence, lacunarity, offset);
            float[,] result = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float normalized = Mathf.Clamp01(noise[x, y]);
                    float temperature = Mathf.Lerp(minTemperature, maxTemperature, normalized);
                    temperature -= heightMap[x, y] * altitudeFactor;
                    result[x, y] = temperature;
                }
            }

            return result;
        }

        private float[,] GenerateFractalNoise(int width, int height, float scale, int octaveCount, float persistenceValue, float lacunarityValue, Vector2 offset)
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
