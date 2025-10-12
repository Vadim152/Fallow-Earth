using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.MapGeneration
{
    [CreateAssetMenu(fileName = "VoronoiBiomePainter", menuName = "World/Map Generation/Biomes/Voronoi")]
    public class VoronoiBiomePainter : BiomePainterBase
    {
        [SerializeField]
        private List<BiomeDefinition> biomeDefinitions = new List<BiomeDefinition>();

        [SerializeField]
        private int voronoiSeedCount = 16;

        [SerializeField]
        private float voronoiJitter = 0.3f;

        [SerializeField, Range(0f, 2f)]
        private float biomeBlendStrength = 0.6f;

        public override BiomeDefinition[,] PaintBiomes(float[,] heightMap, float[,] temperatureMap, float[,] humidityMap)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            var result = new BiomeDefinition[width, height];
            if (biomeDefinitions == null || biomeDefinitions.Count == 0)
            {
                return result;
            }

            List<BiomeSeed> seeds = CreateBiomeSeeds();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float heightValue = heightMap[x, y];
                    float temperature = temperatureMap[x, y];
                    float humidity = humidityMap[x, y];

                    BiomeDefinition biome = SelectBiome(seeds, x, y, width, height, heightValue, temperature, humidity);
                    result[x, y] = biome;
                }
            }

            return result;
        }

        private BiomeDefinition SelectBiome(List<BiomeSeed> seeds, int x, int y, int width, int height, float heightValue, float temperature, float humidity)
        {
            Vector2 pos = new Vector2((float)x / Mathf.Max(1, width - 1), (float)y / Mathf.Max(1, height - 1));
            BiomeDefinition chosen = null;
            float bestScore = float.MinValue;

            foreach (var seed in seeds)
            {
                float distance = Vector2.Distance(pos, seed.position);
                float distanceWeight = Mathf.Exp(-distance * biomeBlendStrength);
                float fitness = seed.biome.EvaluateFitness(heightValue, temperature, humidity);
                if (seed.biome.Matches(heightValue, temperature, humidity))
                {
                    fitness += 0.5f;
                }

                float score = fitness * distanceWeight;
                if (score > bestScore)
                {
                    bestScore = score;
                    chosen = seed.biome;
                }
            }

            if (chosen == null)
            {
                float fallbackScore = float.MinValue;
                foreach (var biome in biomeDefinitions)
                {
                    float score = biome.EvaluateFitness(heightValue, temperature, humidity);
                    if (score > fallbackScore)
                    {
                        fallbackScore = score;
                        chosen = biome;
                    }
                }
            }

            return chosen;
        }

        private List<BiomeSeed> CreateBiomeSeeds()
        {
            var seeds = new List<BiomeSeed>();
            if (biomeDefinitions == null || biomeDefinitions.Count == 0)
                return seeds;

            for (int i = 0; i < biomeDefinitions.Count; i++)
            {
                seeds.Add(new BiomeSeed
                {
                    biome = biomeDefinitions[i],
                    position = new Vector2(Random.value, Random.value)
                });
            }

            int additionalSeeds = Mathf.Max(0, voronoiSeedCount - biomeDefinitions.Count);
            for (int i = 0; i < additionalSeeds; i++)
            {
                var biome = biomeDefinitions[Random.Range(0, biomeDefinitions.Count)];
                Vector2 randomPos = new Vector2(Random.value, Random.value);
                randomPos += Random.insideUnitCircle * voronoiJitter * 0.5f;
                randomPos.x = Mathf.Clamp01(randomPos.x);
                randomPos.y = Mathf.Clamp01(randomPos.y);
                seeds.Add(new BiomeSeed { biome = biome, position = randomPos });
            }

            return seeds;
        }

        private struct BiomeSeed
        {
            public BiomeDefinition biome;
            public Vector2 position;
        }
    }
}
