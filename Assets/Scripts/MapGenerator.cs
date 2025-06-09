using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap obstacleTilemap;
    public TileBase groundTile;
    public TileBase obstacleTile;
    public int width = 50;
    public int height = 50;
    [Range(0f,1f)]
    public float obstacleProbability = 0.2f;

    private bool[,] passable;

    void Start()
    {
        Generate();
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
}
