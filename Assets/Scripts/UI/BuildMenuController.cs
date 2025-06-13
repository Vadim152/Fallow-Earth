using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildMenuController : MonoBehaviour
{
    private Canvas canvas;
    private GameObject menuPanel;
    private bool menuOpen;
    private Coroutine animRoutine;
    private Image toggleButtonImage;
    private RectTransform toggleButtonRect;
    public Color activeColor = new Color(0.6f, 0.9f, 1f, 1f);
    public Color normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    void Start()
    {
        SetupCanvas();
        CreateBuildMenu();
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

    void CreateBuildButton()
    {
        GameObject buttonObj = new GameObject("BuildButton");
        buttonObj.transform.SetParent(canvas.transform, false);
        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        Button btn = buttonObj.AddComponent<Button>();
        btn.targetGraphic = img;
        buttonObj.AddComponent<ButtonPressEffect>();
        btn.onClick.AddListener(ToggleMenu);

        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(20f, 60f);
        rt.sizeDelta = new Vector2(160f, 30f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.text = "\u0421\u0442\u0440\u043e\u0438\u0442\u0435\u043b\u044c\u0441\u0442\u0432\u043e"; // "Строительство"
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = Color.black;
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    void CreateBuildMenu()
    {
        menuPanel = new GameObject("BuildMenu");
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

        GameObject wallObj = new GameObject("WoodWallButton");
        wallObj.transform.SetParent(menuPanel.transform, false);
        Image wallImg = wallObj.AddComponent<Image>();
        wallImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        Button wallBtn = wallObj.AddComponent<Button>();
        wallBtn.targetGraphic = wallImg;
        wallObj.AddComponent<ButtonPressEffect>();
        wallBtn.onClick.AddListener(() =>
        {
            BuildWallController ctrl = FindObjectOfType<BuildWallController>();
            if (ctrl != null)
            {
                ctrl.TogglePlacing();
                if (ctrl.IsPlacing)

                    global::CancelActionUI.Show(toggleButtonRect, ctrl.TogglePlacing);
                else
                    global::CancelActionUI.Hide();
            }
            ToggleMenu();
        });

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(wallObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.text = "\u0414\u0435\u0440\u0435\u0432\u044f\u043d\u043d\u0430\u044f \u0441\u0442\u0435\u043d\u0430"; // "Деревянная стена"
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = Color.black;
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        GameObject doorObj = new GameObject("WoodDoorButton");
        doorObj.transform.SetParent(menuPanel.transform, false);
        Image doorImg = doorObj.AddComponent<Image>();
        doorImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        Button doorBtn = doorObj.AddComponent<Button>();
        doorBtn.targetGraphic = doorImg;
        doorObj.AddComponent<ButtonPressEffect>();
        doorBtn.onClick.AddListener(() =>
        {
            BuildDoorController ctrl = FindObjectOfType<BuildDoorController>();
            if (ctrl != null)
            {
                ctrl.TogglePlacing();
                if (ctrl.IsPlacing)
                    global::CancelActionUI.Show(toggleButtonRect, ctrl.TogglePlacing);
                else
                    global::CancelActionUI.Hide();
            }
            ToggleMenu();
        });

        GameObject dTxtObj = new GameObject("Text");
        dTxtObj.transform.SetParent(doorObj.transform, false);
        Text dTxt = dTxtObj.AddComponent<Text>();
        dTxt.text = "\u0414\u0435\u0440\u0435\u0432\u044f\u043d\u043d\u0430\u044f \u0434\u0432\u0435\u0440\u044c"; // "Деревянная дверь"
        dTxt.alignment = TextAnchor.MiddleCenter;
        dTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dTxt.color = Color.black;
        RectTransform dRt = dTxtObj.GetComponent<RectTransform>();
        dRt.anchorMin = Vector2.zero;
        dRt.anchorMax = Vector2.one;
        dRt.offsetMin = Vector2.zero;
        dRt.offsetMax = Vector2.zero;

        GameObject bedObj = new GameObject("BedButton");
        bedObj.transform.SetParent(menuPanel.transform, false);
        Image bedImg = bedObj.AddComponent<Image>();
        bedImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        Button bedBtn = bedObj.AddComponent<Button>();
        bedBtn.targetGraphic = bedImg;
        bedObj.AddComponent<ButtonPressEffect>();
        bedBtn.onClick.AddListener(() =>
        {
            BuildBedController ctrl = FindObjectOfType<BuildBedController>();
            if (ctrl != null)
            {
                ctrl.TogglePlacing();
                if (ctrl.IsPlacing)
                    global::CancelActionUI.Show(toggleButtonRect, ctrl.TogglePlacing);
                else
                    global::CancelActionUI.Hide();
            }
            ToggleMenu();
        });

        GameObject bTxtObj = new GameObject("Text");
        bTxtObj.transform.SetParent(bedObj.transform, false);
        Text bTxt = bTxtObj.AddComponent<Text>();
        bTxt.text = "\u041a\u0440\u043e\u0432\u0430\u0442\u044c"; // "Кровать"
        bTxt.alignment = TextAnchor.MiddleCenter;
        bTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bTxt.color = Color.black;
        RectTransform bRt = bTxtObj.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;
    }

    public void AssignToggleButton(Image img, RectTransform rect)
    {
        toggleButtonImage = img;
        toggleButtonRect = rect;
        if (toggleButtonImage != null)
            toggleButtonImage.color = normalColor;
    }

    public RectTransform ToggleButtonRect => toggleButtonRect;

    public void ToggleMenu()
    {
        menuOpen = !menuOpen;
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
