using UnityEngine;
using UnityEngine.UI;
using FallowEarth.Balance;

/// <summary>
/// Simple weather system that randomly toggles rain.
/// When raining, a semi-transparent overlay is shown and
/// colonist movement speed is reduced.
/// </summary>
public class WeatherSystem : MonoBehaviour
{
    public static WeatherSystem Instance { get; private set; }

    [Tooltip("Minimum time in seconds before weather can change")]
    public float minClearTime = 30f;
    [Tooltip("Maximum time in seconds before weather can change")]
    public float maxClearTime = 60f;
    public float minRainTime = 15f;
    public float maxRainTime = 30f;

    public float rainMoveSpeedMultiplier = 0.7f;

    private bool raining;
    private float timer;
    private Image overlay;

    private float baseMinClearTime;
    private float baseMaxClearTime;
    private float baseMinRainTime;
    private float baseMaxRainTime;
    private float baseRainMoveSpeedMultiplier;

    public bool IsRaining => raining;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        baseMinClearTime = minClearTime;
        baseMaxClearTime = maxClearTime;
        baseMinRainTime = minRainTime;
        baseMaxRainTime = maxRainTime;
        baseRainMoveSpeedMultiplier = rainMoveSpeedMultiplier;
    }

    void Start()
    {
        SetupOverlay();
        BeginClear();
    }

    void SetupOverlay()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("Canvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            c.AddComponent<CanvasScaler>();
            c.AddComponent<GraphicRaycaster>();
        }

        GameObject o = new GameObject("RainOverlay");
        o.transform.SetParent(canvas.transform, false);
        overlay = o.AddComponent<Image>();
        overlay.color = new Color(0.6f, 0.6f, 1f, 0f);
        // Disable raycasts so the overlay doesn't block UI interactions
        overlay.raycastTarget = false;

        // Ensure the overlay never intercepts input events
        CanvasGroup cg = o.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (raining)
                BeginClear();
            else
                BeginRain();
        }
    }

    void BeginRain()
    {
        raining = true;
        float min = minRainTime;
        float max = maxRainTime;
        float severity = 1f;
        if (GameBalanceManager.Instance != null)
        {
            var range = GameBalanceManager.Instance.RainWeatherDuration;
            min = baseMinRainTime * range.Min;
            max = baseMaxRainTime * range.Max;
            severity = GameBalanceManager.Instance.WeatherSeverityMultiplier;
        }
        timer = Random.Range(min, max);
        rainMoveSpeedMultiplier = Mathf.Clamp(baseRainMoveSpeedMultiplier * Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(severity - 1f)), 0.25f, 1f);
        if (overlay != null)
        {
            Color c = overlay.color;
            c.a = Mathf.Clamp01(0.2f * severity);
            overlay.color = c;
        }
    }

    void BeginClear()
    {
        raining = false;
        float min = minClearTime;
        float max = maxClearTime;
        if (GameBalanceManager.Instance != null)
        {
            var range = GameBalanceManager.Instance.ClearWeatherDuration;
            min = baseMinClearTime * range.Min;
            max = baseMaxClearTime * range.Max;
        }
        timer = Random.Range(min, max);
        rainMoveSpeedMultiplier = baseRainMoveSpeedMultiplier;
        if (overlay != null)
        {
            Color c = overlay.color;
            c.a = 0f;
            overlay.color = c;
        }
    }

    public float GetMoveSpeedMultiplier()
    {
        return raining ? rainMoveSpeedMultiplier : 1f;
    }
}
