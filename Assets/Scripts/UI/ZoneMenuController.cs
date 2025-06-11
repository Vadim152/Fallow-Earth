using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Required for stockpile zone placement
using System.Collections.Generic;

public class ZoneMenuController : MonoBehaviour
{
    private Canvas canvas;
    private GameObject menuPanel;
    private bool menuOpen;
    private Coroutine animRoutine;

    void Start()
    {
        SetupCanvas();
        CreateZoneButton();
        CreateZoneMenu();
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

        // EventSystem is now created by EventSystemBootstrap
    }

    void CreateZoneButton()
    {
        GameObject buttonObj = new GameObject("ZoneButton");
        buttonObj.transform.SetParent(canvas.transform, false);
        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        Button btn = buttonObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(ToggleMenu);

        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        // Place the zones button above the existing build and chop buttons
        // to avoid overlapping with the "Chop" button at (20,20) and the
        // build button at (20,60).
        rt.anchoredPosition = new Vector2(20f, 100f);
        rt.sizeDelta = new Vector2(160f, 30f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.text = "\u0417\u043e\u043d\u044b"; // "Зоны"
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = Color.black;
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    void CreateZoneMenu()
    {
        menuPanel = new GameObject("ZoneMenu");
        menuPanel.transform.SetParent(canvas.transform, false);
        Image img = menuPanel.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.95f);
        RectTransform rt = menuPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(300f, 400f);
        menuPanel.transform.localScale = Vector3.zero;
        menuPanel.SetActive(false);

        GridLayoutGroup grid = menuPanel.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(120f, 50f);
        grid.spacing = new Vector2(5f, 5f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.padding = new RectOffset(5, 5, 5, 5);

        string[] zones = { "\u0421\u043a\u043b\u0430\u0434", "Zone2", "Zone3", "Zone4", "Zone5", "Zone6", "Zone7", "Zone8" };
        for (int i = 0; i < zones.Length; i++)
        {
            GameObject bObj = new GameObject($"Zone{i + 1}");
            bObj.transform.SetParent(menuPanel.transform, false);
            Image bImg = bObj.AddComponent<Image>();
            bImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            Button bBtn = bObj.AddComponent<Button>();
            bBtn.targetGraphic = bImg;
            if (i == 0)
            {
                bBtn.onClick.AddListener(() => {
                    StockpileZoneController ctrl = FindObjectOfType<StockpileZoneController>();
                    if (ctrl != null)
                        ctrl.TogglePlacing();
                    ToggleMenu();
                });
            }

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(bObj.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.text = zones[i];
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = Color.black;
            RectTransform tRt = tObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
        }
    }

    void ToggleMenu()
    {
        menuOpen = !menuOpen;
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
