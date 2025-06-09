using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap obstacleTilemap;

    public Color grassColor = new Color(0.2f, 0.6f, 0.2f);
    public Color waterColor = new Color(0.2f, 0.2f, 0.7f);
    public Color mountainColor = Color.gray;
    public Color obstacleColor = new Color(0.25f, 0.2f, 0.1f);
    public int width = 50;
    public int height = 50;
    [Range(0f,1f)]
    public float obstacleProbability = 0.1f;

    [Header("Noise Settings")]
    public float noiseScale = 0.1f;
    [Range(0f,1f)]
    public float waterThreshold = 0.3f;
    [Range(0f,1f)]
    public float mountainThreshold = 0.7f;

    private TileBase groundTile;
    private TileBase waterTile;
    private TileBase mountainTile;
    private TileBase obstacleTile;

    private bool[,] passable;

    void Awake()
    {
        if (groundTilemap == null || obstacleTilemap == null)
        {
            var gridObj = new GameObject("Grid");
            var grid = gridObj.AddComponent<Grid>();

            var groundObj = new GameObject("Ground");
            groundObj.transform.parent = gridObj.transform;
            groundTilemap = groundObj.AddComponent<Tilemap>();
            groundObj.AddComponent<TilemapRenderer>();

            var obstacleObj = new GameObject("Obstacles");
            obstacleObj.transform.parent = gridObj.transform;
            obstacleTilemap = obstacleObj.AddComponent<Tilemap>();
            obstacleObj.AddComponent<TilemapRenderer>();
        }

        groundTile = CreateColoredTile(grassColor);
        obstacleTile = CreateColoredTile(obstacleColor);
        waterTile = CreateColoredTile(waterColor);
        mountainTile = CreateColoredTile(mountainColor);
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
        if (obstacleTilemap != null)
            obstacleTilemap.ClearAllTiles();

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

                if (passable[x, y] && Random.value < obstacleProbability)
                {
                    if (obstacleTilemap != null && obstacleTile != null)
                        obstacleTilemap.SetTile(pos, obstacleTile);
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
