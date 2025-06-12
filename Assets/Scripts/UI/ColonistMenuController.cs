using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ColonistMenuController : MonoBehaviour
{
    private Canvas canvas;
    private GameObject menuPanel;
    private bool menuOpen;
    private Coroutine animRoutine;

    void Start()
    {
        SetupCanvas();
        CreateMenu();
    }

    void SetupCanvas()
    {
        canvas = FindObjectOfType<Canvas>();
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
    }

    void CreateMenu()
    {
        menuPanel = new GameObject("ColonistMenu");
        menuPanel.transform.SetParent(canvas.transform, false);
        Image img = menuPanel.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.95f);
        RectTransform rt = menuPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(300f, 200f);
        menuPanel.transform.localScale = Vector3.zero;
        menuPanel.SetActive(false);

        VerticalLayoutGroup layout = menuPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(5, 5, 5, 5);

        RefreshList();
    }

    void RefreshList()
    {
        foreach (Transform t in menuPanel.transform)
            GameObject.Destroy(t.gameObject);

        Colonist[] cols = GameObject.FindObjectsOfType<Colonist>();
        foreach (var c in cols)
        {
            GameObject tObj = new GameObject(c.name);
            tObj.transform.SetParent(menuPanel.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = Color.black;
            t.alignment = TextAnchor.MiddleLeft;
            int mood = Mathf.RoundToInt(c.mood * 100f);
            int health = Mathf.RoundToInt(c.health * 100f);
            t.text = $"{c.name} - \u0437\u0434\u043e\u0440\u043e\u0432\u044c\u0435 {health}% \u043d\u0430\u0441\u0442\u0440\u043e\u0435\u043d\u0438\u0435 {mood}%";
            RectTransform tr = t.GetComponent<RectTransform>();
            tr.sizeDelta = new Vector2(0f, 20f);
        }
    }

    public void ToggleMenu()
    {
        menuOpen = !menuOpen;
        if (menuOpen)
            RefreshList();
        if (animRoutine != null)
            StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(MenuAnimation(menuOpen));
    }

    IEnumerator MenuAnimation(bool open)
    {
        if (menuPanel == null)
            yield break;
        if (open)
            menuPanel.SetActive(true);

        Vector3 start = menuPanel.transform.localScale;
        Vector3 target = open ? Vector3.one : Vector3.zero;
        float time = 0f;
        while (time < 0.2f)
        {
            time += Time.unscaledDeltaTime;
            menuPanel.transform.localScale = Vector3.Lerp(start, target, time / 0.2f);
            yield return null;
        }
        menuPanel.transform.localScale = target;
        if (!open)
            menuPanel.SetActive(false);
    }
}
