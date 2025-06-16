using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ColonistMenuController : MonoBehaviour
{
    private Canvas canvas;
    private GameObject menuPanel;
    private bool menuOpen;
    private Coroutine animRoutine;
    private Image toggleButtonImage;
    public Color activeColor = new Color(0.6f, 0.9f, 1f, 1f);
    public Color normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);

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
        JobType[] jobs = (JobType[])Enum.GetValues(typeof(JobType));

        GameObject header = new GameObject("Header");
        header.transform.SetParent(menuPanel.transform, false);
        HorizontalLayoutGroup hLayout = header.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 5f;

        CreateHeaderCell(header, "Colonist");
        foreach (var j in jobs)
            CreateHeaderCell(header, j.ToString());

        Colonist[] cols = GameObject.FindObjectsOfType<Colonist>();
        foreach (var c in cols)
        {
            GameObject row = new GameObject(c.name + "Row");
            row.transform.SetParent(menuPanel.transform, false);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;

            CreateRowLabel(row, c.name);

            foreach (var j in jobs)
            {
                Toggle t = CreateRowToggle(row, c.IsJobAllowed(j));
                JobType jt = j; Colonist col = c;
                t.onValueChanged.AddListener(val => col.SetJobAllowed(jt, val));
            }
        }
    }

    void CreateHeaderCell(GameObject parent, string text)
    {
        GameObject tObj = new GameObject(text);
        tObj.transform.SetParent(parent.transform, false);
        Text t = tObj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.black;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = text;
        RectTransform tr = t.GetComponent<RectTransform>();
        tr.sizeDelta = new Vector2(0f, 20f);
    }

    void CreateRowLabel(GameObject parent, string text)
    {
        GameObject tObj = new GameObject("Label");
        tObj.transform.SetParent(parent.transform, false);
        Text t = tObj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.black;
        t.alignment = TextAnchor.MiddleLeft;
        t.text = text;
        RectTransform tr = t.GetComponent<RectTransform>();
        tr.sizeDelta = new Vector2(80f, 20f);
    }

    Toggle CreateRowToggle(GameObject parent, bool state)
    {
        GameObject obj = new GameObject("Toggle");
        obj.transform.SetParent(parent.transform, false);
        Toggle tog = obj.AddComponent<Toggle>();
        Image bg = obj.AddComponent<Image>();
        bg.color = state ? new Color(0.5f,1f,0.5f,1f) : Color.white;
        tog.targetGraphic = bg;
        tog.onValueChanged.AddListener(v => bg.color = v ? new Color(0.5f,1f,0.5f,1f) : Color.white);
        tog.isOn = state;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(20f, 20f);
        return tog;
    }

    public void AssignToggleButton(Image img)
    {
        toggleButtonImage = img;
        if (toggleButtonImage != null)
            toggleButtonImage.color = normalColor;
    }

    public void ToggleMenu()
    {
        menuOpen = !menuOpen;
        if (menuOpen)
            RefreshList();
        if (toggleButtonImage != null)
            toggleButtonImage.color = menuOpen ? activeColor : normalColor;
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
