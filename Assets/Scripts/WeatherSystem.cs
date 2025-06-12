using UnityEngine;
using UnityEngine.UI;

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
        timer = Random.Range(minRainTime, maxRainTime);
        if (overlay != null)
        {
            Color c = overlay.color;
            c.a = 0.25f;
            overlay.color = c;
        }
    }

    void BeginClear()
    {
        raining = false;
        timer = Random.Range(minClearTime, maxClearTime);
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
