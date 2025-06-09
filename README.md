# Fallow-Earth

## Map Generation

This project includes a simple `MapGenerator` script found in `Assets/Scripts`.
It uses Unity's **Grid** and **Tilemap** systems to build a tile-based map at
runtime. The generator fills the ground layer with tiles and randomly places
trees. Each cell's passability is stored in a `bool[,]` array, accessible via
`IsPassable(x, y)`.

### Usage
1. Create a parent GameObject with a `Grid` component.
2. Add two child objects, each with `Tilemap` and `TilemapRenderer` components –
   one for ground and another for trees.
3. Attach `MapGenerator` to the parent and assign the Tilemaps and tile assets in
   the Inspector.
4. Configure map size and tree probability, then run the scene to generate
   the map.
