using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.MapGeneration
{
    [CreateAssetMenu(fileName = "PerlinHumidity", menuName = "World/Map Generation/Humidity/Perlin")]
    public class PerlinHumidityMapGenerator : HumidityMapGeneratorBase
    {
        [SerializeField]
        private float noiseScale = 0.08f;

        [SerializeField]
        private int octaves = 3;

        [SerializeField]
        private float persistence = 0.5f;

        [SerializeField]
        private float lacunarity = 2.1f;

        [SerializeField]
        private float altitudePenalty = 0.25f;

        [SerializeField]
        private float waterBonus = 0.35f;

        [SerializeField]
        private int waterRadius = 6;

        public override float[,] GenerateHumidityMap(float[,] heightMap, HashSet<Vector2Int> waterCells, Vector2 offset)
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
                    float humidity = normalized;
                    humidity -= heightMap[x, y] * altitudePenalty;

                    float distance = DistanceToNearestWater(waterCells, x, y, waterRadius, width, height);
                    if (distance >= 0f)
                    {
                        float bonus = Mathf.Clamp01(1f - distance / Mathf.Max(1f, waterRadius));
                        humidity += bonus * waterBonus;
                    }

                    result[x, y] = Mathf.Clamp01(humidity);
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

        private float DistanceToNearestWater(HashSet<Vector2Int> waterCells, int x, int y, int radius, int width, int height)
        {
            float best = float.MaxValue;
            bool found = false;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (!InBounds(nx, ny, width, height))
                        continue;
                    Vector2Int cell = new Vector2Int(nx, ny);
                    if (!waterCells.Contains(cell))
                        continue;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance < best)
                    {
                        best = distance;
                        found = true;
                    }
                }
            }

            return found ? best : -1f;
        }

        private bool InBounds(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}
