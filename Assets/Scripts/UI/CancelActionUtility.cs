using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a small cancel button next to a menu button when an action
/// like building placement or tree chopping is active. Pressing the
/// button calls the provided callback and hides the cancel button.
/// </summary>
public class CancelActionUtility : MonoBehaviour
{
    static CancelActionUtility instance;

    RectTransform rect;
    Button btn;

    void Awake()
    {
        instance = this;
        rect = gameObject.AddComponent<RectTransform>();
        CreateUI();
        gameObject.SetActive(false);
    }

    void CreateUI()
    {
        Image img = gameObject.AddComponent<Image>();
        img.color = new Color(0.9f, 0.6f, 0.6f, 1f);
        btn = gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        gameObject.AddComponent<ButtonPressEffect>();
        rect.sizeDelta = new Vector2(25f, 25f);
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);

        GameObject tObj = new GameObject("Text");
        tObj.transform.SetParent(transform, false);
        Text t = tObj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = TextAnchor.MiddleCenter;
        t.text = "X";
        t.color = Color.black;
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;
    }

    void ShowInternal(RectTransform anchor, System.Action callback)
    {
        rect.SetParent(anchor, false);
        rect.anchoredPosition = new Vector2(anchor.rect.width + 5f, 0f);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => { callback?.Invoke(); HideInternal(); });
        gameObject.SetActive(true);
    }

    void HideInternal()
    {
        gameObject.SetActive(false);
    }

    public static void Show(RectTransform anchor, System.Action callback)
    {
        if (anchor == null)
            return;
        if (instance == null)
            instance = new GameObject("CancelActionUtility").AddComponent<CancelActionUtility>();
        instance.ShowInternal(anchor, callback);
    }

    public static void Hide()
    {
        if (instance != null)
            instance.HideInternal();
    }
}
