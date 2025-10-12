using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Centralized controller that builds the base management window with tabbed
/// sections for work, research and health. Other systems (orders, zones,
/// overlays, etc.) plug their widgets into this controller at runtime.
/// </summary>
public class ManagementTabController : MonoBehaviour
{
    public const string WorkTabId = "work";
    public const string ResearchTabId = "research";
    public const string HealthTabId = "health";

    public Color tabActiveColor = new Color(0.6f, 0.9f, 1f, 1f);
    public Color tabNormalColor = new Color(0.88f, 0.88f, 0.88f, 1f);
    public Color panelBackground = new Color(1f, 1f, 1f, 0.95f);
    public Color sectionBackground = new Color(1f, 1f, 1f, 0.08f);

    private Canvas canvas;
    private GameObject menuPanel;
    private RectTransform contentRoot;
    private RectTransform tabHeaderRoot;
    private readonly Dictionary<string, TabInfo> tabs = new Dictionary<string, TabInfo>();
    private TabInfo currentTab;

    private bool menuOpen;
    private Coroutine animRoutine;
    private Image activeToggleImage;
    private object activeRequester;

    private class TabInfo
    {
        public string id;
        public Button headerButton;
        public GameObject content;
        public string label;
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        SetupCanvas();
        CreateMenuPanel();
        EnsureDefaultTabs();
        HideImmediate();
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

    void CreateMenuPanel()
    {
        menuPanel = new GameObject("ManagementTabs");
        menuPanel.transform.SetParent(canvas.transform, false);
        Image img = menuPanel.AddComponent<Image>();
        img.color = panelBackground;
        RectTransform rt = menuPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(420f, 520f);

        VerticalLayoutGroup layout = menuPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);

        GameObject tabHeader = new GameObject("TabHeader");
        tabHeader.transform.SetParent(menuPanel.transform, false);
        tabHeaderRoot = tabHeader.AddComponent<RectTransform>();
        HorizontalLayoutGroup tabLayout = tabHeader.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 6f;
        tabLayout.childForceExpandHeight = false;
        tabLayout.childForceExpandWidth = true;

        GameObject content = new GameObject("TabContent");
        content.transform.SetParent(menuPanel.transform, false);
        contentRoot = content.AddComponent<RectTransform>();
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        LayoutElement element = content.AddComponent<LayoutElement>();
        element.flexibleHeight = 1f;
        element.preferredHeight = 0f;
    }

    void EnsureDefaultTabs()
    {
        EnsureTab(WorkTabId, "\u0420\u0430\u0431\u043e\u0442\u0430");
        EnsureTab(ResearchTabId, "\u0418\u0441\u0441\u043b\u0435\u0434\u043e\u0432\u0430\u043d\u0438\u044f");
        EnsureTab(HealthTabId, "\u0417\u0434\u043e\u0440\u043e\u0432\u044c\u0435");
        ShowTab(WorkTabId);
    }

    void HideImmediate()
    {
        menuPanel.transform.localScale = Vector3.zero;
        menuPanel.SetActive(false);
        menuOpen = false;
    }

    public static ManagementTabController FindOrCreate()
    {
        ManagementTabController ctrl = FindObjectOfType<ManagementTabController>();
        if (ctrl == null)
            ctrl = new GameObject(nameof(ManagementTabController)).AddComponent<ManagementTabController>();
        return ctrl;
    }

    public GameObject EnsureTab(string id, string label)
    {
        if (tabs.TryGetValue(id, out TabInfo info))
            return info.content;

        GameObject buttonObj = new GameObject(label + "TabButton");
        buttonObj.transform.SetParent(tabHeaderRoot, false);
        Image img = buttonObj.AddComponent<Image>();
        img.color = tabNormalColor;
        Button btn = buttonObj.AddComponent<Button>();
        btn.targetGraphic = img;
        buttonObj.AddComponent<ButtonPressEffect>();

        Text txt = CreateLabel(buttonObj.transform, label, TextAnchor.MiddleCenter, 20, FontStyle.Normal);
        RectTransform brt = buttonObj.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(0f, 40f);
        LayoutElement be = buttonObj.AddComponent<LayoutElement>();
        be.flexibleWidth = 1f;
        be.minHeight = 40f;

        GameObject contentObj = new GameObject(label + "Content");
        contentObj.transform.SetParent(contentRoot, false);
        RectTransform crt = contentObj.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 0f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.childForceExpandWidth = true;
        layout.childControlWidth = true;

        TabInfo tabInfo = new TabInfo
        {
            id = id,
            headerButton = btn,
            content = contentObj,
            label = label
        };
        tabs[id] = tabInfo;

        contentObj.SetActive(false);
        btn.onClick.AddListener(() => ShowTab(id));
        return contentObj;
    }

    public void ShowTab(string id)
    {
        if (!tabs.TryGetValue(id, out TabInfo info))
            return;

        if (currentTab != null)
        {
            currentTab.content.SetActive(false);
            if (currentTab.headerButton != null)
                SetButtonState(currentTab.headerButton, false);
        }

        currentTab = info;
        currentTab.content.SetActive(true);
        if (currentTab.headerButton != null)
            SetButtonState(currentTab.headerButton, true);
    }

    void SetButtonState(Button btn, bool active)
    {
        Image img = btn.targetGraphic as Image;
        if (img != null)
            img.color = active ? tabActiveColor : tabNormalColor;
    }

    IEnumerator MenuAnimation(bool open)
    {
        if (menuPanel == null)
            yield break;

        if (open)
            menuPanel.SetActive(true);

        Vector3 start = menuPanel.transform.localScale;
        Vector3 target = open ? Vector3.one : Vector3.zero;
        float elapsed = 0f;
        const float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            menuPanel.transform.localScale = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        menuPanel.transform.localScale = target;
        if (!open)
            menuPanel.SetActive(false);
    }

    public bool ToggleMenu(object requester, string targetTabId, Image toggleImage)
    {
        if (requester == null)
            requester = this;

        bool shouldClose = menuOpen && activeRequester == requester;
        if (shouldClose)
        {
            CloseMenu();
            UpdateToggleImage(toggleImage, false);
            activeRequester = null;
            activeToggleImage = null;
            return false;
        }

        activeRequester = requester;
        ShowTab(targetTabId);
        OpenMenu();
        if (activeToggleImage != null && activeToggleImage != toggleImage)
            UpdateToggleImage(activeToggleImage, false);
        activeToggleImage = toggleImage;
        UpdateToggleImage(toggleImage, true);
        return true;
    }

    void UpdateToggleImage(Image img, bool active)
    {
        if (img == null)
            return;
        img.color = active ? tabActiveColor : tabNormalColor;
    }

    void OpenMenu()
    {
        menuOpen = true;
        if (animRoutine != null)
            StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(MenuAnimation(true));
    }

    void CloseMenu()
    {
        menuOpen = false;
        if (animRoutine != null)
            StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(MenuAnimation(false));
    }

    public void NotifyToggleRegistered(Image image)
    {
        UpdateToggleImage(image, false);
    }

    public GameObject CreateSection(string tabId, string headerTitle)
    {
        GameObject tab = EnsureTab(tabId, ResolveLabel(tabId));
        GameObject section = new GameObject(headerTitle + "Section");
        section.transform.SetParent(tab.transform, false);
        Image bg = section.AddComponent<Image>();
        bg.color = sectionBackground;
        RectTransform rt = section.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childForceExpandWidth = true;
        layout.childControlWidth = true;

        CreateLabel(section.transform, headerTitle, TextAnchor.MiddleLeft, 22, FontStyle.Bold);
        return section;
    }

    string ResolveLabel(string id)
    {
        if (tabs.TryGetValue(id, out TabInfo info))
            return info.label;
        return id;
    }

    public Text CreateLabel(Transform parent, string text, TextAnchor alignment = TextAnchor.MiddleLeft, int fontSize = 18, FontStyle style = FontStyle.Normal)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        Text t = obj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text;
        t.alignment = alignment;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = Color.black;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 24f);
        return t;
    }

    public Button CreateActionButton(GameObject parent, string label, UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(label + "ActionButton");
        buttonObj.transform.SetParent(parent.transform, false);
        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        Button btn = buttonObj.AddComponent<Button>();
        btn.targetGraphic = img;
        buttonObj.AddComponent<ButtonPressEffect>();
        btn.onClick.AddListener(onClick);

        Text txt = CreateLabel(buttonObj.transform, label, TextAnchor.MiddleCenter, 20);
        RectTransform tr = buttonObj.GetComponent<RectTransform>();
        tr.sizeDelta = new Vector2(0f, 36f);
        LayoutElement le = buttonObj.AddComponent<LayoutElement>();
        le.minHeight = 36f;
        le.preferredHeight = 40f;
        return btn;
    }

    public Toggle CreateToggle(GameObject parent, string label, UnityAction<bool> onValueChanged)
    {
        GameObject toggleObj = new GameObject(label + "Toggle");
        toggleObj.transform.SetParent(parent.transform, false);
        HorizontalLayoutGroup layout = toggleObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        GameObject boxObj = new GameObject("Box");
        boxObj.transform.SetParent(toggleObj.transform, false);
        Image bg = boxObj.AddComponent<Image>();
        bg.color = Color.white;
        Toggle toggle = boxObj.AddComponent<Toggle>();
        toggle.targetGraphic = bg;
        toggle.onValueChanged.AddListener(v => bg.color = v ? new Color(0.5f, 0.8f, 1f, 1f) : Color.white);
        toggle.onValueChanged.AddListener(onValueChanged);
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.sizeDelta = new Vector2(24f, 24f);

        CreateLabel(toggleObj.transform, label, TextAnchor.MiddleLeft, 18);
        return toggle;
    }

    public Slider CreateProgressBar(GameObject parent)
    {
        GameObject sliderObj = new GameObject("ProgressBar");
        sliderObj.transform.SetParent(parent.transform, false);
        Image bg = sliderObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.1f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.transition = Selectable.Transition.None;

        RectTransform rt = sliderObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 26f);
        LayoutElement le = sliderObj.AddComponent<LayoutElement>();
        le.minHeight = 26f;

        GameObject fillArea = new GameObject("Fill");
        fillArea.transform.SetParent(sliderObj.transform, false);
        Image fillImage = fillArea.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.6f, 0.9f, 1f);
        RectTransform frt = fillArea.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(3f, 3f);
        frt.offsetMax = new Vector2(-3f, -3f);

        slider.targetGraphic = fillImage;
        slider.fillRect = frt;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }
}
