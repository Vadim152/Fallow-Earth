using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates a procedural world composed of several climate driven biomes. The
/// generator produces height, temperature and humidity maps, runs a simplified
/// water flow simulation to carve rivers and lakes and finally post-processes
/// the tiles with flora/resources before sending everything to the different
/// tilemap layers.
/// </summary>
public class MapGenerator : MonoBehaviour
{

    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap treeTilemap;
    public Tilemap zoneTilemap;
    public Tilemap wallTilemap;
    public Tilemap frameTilemap;
    public Tilemap berryTilemap;
    public Tilemap resourceTilemap;

    [Header("Base Colors")]
    public Color grassColor = Color.white;
    public Color waterColor = new Color(0.2f, 0.2f, 0.7f);
    public Color mountainColor = Color.gray;
    public Color treeColor = new Color(0.25f, 0.2f, 0.1f);
    public Color wallColor = new Color(0.5f, 0.3f, 0.2f);
    public Color zoneColor = new Color(1f, 1f, 0f, 0.4f);
    public Color frameColor = new Color(0.7f, 0.7f, 0.7f, 0.4f);
    public Color doorColor = new Color(0.6f, 0.4f, 0.2f);
    public Color berryColor = new Color(0.8f, 0.1f, 0.2f);
    public Color resourceColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);

    [Header("World Size")]
    public int width = 200;
    public int height = 200;

    [Header("Randomness")]
    [Tooltip("When set to -1 a random seed is used each time.")]
    public int randomSeed = -1;

    [Header("Height Noise")]
    public float heightNoiseScale = 0.05f;
    public int heightOctaves = 4;
    public float heightPersistence = 0.5f;
    public float heightLacunarity = 2.0f;
    [Range(0f, 1f)]
    public float seaLevel = 0.32f;
    [Range(0f, 1f)]
    public float mountainThreshold = 0.78f;

    [Header("Temperature Noise")]
    public float temperatureNoiseScale = 0.03f;
    public int temperatureOctaves = 3;
    public float temperaturePersistence = 0.55f;
    public float temperatureLacunarity = 2.2f;
    public float minTemperature = -15f;
    public float maxTemperature = 40f;
    public float temperatureAltitudeFactor = 15f;

    [Header("Humidity Noise")]
    public float humidityNoiseScale = 0.08f;
    public int humidityOctaves = 3;
    public float humidityPersistence = 0.5f;
    public float humidityLacunarity = 2.1f;
    public float humidityAltitudePenalty = 0.25f;
    public float humidityWaterBonus = 0.35f;
    public int humidityWaterRadius = 6;

    [Header("Biome Layout")]
    public List<BiomeDefinition> biomeDefinitions = new List<BiomeDefinition>();
    public int voronoiSeedCount = 16;
    public float voronoiJitter = 0.3f;
    [Range(0f, 2f)]
    public float biomeBlendStrength = 0.6f;

    [Header("Rivers & Lakes")]
    public int riverCount = 4;
    public int maxRiverLength = 300;
    [Range(0f, 1f)]
    public float riverSourceHeight = 0.65f;
    public float lakeHeightTolerance = 0.02f;

    [Header("Legacy Vegetation Settings")]
    [Range(0f, 1f)]
    public float treeProbability = 0.1f;
    [Range(0f, 1f)]
    public float berryProbability = 0.05f;
    [Tooltip("Seconds for a berry bush to regrow after being harvested")]
    public float berryGrowTime = 60f;

    private TileBase groundTile;
    private TileBase waterTile;
    private TileBase mountainTile;
    private TileBase treeTile;
    private TileBase zoneTile;
    private TileBase wallTile;
    private TileBase frameTile;
    private TileBase doorFrameTile;
    private TileBase berryTile;
    private TileBase resourceTile;

    private Color currentZoneColor;

    private bool[,] passable;
    private float[,] heightMapCache;
    private float[,] temperatureMapCache;
    private float[,] humidityMapCache;
    private BiomeDefinition[,] biomeMapCache;
    private HashSet<Vector2Int> waterCellsCache = new HashSet<Vector2Int>();

    private readonly Dictionary<Vector2Int, float> berryTimers = new Dictionary<Vector2Int, float>();
    private int autoSeedCounter;

    /// <summary>
    /// The color used for the most recently created zone. Exposed so other
    /// components can create overlays even if they cannot place tiles through
    /// this generator.
    /// </summary>
    public Color CurrentZoneColor => currentZoneColor;

    public float[,] HeightMap => heightMapCache;
    public float[,] TemperatureMap => temperatureMapCache;
    public float[,] HumidityMap => humidityMapCache;
    public BiomeDefinition[,] BiomeMap => biomeMapCache;
    public IReadOnlyCollection<Vector2Int> WaterCells => waterCellsCache;

    /// <summary>
    /// Prepare a new zone tile using a random dim color. Call this before
    /// placing tiles for a new zone.
    /// </summary>
    public void BeginNewZone()
    {
        currentZoneColor = GenerateZoneColor();
        zoneTile = CreateColoredTile(currentZoneColor);
    }

    private Color GenerateZoneColor()
    {
        float h = UnityEngine.Random.value;
        float s = UnityEngine.Random.Range(0.2f, 0.4f);
        float v = UnityEngine.Random.Range(0.4f, 0.6f);
        Color c = Color.HSVToRGB(h, s, v);
        c.a = 0.3f;
        return c;
    }

    protected virtual void Awake()
    {
        if (groundTilemap == null || treeTilemap == null || zoneTilemap == null || wallTilemap == null)
        {
            var gridObj = new GameObject("Grid");
            gridObj.AddComponent<Grid>();

            groundTilemap = CreateLayer(gridObj.transform, "Ground", 0);
            treeTilemap = CreateLayer(gridObj.transform, "Trees", 1);
            berryTilemap = CreateLayer(gridObj.transform, "Berries", 2);
            resourceTilemap = CreateLayer(gridObj.transform, "Resources", 3);

            zoneTilemap = CreateLayer(gridObj.transform, "Zones", 10);
            wallTilemap = CreateLayer(gridObj.transform, "Walls", 5);
            frameTilemap = CreateLayer(gridObj.transform, "Frames", 6);
        }

        Transform parent = groundTilemap != null ? groundTilemap.transform.parent : null;
        if (parent != null)
        {
            if (treeTilemap == null)
                treeTilemap = CreateLayer(parent, "Trees", 1);
            if (berryTilemap == null)
                berryTilemap = CreateLayer(parent, "Berries", 2);
            if (resourceTilemap == null)
                resourceTilemap = CreateLayer(parent, "Resources", 3);
            if (zoneTilemap == null)
                zoneTilemap = CreateLayer(parent, "Zones", 10);
            if (wallTilemap == null)
                wallTilemap = CreateLayer(parent, "Walls", 5);
            if (frameTilemap == null)
                frameTilemap = CreateLayer(parent, "Frames", 6);
        }

        groundTile = CreateTileFromResource("grass");
        treeTile = CreateTileFromResource("tree");
        waterTile = CreateTileFromResource("water");
        mountainTile = CreateTileFromResource("stone");
        zoneTile = CreateColoredTile(zoneColor);
        wallTile = CreateColoredTile(wallColor);
        frameTile = CreateColoredTile(frameColor);
        doorFrameTile = CreateColoredTile(frameColor);
        berryTile = CreateColoredTile(berryColor);
        resourceTile = CreateColoredTile(resourceColor);
        currentZoneColor = zoneColor;
    }

    protected virtual Tilemap CreateLayer(Transform parent, string name, int sortingOrder)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var tilemap = obj.AddComponent<Tilemap>();
        var renderer = obj.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        return tilemap;
    }

    protected virtual void Start()
    {
        Generate();
        CenterCamera();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Generate 10 Sample Maps")]
    private void DebugGenerateSampleMaps()
    {
        int originalWidth = width;
        int originalHeight = height;
        for (int i = 0; i < 10; i++)
        {
            width = UnityEngine.Random.Range(Mathf.Max(16, originalWidth / 2), Mathf.Max(32, originalWidth * 2));
            height = UnityEngine.Random.Range(Mathf.Max(16, originalHeight / 2), Mathf.Max(32, originalHeight * 2));
            Generate();
            Debug.Log($"[MapGenerator] Generated sample map {i + 1}/10 ({width}x{height}).");
        }
        width = originalWidth;
        height = originalHeight;
        Generate();
    }
#endif

    public void Generate()
    {
        InitializeRandom();

        PrepareTilemaps();

        Vector2 heightOffset = RandomOffset();
        Vector2 temperatureOffset = RandomOffset();
        Vector2 humidityOffset = RandomOffset();

        heightMapCache = GenerateHeightMap(width, height, heightOffset);
        waterCellsCache = SimulateWaterFlow(heightMapCache);
        temperatureMapCache = GenerateTemperatureMap(heightMapCache, temperatureOffset);
        humidityMapCache = GenerateHumidityMap(heightMapCache, waterCellsCache, humidityOffset);
        biomeMapCache = GenerateBiomeMap(heightMapCache, temperatureMapCache, humidityMapCache);

        ApplyTilemaps(heightMapCache, temperatureMapCache, humidityMapCache, biomeMapCache, waterCellsCache);
    }

    protected virtual void InitializeRandom()
    {
        if (randomSeed >= 0)
        {
            UnityEngine.Random.InitState(randomSeed);
        }
        else
        {
            int seed = Environment.TickCount ^ autoSeedCounter++;
            UnityEngine.Random.InitState(seed);
        }
    }

    protected virtual void PrepareTilemaps()
    {
        passable = new bool[width, height];
        berryTimers.Clear();

        ClearTilemap(groundTilemap);
        ClearTilemap(treeTilemap);
        ClearTilemap(zoneTilemap);
        ClearTilemap(wallTilemap);
        ClearTilemap(frameTilemap);
        ClearTilemap(berryTilemap);
        ClearTilemap(resourceTilemap);
    }

    private void ClearTilemap(Tilemap tilemap)
    {
        if (tilemap != null)
        {
            tilemap.ClearAllTiles();
        }
    }

    protected virtual float[,] GenerateHeightMap(int mapWidth, int mapHeight, Vector2 offset)
    {
        return GenerateFractalNoise(mapWidth, mapHeight, heightNoiseScale, heightOctaves, heightPersistence, heightLacunarity, offset);
    }

    protected float[,] GenerateFractalNoise(int mapWidth, int mapHeight, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        if (scale <= 0f)
            scale = 0.0001f;

        float[,] map = new float[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float sampleX = (x + offset.x) * frequency * scale;
                    float sampleY = (y + offset.y) * frequency * scale;
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                map[x, y] = Mathf.InverseLerp(-1f, 1f, noiseHeight);
            }
        }

        return map;
    }

    protected virtual HashSet<Vector2Int> SimulateWaterFlow(float[,] heightMap)
    {
        var result = new HashSet<Vector2Int>();

        for (int x = 0; x < heightMap.GetLength(0); x++)
        {
            for (int y = 0; y < heightMap.GetLength(1); y++)
            {
                if (heightMap[x, y] <= seaLevel)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }
        }

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < heightMap.GetLength(0); x++)
        {
            for (int y = 0; y < heightMap.GetLength(1); y++)
            {
                if (heightMap[x, y] >= riverSourceHeight)
                {
                    candidates.Add(new Vector2Int(x, y));
                }
            }
        }

        candidates = candidates.OrderByDescending(p => heightMap[p.x, p.y]).ToList();
        int riversToSpawn = Mathf.Min(riverCount, candidates.Count);
        for (int i = 0; i < riversToSpawn && candidates.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            Vector2Int start = candidates[index];
            candidates.RemoveAt(index);
            SimulateRiver(heightMap, result, start);
        }

        return result;
    }

    private void SimulateRiver(float[,] heightMap, HashSet<Vector2Int> waterCells, Vector2Int start)
    {
        Vector2Int current = start;
        float currentHeight = heightMap[current.x, current.y];
        int steps = 0;
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        while (steps++ < maxRiverLength)
        {
            waterCells.Add(current);
            if (currentHeight <= seaLevel)
                break;

            Vector2Int next = GetDownhillNeighbor(heightMap, current, visited);
            float nextHeight = heightMap[next.x, next.y];

            if (next == current)
            {
                CreateLake(heightMap, waterCells, current, currentHeight);
                break;
            }

            visited.Add(current);
            current = next;
            currentHeight = nextHeight;
        }
    }

    private void CreateLake(float[,] heightMap, HashSet<Vector2Int> waterCells, Vector2Int center, float centerHeight)
    {
        int radius = 2;
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int pos = new Vector2Int(center.x + dx, center.y + dy);
                if (!InBounds(pos.x, pos.y))
                    continue;

                float h = heightMap[pos.x, pos.y];
                if (Mathf.Abs(h - centerHeight) <= lakeHeightTolerance)
                {
                    waterCells.Add(pos);
                }
            }
        }
    }

    private Vector2Int GetDownhillNeighbor(float[,] heightMap, Vector2Int pos, HashSet<Vector2Int> visited)
    {
        Vector2Int best = pos;
        float bestHeight = heightMap[pos.x, pos.y];
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int nx = pos.x + dx;
                int ny = pos.y + dy;
                if (!InBounds(nx, ny))
                    continue;
                Vector2Int n = new Vector2Int(nx, ny);
                if (visited.Contains(n))
                    continue;

                float h = heightMap[nx, ny];
                if (h < bestHeight)
                {
                    bestHeight = h;
                    best = n;
                }
            }
        }

        return best;
    }

    protected virtual float[,] GenerateTemperatureMap(float[,] heightMap, Vector2 offset)
    {
        float[,] noise = GenerateFractalNoise(width, height, temperatureNoiseScale, temperatureOctaves, temperaturePersistence, temperatureLacunarity, offset);
        float[,] result = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float normalized = Mathf.Clamp01(noise[x, y]);
                float temperature = Mathf.Lerp(minTemperature, maxTemperature, normalized);
                temperature -= heightMap[x, y] * temperatureAltitudeFactor;
                result[x, y] = temperature;
            }
        }

        return result;
    }

    protected virtual float[,] GenerateHumidityMap(float[,] heightMap, HashSet<Vector2Int> waterCells, Vector2 offset)
    {
        float[,] noise = GenerateFractalNoise(width, height, humidityNoiseScale, humidityOctaves, humidityPersistence, humidityLacunarity, offset);
        float[,] result = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float normalized = Mathf.Clamp01(noise[x, y]);
                float humidity = normalized;
                humidity -= heightMap[x, y] * humidityAltitudePenalty;

                float distance = DistanceToNearestWater(waterCells, x, y, humidityWaterRadius);
                if (distance >= 0f)
                {
                    float bonus = Mathf.Clamp01(1f - distance / Mathf.Max(1f, humidityWaterRadius));
                    humidity += bonus * humidityWaterBonus;
                }

                result[x, y] = Mathf.Clamp01(humidity);
            }
        }

        return result;
    }

    private float DistanceToNearestWater(HashSet<Vector2Int> waterCells, int x, int y, int radius)
    {
        float best = float.MaxValue;
        bool found = false;
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (!InBounds(nx, ny))
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

    protected virtual BiomeDefinition[,] GenerateBiomeMap(float[,] heightMap, float[,] temperatureMap, float[,] humidityMap)
    {
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

                BiomeDefinition biome = SelectBiome(seeds, x, y, heightValue, temperature, humidity);
                result[x, y] = biome;
            }
        }

        return result;
    }

    private BiomeDefinition SelectBiome(List<BiomeSeed> seeds, int x, int y, float height, float temperature, float humidity)
    {
        Vector2 pos = new Vector2((float)x / Mathf.Max(1, width - 1), (float)y / Mathf.Max(1, height - 1));
        BiomeDefinition chosen = null;
        float bestScore = float.MinValue;

        foreach (var seed in seeds)
        {
            float distance = Vector2.Distance(pos, seed.position);
            float distanceWeight = Mathf.Exp(-distance * biomeBlendStrength);
            float fitness = seed.biome.EvaluateFitness(height, temperature, humidity);
            if (seed.biome.Matches(height, temperature, humidity))
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
                float score = biome.EvaluateFitness(height, temperature, humidity);
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
                position = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
            });
        }

        int additionalSeeds = Mathf.Max(0, voronoiSeedCount - biomeDefinitions.Count);
        for (int i = 0; i < additionalSeeds; i++)
        {
            var biome = biomeDefinitions[UnityEngine.Random.Range(0, biomeDefinitions.Count)];
            Vector2 randomPos = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value);
            randomPos += UnityEngine.Random.insideUnitCircle * voronoiJitter * 0.5f;
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

    protected virtual void ApplyTilemaps(float[,] heightMap, float[,] temperatureMap, float[,] humidityMap, BiomeDefinition[,] biomeMap, HashSet<Vector2Int> waterCells)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                Vector2Int cell = new Vector2Int(x, y);

                ClearDecorationsAt(pos);

                if (waterCells.Contains(cell))
                {
                    PaintGround(pos, waterTile, waterColor);
                    passable[x, y] = false;
                    continue;
                }

                float heightValue = heightMap[x, y];
                if (heightValue >= mountainThreshold)
                {
                    PaintGround(pos, mountainTile, mountainColor);
                    passable[x, y] = false;
                    continue;
                }

                BiomeDefinition biome = biomeMap[x, y];
                TileBase baseTile = ResolveGroundTile(biome);
                Color tint = biome != null ? biome.groundTint : grassColor;
                PaintGround(pos, baseTile ?? groundTile, tint);

                bool cellPassable = true;
                if (biome != null)
                {
                    cellPassable &= ApplyBiomeDecorations(pos, biome);
                }

                // Fallbacks for cases when the biome does not define flora/resources.
                if (biome == null || biome.flora.Count == 0)
                {
                    cellPassable &= MaybeSpawnLegacyTree(pos);
                }

                if (biome == null || biome.resources.Count == 0)
                {
                    cellPassable &= MaybeSpawnLegacyBerry(pos);
                }

                passable[x, y] = cellPassable;
            }
        }
    }

    private void ClearDecorationsAt(Vector3Int pos)
    {
        if (treeTilemap != null)
            treeTilemap.SetTile(pos, null);
        if (berryTilemap != null)
            berryTilemap.SetTile(pos, null);
        if (resourceTilemap != null)
            resourceTilemap.SetTile(pos, null);
    }

    protected virtual TileBase ResolveGroundTile(BiomeDefinition biome)
    {
        if (biome != null && biome.groundTile != null)
            return biome.groundTile;
        return groundTile;
    }

    protected virtual bool ApplyBiomeDecorations(Vector3Int pos, BiomeDefinition biome)
    {
        bool passableHere = true;
        if (biome == null)
            return passableHere;

        if (biome.flora != null)
        {
            foreach (var flora in biome.flora)
            {
                if (flora == null || flora.tile == null)
                    continue;
                if (UnityEngine.Random.value > flora.probability)
                    continue;

                if (treeTilemap != null)
                {
                    treeTilemap.SetTile(pos, flora.tile);
                    if (flora.blocksMovement)
                        passableHere = false;
                }
            }
        }

        if (biome.resources != null)
        {
            foreach (var resource in biome.resources)
            {
                if (resource == null)
                    continue;

                TileBase resourceTileToUse = resource.tile != null ? resource.tile : resourceTile;
                if (resourceTileToUse == null)
                    continue;

                if (UnityEngine.Random.value > resource.probability)
                    continue;

                PlaceResourceTile(pos, resource, resourceTileToUse);
                if (resource.blocksMovement)
                    passableHere = false;
            }
        }

        return passableHere;
    }

    private void PlaceResourceTile(Vector3Int pos, BiomeDefinition.ResourceOption resource, TileBase tile)
    {
        Tilemap target = resource.useBerryLayer ? berryTilemap : resourceTilemap;
        if (target == null)
            return;

        target.SetTile(pos, tile);
        target.SetTileFlags(pos, TileFlags.None);
        target.SetColor(pos, resource.useBerryLayer ? berryColor : resourceColor);
    }

    private bool MaybeSpawnLegacyTree(Vector3Int pos)
    {
        if (treeTilemap == null || treeTile == null)
            return true;

        if (UnityEngine.Random.value < treeProbability)
        {
            treeTilemap.SetTile(pos, treeTile);
            treeTilemap.SetTileFlags(pos, TileFlags.None);
            treeTilemap.SetColor(pos, treeColor);
            return false;
        }

        return true;
    }

    private bool MaybeSpawnLegacyBerry(Vector3Int pos)
    {
        if (berryTilemap == null || berryTile == null)
            return true;

        if (UnityEngine.Random.value < berryProbability)
        {
            berryTilemap.SetTile(pos, berryTile);
            berryTilemap.SetTileFlags(pos, TileFlags.None);
            berryTilemap.SetColor(pos, berryColor);
            return false;
        }

        return true;
    }

    private void PaintGround(Vector3Int pos, TileBase tile, Color tint)
    {
        if (groundTilemap == null)
            return;

        if (tile == null)
            tile = groundTile;

        groundTilemap.SetTile(pos, tile);
        groundTilemap.SetTileFlags(pos, TileFlags.None);
        groundTilemap.SetColor(pos, tint);
    }

    private Vector2 RandomOffset()
    {
        return new Vector2(UnityEngine.Random.Range(0f, 1000f), UnityEngine.Random.Range(0f, 1000f));
    }

    public bool IsPassable(int x, int y)
    {
        if (!InBounds(x, y))
            return false;
        return passable[x, y];
    }

    private bool InBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool HasTree(int x, int y)
    {
        if (treeTilemap == null || !InBounds(x, y))
            return false;
        return treeTilemap.GetTile(new Vector3Int(x, y, 0)) != null;
    }

    public void HighlightTree(int x, int y, Color tint)
    {
        if (treeTilemap == null || !InBounds(x, y))
            return;

        Vector3Int pos = new Vector3Int(x, y, 0);
        treeTilemap.SetTileFlags(pos, TileFlags.None);
        treeTilemap.SetColor(pos, tint);
    }

    public void RemoveTree(int x, int y)
    {
        if (treeTilemap == null || !InBounds(x, y))
            return;

        treeTilemap.SetTile(new Vector3Int(x, y, 0), null);
        if (passable != null && InBounds(x, y))
            passable[x, y] = true;

        int amount = UnityEngine.Random.Range(30, 51);
        WoodLog.Create(new Vector2(x + 0.5f, y + 0.5f), amount);
    }

    public bool HasBerries(int x, int y)
    {
        if (berryTilemap == null || !InBounds(x, y))
            return false;
        return berryTilemap.GetTile(new Vector3Int(x, y, 0)) != null;
    }

    public void RemoveBerries(int x, int y)
    {
        if (berryTilemap == null || !InBounds(x, y))
            return;

        berryTilemap.SetTile(new Vector3Int(x, y, 0), null);
        if (passable != null && InBounds(x, y))
            passable[x, y] = true;
        var cell = new Vector2Int(x, y);
        berryTimers[cell] = berryGrowTime;
    }

    public bool TryFindClosestBerryCell(Vector2 pos, out Vector2Int cell)
    {
        cell = Vector2Int.zero;
        if (berryTilemap == null)
            return false;
        float best = float.MaxValue;
        bool found = false;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!HasBerries(x, y))
                    continue;
                float d = Vector2.Distance(pos, new Vector2(x + 0.5f, y + 0.5f));
                if (d < best)
                {
                    best = d;
                    cell = new Vector2Int(x, y);
                    found = true;
                }
            }
        }
        return found;
    }

    public void SetZone(int x, int y)
    {
        if (zoneTilemap == null || !IsPassable(x, y))
            return;

        zoneTilemap.SetTile(new Vector3Int(x, y, 0), zoneTile);
        ZoneOverlay.Create(new Vector2(x + 0.5f, y + 0.5f), currentZoneColor);
    }

    public void PlaceWall(int x, int y)
    {
        if (wallTilemap == null || !IsPassable(x, y))
            return;

        wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
        passable[x, y] = false;
    }

    public void PlaceWallFrame(int x, int y)
    {
        if (frameTilemap == null || !IsPassable(x, y) || HasWall(x, y)
            || HasDoor(x, y) || HasBed(x, y) || HasDoorFrame(x, y) || HasBedFrame(x, y))
            return;
        frameTilemap.SetTile(new Vector3Int(x, y, 0), frameTile);
    }

    public bool HasWallFrame(int x, int y)
    {
        if (frameTilemap == null || !InBounds(x, y))
            return false;
        return frameTilemap.GetTile(new Vector3Int(x, y, 0)) != null;
    }

    public void BuildWallFromFrame(int x, int y)
    {
        if (!HasWallFrame(x, y) || wallTilemap == null)
            return;
        frameTilemap.SetTile(new Vector3Int(x, y, 0), null);
        PlaceWall(x, y);
    }

    public bool HasWall(int x, int y)
    {
        if (wallTilemap == null || !InBounds(x, y))
            return false;
        return wallTilemap.GetTile(new Vector3Int(x, y, 0)) != null;
    }

    public void PlaceDoorFrame(int x, int y)
    {
        if (frameTilemap == null || !IsPassable(x, y) || HasWall(x, y) || HasDoor(x, y))
            return;
        frameTilemap.SetTile(new Vector3Int(x, y, 0), doorFrameTile);
    }

    public bool HasDoorFrame(int x, int y)
    {
        if (frameTilemap == null || !InBounds(x, y))
            return false;
        return frameTilemap.GetTile(new Vector3Int(x, y, 0)) == doorFrameTile;
    }

    public void BuildDoorFromFrame(int x, int y)
    {
        if (!HasDoorFrame(x, y))
            return;
        frameTilemap.SetTile(new Vector3Int(x, y, 0), null);
        Door.Create(new Vector2(x + 0.5f, y + 0.5f));
    }

    public void PlaceBedFrame(int x, int y)
    {
        if (frameTilemap == null || !IsPassable(x, y) || HasWall(x, y) || HasDoor(x, y))
            return;
        frameTilemap.SetTile(new Vector3Int(x, y, 0), frameTile);
    }

    public bool HasBedFrame(int x, int y)
    {
        if (frameTilemap == null || !InBounds(x, y))
            return false;
        return frameTilemap.GetTile(new Vector3Int(x, y, 0)) == frameTile;
    }

    public void BuildBedFromFrame(int x, int y)
    {
        if (!HasBedFrame(x, y))
            return;
        frameTilemap.SetTile(new Vector3Int(x, y, 0), null);
        Bed.Create(new Vector2(x + 0.5f, y + 0.5f));
    }

    public bool HasDoor(int x, int y)
    {
        Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
        Collider2D[] cols = Physics2D.OverlapPointAll(p);
        foreach (var col in cols)
        {
            if (col != null && col.GetComponent<Door>() != null)
                return true;
        }
        return false;
    }

    public bool HasBed(int x, int y)
    {
        Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
        Collider2D[] cols = Physics2D.OverlapPointAll(p);
        foreach (var col in cols)
        {
            if (col != null && col.GetComponent<Bed>() != null)
                return true;
        }
        return false;
    }

    protected virtual Tile CreateColoredTile(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        return tile;
    }

    protected virtual Tile CreateTileFromResource(string name)
    {
        Sprite sprite = Resources.Load<Sprite>("Textures/" + name);
        if (sprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("Textures/" + name);
            if (tex != null)
            {
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
            }
        }

        if (sprite == null)
        {
            Debug.LogError($"Sprite '{name}' not found in Resources/Textures");
            return CreateColoredTile(Color.magenta);
        }

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        return tile;
    }

    private void CenterCamera()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.transform.position = new Vector3(width / 2f, height / 2f, -10f);
            cam.orthographicSize = Mathf.Max(width, height) / 2f;
        }
    }

    protected virtual void Update()
    {
        if (berryTimers.Count == 0)
            return;

        var keys = new List<Vector2Int>(berryTimers.Keys);
        foreach (var cell in keys)
        {
            float time = berryTimers[cell] - Time.deltaTime;
            if (time <= 0f)
            {
                if (berryTilemap != null && berryTile != null)
                {
                    Vector3Int pos = new Vector3Int(cell.x, cell.y, 0);
                    berryTilemap.SetTile(pos, berryTile);
                    berryTilemap.SetTileFlags(pos, TileFlags.None);
                    berryTilemap.SetColor(pos, berryColor);
                }
                if (passable != null && InBounds(cell.x, cell.y))
                    passable[cell.x, cell.y] = false;
                berryTimers.Remove(cell);
            }
            else
            {
                berryTimers[cell] = time;
            }
        }
    }
}
