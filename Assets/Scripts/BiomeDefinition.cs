using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Describes a biome that can be painted on the generated map. The biome
/// specifies the acceptable climate range as well as the tiles used for
/// decoration and resource placement. Designers can author multiple biome
/// assets and feed them to the <see cref="MapGenerator"/>.
/// </summary>
[CreateAssetMenu(fileName = "Biome", menuName = "World/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
    [Serializable]
    public class FloraOption
    {
        [Tooltip("Tile placed on the flora layer when this option is selected.")]
        public TileBase tile;

        [Tooltip("Chance of spawning the flora (0-1).")]
        [Range(0f, 1f)]
        public float probability = 0.15f;

        [Tooltip("If true, colonists cannot walk through the tile.")]
        public bool blocksMovement = true;
    }

    [Serializable]
    public class ResourceOption
    {
        [Tooltip("Tile placed on the resource layer when this option is selected.")]
        public TileBase tile;

        [Tooltip("Chance of spawning the resource (0-1).")]
        [Range(0f, 1f)]
        public float probability = 0.05f;

        [Tooltip("If true the resource blocks movement until harvested.")]
        public bool blocksMovement = true;

        [Tooltip("When true the resource will be painted on the berry tilemap layer.")]
        public bool useBerryLayer;
    }

    [Header("Identification")]
    public string biomeName = "Biome";

    [Header("Climate Ranges")]
    [Tooltip("Inclusive temperature range (in °C) that this biome can exist in.")]
    public Vector2 temperatureRange = new Vector2(-10f, 35f);

    [Tooltip("Inclusive humidity range (0-1) that this biome can exist in.")]
    public Vector2 humidityRange = new Vector2(0.1f, 0.9f);

    [Tooltip("Inclusive normalized height range (0-1) for this biome.")]
    public Vector2 heightRange = new Vector2(0.2f, 0.8f);

    [Header("Base Appearance")]
    [Tooltip("Tile painted on the ground tilemap for the biome.")]
    public TileBase groundTile;

    [Tooltip("Optional tint applied to the ground tile.")]
    public Color groundTint = Color.white;

    [Header("Flora")]
    public List<FloraOption> flora = new List<FloraOption>();

    [Header("Resources")]
    public List<ResourceOption> resources = new List<ResourceOption>();

    /// <summary>
    /// Determines whether this biome can exist under the supplied climate
    /// conditions.
    /// </summary>
    public bool Matches(float height, float temperature, float humidity)
    {
        return height >= heightRange.x && height <= heightRange.y
            && temperature >= temperatureRange.x && temperature <= temperatureRange.y
            && humidity >= humidityRange.x && humidity <= humidityRange.y;
    }

    /// <summary>
    /// Calculates how well the biome fits the specified climate. The higher the
    /// value, the closer the climate is to the center of the allowed ranges.
    /// </summary>
    public float EvaluateFitness(float height, float temperature, float humidity)
    {
        float heightCenter = (heightRange.x + heightRange.y) * 0.5f;
        float tempCenter = (temperatureRange.x + temperatureRange.y) * 0.5f;
        float humidityCenter = (humidityRange.x + humidityRange.y) * 0.5f;

        float heightScore = 1f - Mathf.Clamp01(Mathf.Abs(height - heightCenter) / Mathf.Max(0.0001f, heightRange.y - heightRange.x));
        float tempScore = 1f - Mathf.Clamp01(Mathf.Abs(temperature - tempCenter) / Mathf.Max(0.0001f, temperatureRange.y - temperatureRange.x));
        float humidityScore = 1f - Mathf.Clamp01(Mathf.Abs(humidity - humidityCenter) / Mathf.Max(0.0001f, humidityRange.y - humidityRange.x));

        return (heightScore + tempScore + humidityScore) / 3f;
    }
}
