using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the current time of day (morning, day, evening, night)
/// in the top-right corner of the screen.
/// </summary>
public class TimeOfDayUI : MonoBehaviour
{
    private Text text;
    private DayNightCycle cycle;

    void Start()
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

        GameObject textObj = new GameObject("TimeOfDayText");
        textObj.transform.SetParent(canvas.transform, false);
        text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleRight;
        text.color = Color.black;

        RectTransform rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -60f);
        rt.sizeDelta = new Vector2(200f, 30f);

        cycle = FindObjectOfType<DayNightCycle>();
    }

    void Update()
    {
        if (text == null)
            return;

        float minutesPerDay = 2f;
        if (cycle != null)
            minutesPerDay = cycle.minutesPerDay;

        float t = (Time.time / (minutesPerDay * 60f)) % 1f;
        int hour = Mathf.FloorToInt(t * 24f);

        text.text = hour.ToString("00") + ":00";
    }
}
