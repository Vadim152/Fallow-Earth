using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple day/night cycle that darkens the screen at night.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Tooltip("Minutes of real time for a full day cycle")]
    public float minutesPerDay = 2f;

    private Image overlay;
    public static DayNightCycle Instance { get; private set; }
    public float CurrentHour { get; private set; }
    public int CurrentHourInt => Mathf.FloorToInt(CurrentHour);

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(Instance);
        Instance = this;
    }

    void Start()
    {
        SetupOverlay();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

        GameObject o = new GameObject("NightOverlay");
        o.transform.SetParent(canvas.transform, false);
        overlay = o.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0f);
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
        // Fraction of the current day (0..1) where 0 is midnight
        float t = (Time.time / (minutesPerDay * 60f)) % 1f;
        CurrentHour = t * 24f;
        // Shift the cosine so that t=0 corresponds to midnight rather than noon
        float phase = Mathf.Cos((t - 0.5f) * Mathf.PI * 2f) * 0.5f + 0.5f; // 1 at noon, 0 at midnight
        float alpha = Mathf.Clamp01(1f - phase);
        if (overlay != null)
        {
            Color c = overlay.color;
            c.a = alpha * 0.5f;
            overlay.color = c;
        }
    }
}
