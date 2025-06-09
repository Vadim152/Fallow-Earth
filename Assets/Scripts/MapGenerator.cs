using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap obstacleTilemap;
    public Color groundColor = new Color(0.2f, 0.6f, 0.2f);
    public Color obstacleColor = Color.gray;
    public int width = 50;
    public int height = 50;
    [Range(0f,1f)]
    public float obstacleProbability = 0.2f;

    private TileBase groundTile;
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

        groundTile = CreateColoredTile(groundColor);
        obstacleTile = CreateColoredTile(obstacleColor);
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

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (groundTilemap != null && groundTile != null)
                    groundTilemap.SetTile(pos, groundTile);
                if (Random.value < obstacleProbability)
                {
                    if (obstacleTilemap != null && obstacleTile != null)
                        obstacleTilemap.SetTile(pos, obstacleTile);
                    passable[x, y] = false;
                }
                else
                {
                    passable[x, y] = true;
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
