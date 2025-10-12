using UnityEngine;
using UnityEngine.Tilemaps;

namespace FallowEarth.MapGeneration
{
    [CreateAssetMenu(fileName = "DefaultTilemapApplier", menuName = "World/Map Generation/Tiles/Default")]
    public class DefaultTilemapApplier : TilemapApplierBase, IMountainThresholdProvider
    {
        [SerializeField, Range(0f, 1f)]
        private float mountainThreshold = 0.78f;

        [SerializeField, Range(0f, 1f)]
        private float treeProbability = 0.1f;

        [SerializeField, Range(0f, 1f)]
        private float berryProbability = 0.05f;

        public float MountainThreshold => mountainThreshold;

        public override void Apply(TilemapApplierContext context)
        {
            if (context == null)
            {
                return;
            }

            if (context.Passable == null)
            {
                context.Passable = new bool[context.Width, context.Height];
            }

            for (int x = 0; x < context.Width; x++)
            {
                for (int y = 0; y < context.Height; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    Vector2Int cell = new Vector2Int(x, y);

                    ClearDecorationsAt(pos, context);

                    if (context.WaterCells.Contains(cell))
                    {
                        PaintGround(pos, context.WaterTile, context.WaterColor, context);
                        context.Passable[x, y] = false;
                        continue;
                    }

                    float heightValue = context.HeightMap[x, y];
                    if (heightValue >= mountainThreshold)
                    {
                        PaintGround(pos, context.MountainTile, context.MountainColor, context);
                        context.Passable[x, y] = false;
                        continue;
                    }

                    BiomeDefinition biome = context.BiomeMap[x, y];
                    TileBase baseTile = ResolveGroundTile(biome, context.GroundTile);
                    Color tint = biome != null ? biome.groundTint : context.GrassColor;
                    PaintGround(pos, baseTile ?? context.GroundTile, tint, context);

                    bool cellPassable = true;
                    if (biome != null)
                    {
                        cellPassable &= ApplyBiomeDecorations(pos, biome, context);
                    }

                    if (biome == null || biome.flora.Count == 0)
                    {
                        cellPassable &= MaybeSpawnLegacyTree(pos, context);
                    }

                    if (biome == null || biome.resources.Count == 0)
                    {
                        cellPassable &= MaybeSpawnLegacyBerry(pos, context);
                    }

                    context.Passable[x, y] = cellPassable;
                }
            }
        }

        private void ClearDecorationsAt(Vector3Int pos, TilemapApplierContext context)
        {
            context.TreeLayer?.SetTile(pos, null);
            context.BerryLayer?.SetTile(pos, null);
            context.ResourceLayer?.SetTile(pos, null);
        }

        private TileBase ResolveGroundTile(BiomeDefinition biome, TileBase fallback)
        {
            if (biome != null && biome.groundTile != null)
                return biome.groundTile;
            return fallback;
        }

        private bool ApplyBiomeDecorations(Vector3Int pos, BiomeDefinition biome, TilemapApplierContext context)
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
                    if (Random.value > flora.probability)
                        continue;

                    if (context.TreeLayer != null)
                    {
                        context.TreeLayer.SetTile(pos, flora.tile);
                        context.TreeLayer.SetTileFlags(pos, TileFlags.None);
                        context.TreeLayer.SetColor(pos, context.TreeColor);
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

                    TileBase resourceTileToUse = resource.tile != null ? resource.tile : context.ResourceTile;
                    if (resourceTileToUse == null)
                        continue;

                    if (Random.value > resource.probability)
                        continue;

                    PlaceResourceTile(pos, resource, resourceTileToUse, context);
                    if (resource.blocksMovement)
                        passableHere = false;
                }
            }

            return passableHere;
        }

        private void PlaceResourceTile(Vector3Int pos, BiomeDefinition.ResourceOption resource, TileBase tile, TilemapApplierContext context)
        {
            ITileLayer target = resource.useBerryLayer ? context.BerryLayer : context.ResourceLayer;
            if (target == null)
                return;

            target.SetTile(pos, tile);
            target.SetTileFlags(pos, TileFlags.None);
            target.SetColor(pos, resource.useBerryLayer ? context.BerryColor : context.ResourceColor);
        }

        private bool MaybeSpawnLegacyTree(Vector3Int pos, TilemapApplierContext context)
        {
            if (context.TreeLayer == null || context.TreeTile == null)
                return true;

            if (Random.value < treeProbability)
            {
                context.TreeLayer.SetTile(pos, context.TreeTile);
                context.TreeLayer.SetTileFlags(pos, TileFlags.None);
                context.TreeLayer.SetColor(pos, context.TreeColor);
                return false;
            }

            return true;
        }

        private bool MaybeSpawnLegacyBerry(Vector3Int pos, TilemapApplierContext context)
        {
            if (context.BerryLayer == null || context.BerryTile == null)
                return true;

            if (Random.value < berryProbability)
            {
                context.BerryLayer.SetTile(pos, context.BerryTile);
                context.BerryLayer.SetTileFlags(pos, TileFlags.None);
                context.BerryLayer.SetColor(pos, context.BerryColor);
                return false;
            }

            return true;
        }

        private void PaintGround(Vector3Int pos, TileBase tile, Color tint, TilemapApplierContext context)
        {
            if (context.GroundLayer == null)
                return;

            TileBase tileToUse = tile ?? context.GroundTile;
            context.GroundLayer.SetTile(pos, tileToUse);
            context.GroundLayer.SetTileFlags(pos, TileFlags.None);
            context.GroundLayer.SetColor(pos, tint);
        }
    }
}
