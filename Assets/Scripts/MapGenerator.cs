using System;
using System.Collections.Generic;
using FallowEarth.MapGeneration;
using FallowEarth.Navigation;
using FallowEarth.ResourcesSystem;
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

    [Header("Generation Strategies")]
    [SerializeField]
    private HeightMapGeneratorBase heightMapGenerator;

    [SerializeField]
    private WaterSimulatorBase waterSimulator;

    [SerializeField]
    private TemperatureMapGeneratorBase temperatureGenerator;

    [SerializeField]
    private HumidityMapGeneratorBase humidityGenerator;

    [SerializeField]
    private BiomePainterBase biomePainter;

    [SerializeField]
    private TilemapApplierBase tilemapApplier;

    [Header("Legacy Vegetation Settings")]
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

        EnsureStrategies();
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

    private void EnsureStrategies()
    {
        if (heightMapGenerator == null)
            heightMapGenerator = ScriptableObject.CreateInstance<PerlinHeightMapGenerator>();
        if (waterSimulator == null)
            waterSimulator = ScriptableObject.CreateInstance<DefaultWaterSimulator>();
        if (temperatureGenerator == null)
            temperatureGenerator = ScriptableObject.CreateInstance<PerlinTemperatureMapGenerator>();
        if (humidityGenerator == null)
            humidityGenerator = ScriptableObject.CreateInstance<PerlinHumidityMapGenerator>();
        if (biomePainter == null)
            biomePainter = ScriptableObject.CreateInstance<VoronoiBiomePainter>();
        if (tilemapApplier == null)
            tilemapApplier = ScriptableObject.CreateInstance<DefaultTilemapApplier>();
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

        heightMapCache = heightMapGenerator?.GenerateHeightMap(width, height, heightOffset) ?? new float[width, height];
        waterCellsCache = waterSimulator?.SimulateWater(heightMapCache) ?? new HashSet<Vector2Int>();
        temperatureMapCache = temperatureGenerator?.GenerateTemperatureMap(heightMapCache, temperatureOffset) ?? new float[width, height];
        humidityMapCache = humidityGenerator?.GenerateHumidityMap(heightMapCache, waterCellsCache, humidityOffset) ?? new float[width, height];
        biomeMapCache = biomePainter?.PaintBiomes(heightMapCache, temperatureMapCache, humidityMapCache) ?? new BiomeDefinition[width, height];

        if (tilemapApplier != null)
        {
            var context = BuildTilemapContext();
            tilemapApplier.Apply(context);
            passable = context.Passable;
        }

        PathfindingService.Instance?.Initialize(width, height, passable);
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

    private TilemapApplierContext BuildTilemapContext()
    {
        return new TilemapApplierContext
        {
            Width = width,
            Height = height,
            HeightMap = heightMapCache,
            TemperatureMap = temperatureMapCache,
            HumidityMap = humidityMapCache,
            BiomeMap = biomeMapCache,
            WaterCells = waterCellsCache,
            Passable = passable,
            GroundLayer = groundTilemap != null ? new TilemapLayerAdapter(groundTilemap) : null,
            TreeLayer = treeTilemap != null ? new TilemapLayerAdapter(treeTilemap) : null,
            BerryLayer = berryTilemap != null ? new TilemapLayerAdapter(berryTilemap) : null,
            ResourceLayer = resourceTilemap != null ? new TilemapLayerAdapter(resourceTilemap) : null,
            GroundTile = groundTile,
            WaterTile = waterTile,
            MountainTile = mountainTile,
            TreeTile = treeTile,
            BerryTile = berryTile,
            ResourceTile = resourceTile,
            GrassColor = grassColor,
            WaterColor = waterColor,
            MountainColor = mountainColor,
            TreeColor = treeColor,
            BerryColor = berryColor,
            ResourceColor = resourceColor
        };
    }

    private void ClearTilemap(Tilemap tilemap)
    {
        if (tilemap != null)
        {
            tilemap.ClearAllTiles();
        }
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
        {
            passable[x, y] = true;
            PathfindingService.Instance?.SetWalkable(new Vector2Int(x, y), true);
        }

        int amount = UnityEngine.Random.Range(30, 51);
        var qualityRoll = UnityEngine.Random.value;
        var quality = FallowEarth.ResourcesSystem.ResourceQuality.Common;
        if (qualityRoll > 0.95f)
            quality = FallowEarth.ResourcesSystem.ResourceQuality.Masterwork;
        else if (qualityRoll > 0.8f)
            quality = FallowEarth.ResourcesSystem.ResourceQuality.Fine;
        else if (qualityRoll < 0.1f)
            quality = FallowEarth.ResourcesSystem.ResourceQuality.Defective;
        WoodLog.Create(new Vector2(x + 0.5f, y + 0.5f), amount, quality);
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
        {
            passable[x, y] = true;
            PathfindingService.Instance?.SetWalkable(new Vector2Int(x, y), true);
        }
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
        PathfindingService.Instance?.SetWalkable(new Vector2Int(x, y), false);
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
                {
                    passable[cell.x, cell.y] = false;
                    PathfindingService.Instance?.SetWalkable(cell, false);
                }
                berryTimers.Remove(cell);
            }
            else
            {
                berryTimers[cell] = time;
            }
        }
    }

    private sealed class TilemapLayerAdapter : ITileLayer
    {
        private readonly Tilemap tilemap;

        public TilemapLayerAdapter(Tilemap tilemap)
        {
            this.tilemap = tilemap;
        }

        public void SetTile(Vector3Int position, TileBase tile)
        {
            tilemap.SetTile(position, tile);
        }

        public void SetTileFlags(Vector3Int position, TileFlags flags)
        {
            tilemap.SetTileFlags(position, flags);
        }

        public void SetColor(Vector3Int position, Color color)
        {
            tilemap.SetColor(position, color);
        }
    }
}
