using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple time speed controls similar to RimWorld.
/// Provides Pause, 1x and 3x speed buttons.
/// </summary>
public class TimeControlUI : MonoBehaviour
{
    private Image pauseImg;
    private Image normalImg;
    private Image fastImg;

    void Start()
    {
        SetupUI();
        UpdateButtonHighlight();
    }

    void SetupUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("Canvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = c.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            c.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = new GameObject("TimeControls");
        panel.transform.SetParent(canvas.transform, false);
        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5f;
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-20f, 20f);
        rt.sizeDelta = new Vector2(180f, 30f);

        pauseImg = CreateButton(panel, "||", 0f);
        normalImg = CreateButton(panel, "1x", 1f);
        fastImg = CreateButton(panel, "3x", 3f);
    }

    void UpdateButtonHighlight()
    {
        Color active = new Color(0.6f, 1f, 0.6f, 1f);
        Color inactive = new Color(0.9f, 0.9f, 0.9f, 1f);

        float s = Time.timeScale;
        if (pauseImg != null)
            pauseImg.color = Mathf.Approximately(s, 0f) ? active : inactive;
        if (normalImg != null)
            normalImg.color = Mathf.Approximately(s, 1f) ? active : inactive;
        if (fastImg != null)
            fastImg.color = Mathf.Approximately(s, 3f) ? active : inactive;
    }

    Image CreateButton(GameObject parent, string label, float speed)
    {
        GameObject bObj = new GameObject(label + "Button");
        bObj.transform.SetParent(parent.transform, false);
        Image img = bObj.AddComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        Button btn = bObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            Time.timeScale = speed;
            UpdateButtonHighlight();
        });

        GameObject tObj = new GameObject("Text");
        tObj.transform.SetParent(bObj.transform, false);
        Text t = tObj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = label;
        t.color = Color.black;
        t.alignment = TextAnchor.MiddleCenter;
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;

        RectTransform rt = bObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(40f, 30f);

        return img;
    }
}
