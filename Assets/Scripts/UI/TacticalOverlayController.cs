using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and manages tactical overlays (temperature, lighting, danger) that
/// can be toggled from the management menu.
/// </summary>
public class TacticalOverlayController : MonoBehaviour
{
    private enum OverlayType
    {
        Temperature,
        Lighting,
        Danger
    }

    private ManagementTabController tabs;
    private readonly Dictionary<OverlayType, GameObject> overlayRoots = new Dictionary<OverlayType, GameObject>();
    private readonly Dictionary<OverlayType, Toggle> overlayToggles = new Dictionary<OverlayType, Toggle>();
    private OverlayType? activeOverlay;

    private MapGenerator map;
    private static Sprite overlaySprite;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        tabs = ManagementTabController.FindOrCreate();
        tabs.StartCoroutine(DelayedSetup());
    }

    System.Collections.IEnumerator DelayedSetup()
    {
        // Wait a frame so the management tabs are fully initialized.
        yield return null;
        BuildUI();
    }

    void BuildUI()
    {
        GameObject section = tabs.CreateSection(ManagementTabController.WorkTabId, "\u0422\u0430\u043a\u0442\u0438\u0447\u0435\u0441\u043a\u0438\u0435 \u043e\u0432\u0435\u0440\u043b\u0435\u0438");
        tabs.CreateLabel(section.transform, "\u0412\u044b\u0431\u0435\u0440\u0438\u0442\u0435 \u0441\u043b\u043e\u0439 \u0434\u043b\u044f \u0430\u043d\u0430\u043b\u0438\u0437\u0430 \u043a\u0430\u0440\u0442\u044b", TextAnchor.MiddleLeft, 16);

        overlayToggles[OverlayType.Temperature] = tabs.CreateToggle(section, "\u0422\u0435\u043c\u043f\u0435\u0440\u0430\u0442\u0443\u0440\u0430", v => HandleToggle(OverlayType.Temperature, v));
        overlayToggles[OverlayType.Lighting] = tabs.CreateToggle(section, "\u041e\u0441\u0432\u0435\u0449\u0451\u043d\u043d\u043e\u0441\u0442\u044c", v => HandleToggle(OverlayType.Lighting, v));
        overlayToggles[OverlayType.Danger] = tabs.CreateToggle(section, "\u041e\u043f\u0430\u0441\u043d\u043e\u0441\u0442\u044c", v => HandleToggle(OverlayType.Danger, v));
    }

    void HandleToggle(OverlayType type, bool state)
    {
        if (state)
        {
            foreach (var kvp in overlayToggles)
            {
                if (kvp.Key != type)
                    kvp.Value.isOn = false;
            }
            ShowOverlay(type);
            EventLogUI.AddEntry($"\u0412\u043a\u043b\u044e\u0447\u0451\u043d \u043e\u0432\u0435\u0440\u043b\u0435\u0439 {ResolveOverlayName(type)}.");
        }
        else if (activeOverlay == type)
        {
            HideOverlay();
            EventLogUI.AddEntry($"\u0412\u044b\u043a\u043b\u044e\u0447\u0451\u043d \u043e\u0432\u0435\u0440\u043b\u0435\u0439 {ResolveOverlayName(type)}.");
        }
    }

    string ResolveOverlayName(OverlayType type)
    {
        switch (type)
        {
            case OverlayType.Temperature: return "\u0442\u0435\u043c\u043f\u0435\u0440\u0430\u0442\u0443\u0440\u044b";
            case OverlayType.Lighting: return "\u043e\u0441\u0432\u0435\u0449\u0451\u043d\u043d\u043e\u0441\u0442\u0438";
            default: return "\u043e\u043f\u0430\u0441\u043d\u043e\u0441\u0442\u0438";
        }
    }

    void ShowOverlay(OverlayType type)
    {
        if (!overlayRoots.TryGetValue(type, out GameObject root) || root == null)
        {
            root = BuildOverlay(type);
            overlayRoots[type] = root;
        }

        if (root != null)
        {
            root.SetActive(true);
            activeOverlay = type;
        }
    }

    void HideOverlay()
    {
        if (!activeOverlay.HasValue)
            return;

        OverlayType type = activeOverlay.Value;
        if (overlayRoots.TryGetValue(type, out GameObject root) && root != null)
            root.SetActive(false);
        activeOverlay = null;
    }

    GameObject BuildOverlay(OverlayType type)
    {
        if (map == null)
            map = FindObjectOfType<MapGenerator>();
        if (map == null || map.TemperatureMap == null)
            return null;

        if (overlaySprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            overlaySprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }

        GameObject root = new GameObject(type + "OverlayRoot");
        root.transform.SetParent(transform, false);

        int width = Mathf.Max(map.width, map.HeightMap?.GetLength(0) ?? map.width);
        int height = Mathf.Max(map.height, map.HeightMap?.GetLength(1) ?? map.height);
        int step = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(width, height) / 32f));

        for (int y = 0; y < map.height; y += step)
        {
            for (int x = 0; x < map.width; x += step)
            {
                float value = SampleValue(type, x, y);
                Color tint = EvaluateColor(type, value);
                CreateOverlayTile(root.transform, x, y, step, tint);
            }
        }

        root.SetActive(false);
        return root;
    }

    float SampleValue(OverlayType type, int x, int y)
    {
        switch (type)
        {
            case OverlayType.Temperature:
                float temp = map.TemperatureMap[x, y];
                float normalized = Mathf.InverseLerp(map.MinTemperature, map.MaxTemperature, temp);
                return Mathf.Clamp01(normalized);
            case OverlayType.Lighting:
                float baseLight = 1f;
                if (DayNightCycle.Instance != null)
                {
                    float hour = DayNightCycle.Instance.CurrentHour;
                    float t = Mathf.Cos((hour - 12f) / 24f * Mathf.PI * 2f) * 0.5f + 0.5f;
                    baseLight = Mathf.Clamp01(t);
                }
                float noise = Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.35f;
                return Mathf.Clamp01(baseLight * 0.7f + noise);
            case OverlayType.Danger:
                Vector2Int cell = new Vector2Int(x, y);
                bool nearWater = map.WaterCells != null && map.IsWaterCell(cell);
                float height = map.HeightMap != null ? map.HeightMap[x, y] : 0f;
                float steep = Mathf.InverseLerp(map.MountainThreshold - 0.05f, 1f, height);
                return Mathf.Clamp01(nearWater ? 1f : steep);
            default:
                return 0f;
        }
    }

    Color EvaluateColor(OverlayType type, float value)
    {
        switch (type)
        {
            case OverlayType.Temperature:
                return Color.Lerp(new Color(0.2f, 0.4f, 1f, 0.35f), new Color(1f, 0.3f, 0.1f, 0.45f), value);
            case OverlayType.Lighting:
                return Color.Lerp(new Color(0.1f, 0.1f, 0.2f, 0.45f), new Color(1f, 0.9f, 0.4f, 0.35f), value);
            case OverlayType.Danger:
                return Color.Lerp(new Color(0.2f, 0.6f, 0.2f, 0.35f), new Color(0.8f, 0.1f, 0.1f, 0.5f), value);
            default:
                return new Color(1f, 1f, 1f, 0.3f);
        }
    }

    void CreateOverlayTile(Transform parent, int x, int y, int step, Color color)
    {
        GameObject tile = new GameObject($"OverlayTile_{x}_{y}");
        tile.transform.SetParent(parent, false);
        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        sr.sprite = overlaySprite;
        sr.color = color;
        sr.sortingOrder = 50;
        tile.transform.position = new Vector3(x + step * 0.5f, y + step * 0.5f, 0f);
        tile.transform.localScale = new Vector3(step, step, 1f);
    }
}
