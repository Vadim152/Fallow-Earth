using System.Collections.Generic;
using System.Reflection;
using FallowEarth.MapGeneration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapGeneration.Tests
{
    [TestFixture]
    public class MapGenerationStrategyTests
    {
        [SetUp]
        public void ResetRandom()
        {
            Random.InitState(0);
        }

        [Test]
        public void HeightMapGenerator_ProducesNormalizedValues()
        {
            var generator = ScriptableObject.CreateInstance<PerlinHeightMapGenerator>();
            float[,] heightMap = generator.GenerateHeightMap(8, 6, Vector2.zero);

            Assert.AreEqual(8, heightMap.GetLength(0));
            Assert.AreEqual(6, heightMap.GetLength(1));

            foreach (float value in heightMap)
            {
                Assert.That(value, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void WaterSimulator_FlagsSeaLevelCells()
        {
            var simulator = ScriptableObject.CreateInstance<DefaultWaterSimulator>();
            float[,] heightMap = new float[,]
            {
                { 0.1f, 0.4f },
                { 0.2f, 0.9f }
            };

            HashSet<Vector2Int> water = simulator.SimulateWater(heightMap);

            Assert.IsTrue(water.Contains(new Vector2Int(0, 0)), "Cells below sea level should be water.");
            Assert.IsFalse(water.Contains(new Vector2Int(1, 1)), "High altitude cells should remain dry.");
        }

        [Test]
        public void TemperatureGenerator_DropsWithAltitude()
        {
            var generator = ScriptableObject.CreateInstance<PerlinTemperatureMapGenerator>();
            float[,] heightMap = new float[,]
            {
                { 0f, 1f }
            };

            float[,] temperature = generator.GenerateTemperatureMap(heightMap, Vector2.zero);

            Assert.Greater(temperature[0, 0], temperature[0, 1]);
        }

        [Test]
        public void HumidityGenerator_BenefitsNearWater()
        {
            var generator = ScriptableObject.CreateInstance<PerlinHumidityMapGenerator>();
            float[,] heightMap = new float[,]
            {
                { 0.2f, 0.2f },
                { 0.2f, 0.2f }
            };
            var waterCells = new HashSet<Vector2Int> { new Vector2Int(0, 0) };

            float[,] humidity = generator.GenerateHumidityMap(heightMap, waterCells, Vector2.zero);

            Assert.GreaterOrEqual(humidity[0, 0], humidity[1, 1]);
        }

        [Test]
        public void BiomePainter_SelectsBestMatchingBiome()
        {
            var painter = ScriptableObject.CreateInstance<VoronoiBiomePainter>();

            var coldBiome = ScriptableObject.CreateInstance<BiomeDefinition>();
            coldBiome.heightRange = new Vector2(0f, 1f);
            coldBiome.temperatureRange = new Vector2(-50f, 5f);
            coldBiome.humidityRange = new Vector2(0f, 1f);

            var warmBiome = ScriptableObject.CreateInstance<BiomeDefinition>();
            warmBiome.heightRange = new Vector2(0f, 1f);
            warmBiome.temperatureRange = new Vector2(10f, 50f);
            warmBiome.humidityRange = new Vector2(0f, 1f);

            SetPrivateField(painter, "biomeDefinitions", new List<BiomeDefinition> { coldBiome, warmBiome });
            SetPrivateField(painter, "voronoiSeedCount", 2);
            SetPrivateField(painter, "voronoiJitter", 0f);
            SetPrivateField(painter, "biomeBlendStrength", 0.5f);

            float[,] height = new float[,] { { 0.5f } };
            float[,] temperature = new float[,] { { -10f } };
            float[,] humidity = new float[,] { { 0.5f } };

            BiomeDefinition[,] result = painter.PaintBiomes(height, temperature, humidity);

            Assert.AreSame(coldBiome, result[0, 0]);
        }

        [Test]
        public void TilemapApplier_MarksWaterAsImpassable()
        {
            var applier = ScriptableObject.CreateInstance<DefaultTilemapApplier>();
            SetPrivateField(applier, "treeProbability", 0f);
            SetPrivateField(applier, "berryProbability", 0f);
            SetPrivateField(applier, "mountainThreshold", 0.9f);

            var groundLayer = new InMemoryTileLayer();
            var treeLayer = new InMemoryTileLayer();
            var berryLayer = new InMemoryTileLayer();
            var resourceLayer = new InMemoryTileLayer();

            TileBase groundTile = ScriptableObject.CreateInstance<Tile>();
            TileBase waterTile = ScriptableObject.CreateInstance<Tile>();
            TileBase mountainTile = ScriptableObject.CreateInstance<Tile>();
            TileBase treeTile = ScriptableObject.CreateInstance<Tile>();
            TileBase berryTile = ScriptableObject.CreateInstance<Tile>();
            TileBase resourceTile = ScriptableObject.CreateInstance<Tile>();

            var context = new TilemapApplierContext
            {
                Width = 2,
                Height = 2,
                HeightMap = new float[,]
                {
                    { 0.1f, 0.1f },
                    { 0.1f, 0.1f }
                },
                TemperatureMap = new float[,]
                {
                    { 0f, 0f },
                    { 0f, 0f }
                },
                HumidityMap = new float[,]
                {
                    { 0f, 0f },
                    { 0f, 0f }
                },
                BiomeMap = new BiomeDefinition[2, 2],
                WaterCells = new HashSet<Vector2Int> { new Vector2Int(0, 0) },
                Passable = new bool[2, 2],
                GroundLayer = groundLayer,
                TreeLayer = treeLayer,
                BerryLayer = berryLayer,
                ResourceLayer = resourceLayer,
                GroundTile = groundTile,
                WaterTile = waterTile,
                MountainTile = mountainTile,
                TreeTile = treeTile,
                BerryTile = berryTile,
                ResourceTile = resourceTile,
                GrassColor = Color.white,
                WaterColor = Color.blue,
                MountainColor = Color.gray,
                TreeColor = Color.green,
                BerryColor = Color.red,
                ResourceColor = Color.yellow
            };

            applier.Apply(context);

            Assert.IsFalse(context.Passable[0, 0], "Water cell should be impassable.");
            Assert.IsTrue(context.Passable[1, 1], "Land cell should remain passable.");
            Assert.AreEqual(waterTile, groundLayer.GetTile(new Vector3Int(0, 0, 0)));
            Assert.AreEqual(Color.blue, groundLayer.GetColor(new Vector3Int(0, 0, 0)));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private sealed class InMemoryTileLayer : ITileLayer
        {
            private readonly Dictionary<Vector3Int, TileBase?> tiles = new();
            private readonly Dictionary<Vector3Int, Color> colors = new();

            public void SetTile(Vector3Int position, TileBase tile)
            {
                tiles[position] = tile;
            }

            public void SetTileFlags(Vector3Int position, TileFlags flags)
            {
                // No-op for tests.
            }

            public void SetColor(Vector3Int position, Color color)
            {
                colors[position] = color;
            }

            public TileBase? GetTile(Vector3Int position)
            {
                tiles.TryGetValue(position, out var tile);
                return tile;
            }

            public Color GetColor(Vector3Int position)
            {
                return colors.TryGetValue(position, out var color) ? color : Color.white;
            }
        }
    }
}
