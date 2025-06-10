using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap treeTilemap;
    public Tilemap zoneTilemap;

    public Color grassColor = new Color(0.2f, 0.6f, 0.2f);
    public Color waterColor = new Color(0.2f, 0.2f, 0.7f);
    public Color mountainColor = Color.gray;
    public Color treeColor = new Color(0.25f, 0.2f, 0.1f);
    public Color zoneColor = new Color(1f, 1f, 0f, 0.4f);
    public int width = 200;
    public int height = 200;
    [Range(0f,1f)]
    public float treeProbability = 0.1f;

    [Header("Noise Settings")]
    public float noiseScale = 0.1f;
    [Range(0f,1f)]
    public float waterThreshold = 0.3f;
    [Range(0f,1f)]
    public float mountainThreshold = 0.7f;

    private TileBase groundTile;
    private TileBase waterTile;
    private TileBase mountainTile;
    private TileBase treeTile;
    private TileBase zoneTile;
    private Color currentZoneColor;

    private bool[,] passable;

    /// <summary>
    /// Prepare a new zone tile using a random dim color.
    /// Call this before placing tiles for a new zone.
    /// </summary>
    public void BeginNewZone()
    {
        currentZoneColor = GenerateZoneColor();
        zoneTile = CreateColoredTile(currentZoneColor);
    }

    private Color GenerateZoneColor()
    {
        float h = Random.value;
        float s = Random.Range(0.2f, 0.4f);
        float v = Random.Range(0.4f, 0.6f);
        Color c = Color.HSVToRGB(h, s, v);
        c.a = 0.3f;
        return c;
    }

    void Awake()
    {
        if (groundTilemap == null || treeTilemap == null || zoneTilemap == null)
        {
            var gridObj = new GameObject("Grid");
            var grid = gridObj.AddComponent<Grid>();

            var groundObj = new GameObject("Ground");
            groundObj.transform.parent = gridObj.transform;
            groundTilemap = groundObj.AddComponent<Tilemap>();
            groundObj.AddComponent<TilemapRenderer>();

            var treeObj = new GameObject("Trees");
            treeObj.transform.parent = gridObj.transform;
            treeTilemap = treeObj.AddComponent<Tilemap>();
            treeObj.AddComponent<TilemapRenderer>();

            var zoneObj = new GameObject("Zones");
            zoneObj.transform.parent = gridObj.transform;
            zoneTilemap = zoneObj.AddComponent<Tilemap>();
            var zr = zoneObj.AddComponent<TilemapRenderer>();
            zr.sortingOrder = 10;
        }

        groundTile = CreateTileFromResource("grass");
        treeTile = CreateTileFromResource("tree");
        waterTile = CreateTileFromResource("water");
        mountainTile = CreateTileFromResource("stone");
        zoneTile = CreateColoredTile(zoneColor);
        currentZoneColor = zoneColor;
    }

    void Start()
    {
        Generate();
        CenterCamera();
    }

    public void Generate()
    {
        passable = new bool[width, height];
        if (groundTilemap != null)
            groundTilemap.ClearAllTiles();
        if (treeTilemap != null)
            treeTilemap.ClearAllTiles();
        if (zoneTilemap != null)
            zoneTilemap.ClearAllTiles();

        Vector2 noiseOffset = new Vector2(Random.Range(0f, 1000f), Random.Range(0f, 1000f));

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                float n = Mathf.PerlinNoise((x + noiseOffset.x) * noiseScale, (y + noiseOffset.y) * noiseScale);

                TileBase tile;
                if (n < waterThreshold)
                {
                    tile = waterTile;
                    passable[x, y] = false;
                }
                else if (n > mountainThreshold)
                {
                    tile = mountainTile;
                    passable[x, y] = false;
                }
                else
                {
                    tile = groundTile;
                    passable[x, y] = true;
                }

                if (groundTilemap != null)
                    groundTilemap.SetTile(pos, tile);

                if (passable[x, y] && Random.value < treeProbability)
                {
                    if (treeTilemap != null && treeTile != null)
                        treeTilemap.SetTile(pos, treeTile);
                    passable[x, y] = false;
                }
            }
        }
    }

    public bool IsPassable(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;
        return passable[x, y];
    }

    public bool HasTree(int x, int y)
    {
        if (treeTilemap == null || x < 0 || x >= width || y < 0 || y >= height)
            return false;
        return treeTilemap.GetTile(new Vector3Int(x, y, 0)) != null;
    }

    public void HighlightTree(int x, int y, Color tint)
    {
        if (treeTilemap == null || x < 0 || x >= width || y < 0 || y >= height)
            return;

        Vector3Int pos = new Vector3Int(x, y, 0);
        treeTilemap.SetTileFlags(pos, TileFlags.None);
        treeTilemap.SetColor(pos, tint);
    }

    public void RemoveTree(int x, int y)
    {
        if (treeTilemap == null || x < 0 || x >= width || y < 0 || y >= height)
            return;

        treeTilemap.SetTile(new Vector3Int(x, y, 0), null);
        if (passable != null && x < passable.GetLength(0) && y < passable.GetLength(1))
            passable[x, y] = true;

        WoodLog.Create(new Vector2(x + 0.5f, y + 0.5f));
    }

    public void SetZone(int x, int y)
    {
        if (zoneTilemap == null || !IsPassable(x, y))
            return;

        zoneTilemap.SetTile(new Vector3Int(x, y, 0), zoneTile);
        ZoneOverlay.Create(new Vector2(x + 0.5f, y + 0.5f), currentZoneColor);
    }

    private Tile CreateColoredTile(Color color)
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

    private Tile CreateTileFromResource(string name)
    {

        // Try to load a Sprite directly. If the texture was not imported as a
        // Sprite we attempt to load the Texture2D and create a Sprite at runtime.
        Sprite sprite = Resources.Load<Sprite>("Textures/" + name);
        if (sprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("Textures/" + name);
            if (tex != null)
            {
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100);
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
}
