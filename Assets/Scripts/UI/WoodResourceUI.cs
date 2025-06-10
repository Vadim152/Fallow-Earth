using UnityEngine;
using UnityEngine.UI;

public class WoodResourceUI : MonoBehaviour
{
    private Text text;

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

        GameObject textObj = new GameObject("WoodText");
        textObj.transform.SetParent(canvas.transform, false);
        text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(200f, 30f);
        text.alignment = TextAnchor.MiddleRight;
        text.color = Color.black;
    }

    void Update()
    {
        if (text != null && ResourceManager.Instance != null)
            text.text = $"Wood: {ResourceManager.Instance.Wood}";
    }
}
