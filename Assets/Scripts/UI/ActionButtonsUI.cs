using UnityEngine;
using UnityEngine.UI;

public class ActionButtonsUI : MonoBehaviour
{
    void Start()
    {
        SetupUI();
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

        GameObject panel = new GameObject("ActionButtons");
        panel.transform.SetParent(canvas.transform, false);
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5f;
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(20f, 20f);
        rt.sizeDelta = new Vector2(160f, 100f);

        CreateChopButton(panel);
        CreateBuildButton(panel);
        CreateZoneButton(panel);
    }

    void CreateChopButton(GameObject parent)
    {
        TreeChopController ctrl = FindObjectOfType<TreeChopController>();
        Image img; RectTransform rect;
        Button btn = CreateButton(parent, "\u0421\u0440\u0443\u0431\u0438\u0442\u044c", out img, out rect);
        btn.onClick.AddListener(() => { if (ctrl != null) ctrl.ToggleSelecting(); });
        if (ctrl != null) ctrl.AssignButton(img, rect);
    }

    void CreateBuildButton(GameObject parent)
    {
        BuildMenuController ctrl = FindObjectOfType<BuildMenuController>();
        Image img; RectTransform rect;
        Button btn = CreateButton(parent, "\u0421\u0442\u0440\u043e\u0438\u0442\u0435\u043b\u044c\u0441\u0442\u0432\u043e", out img, out rect);
        btn.onClick.AddListener(() => { if (ctrl != null) ctrl.ToggleMenu(); });
    }

    void CreateZoneButton(GameObject parent)
    {
        ZoneMenuController ctrl = FindObjectOfType<ZoneMenuController>();
        Image img; RectTransform rect;
        Button btn = CreateButton(parent, "\u0417\u043e\u043d\u044b", out img, out rect);
        btn.onClick.AddListener(() => { if (ctrl != null) ctrl.ToggleMenu(); });
    }

    Button CreateButton(GameObject parent, string label, out Image img, out RectTransform rect)
    {
        GameObject bObj = new GameObject(label + "Button");
        bObj.transform.SetParent(parent.transform, false);
        img = bObj.AddComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        Button btn = bObj.AddComponent<Button>();
        btn.targetGraphic = img;

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

        rect = bObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160f, 30f);
        return btn;
    }
}
