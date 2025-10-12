using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FallowEarth.MapGeneration
{
    [CreateAssetMenu(fileName = "WaterSimulator", menuName = "World/Map Generation/Water/Default")]
    public class DefaultWaterSimulator : WaterSimulatorBase
    {
        [SerializeField, Range(0f, 1f)]
        private float seaLevel = 0.32f;

        [SerializeField]
        private int riverCount = 4;

        [SerializeField]
        private int maxRiverLength = 300;

        [SerializeField, Range(0f, 1f)]
        private float riverSourceHeight = 0.65f;

        [SerializeField]
        private float lakeHeightTolerance = 0.02f;

        public override HashSet<Vector2Int> SimulateWater(float[,] heightMap)
        {
            var result = new HashSet<Vector2Int>();
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (heightMap[x, y] <= seaLevel)
                    {
                        result.Add(new Vector2Int(x, y));
                    }
                }
            }

            List<Vector2Int> candidates = new List<Vector2Int>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
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
                int index = Random.Range(0, candidates.Count);
                Vector2Int start = candidates[index];
                candidates.RemoveAt(index);
                SimulateRiver(heightMap, result, start, width, height);
            }

            return result;
        }

        private void SimulateRiver(float[,] heightMap, HashSet<Vector2Int> waterCells, Vector2Int start, int width, int height)
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

                Vector2Int next = GetDownhillNeighbor(heightMap, current, visited, width, height);
                float nextHeight = heightMap[next.x, next.y];

                if (next == current)
                {
                    CreateLake(heightMap, waterCells, current, currentHeight, width, height);
                    break;
                }

                visited.Add(current);
                current = next;
                currentHeight = nextHeight;
            }
        }

        private void CreateLake(float[,] heightMap, HashSet<Vector2Int> waterCells, Vector2Int center, float centerHeight, int width, int height)
        {
            int radius = 2;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int pos = new Vector2Int(center.x + dx, center.y + dy);
                    if (!InBounds(pos.x, pos.y, width, height))
                        continue;

                    float h = heightMap[pos.x, pos.y];
                    if (Mathf.Abs(h - centerHeight) <= lakeHeightTolerance)
                    {
                        waterCells.Add(pos);
                    }
                }
            }
        }

        private Vector2Int GetDownhillNeighbor(float[,] heightMap, Vector2Int pos, HashSet<Vector2Int> visited, int width, int height)
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
                    if (!InBounds(nx, ny, width, height))
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

        private bool InBounds(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}
