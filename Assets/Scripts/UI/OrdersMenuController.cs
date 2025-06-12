using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OrdersMenuController : MonoBehaviour
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
        menuPanel = new GameObject("OrdersMenu");
        menuPanel.transform.SetParent(canvas.transform, false);
        Image img = menuPanel.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.95f);
        RectTransform rt = menuPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(300f, 100f);
        menuPanel.transform.localScale = Vector3.zero;
        menuPanel.SetActive(false);

        VerticalLayoutGroup layout = menuPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5f;
        layout.padding = new RectOffset(5, 5, 5, 5);

        CreateChopButton();
    }

    void CreateChopButton()
    {
        TreeChopController ctrl = FindObjectOfType<TreeChopController>();
        GameObject obj = new GameObject("ChopTreesButton");
        obj.transform.SetParent(menuPanel.transform, false);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        obj.AddComponent<ButtonPressEffect>();
        btn.onClick.AddListener(() =>
        {
            if (ctrl != null)
            {
                ctrl.ToggleSelecting();
                if (ctrl.IsSelecting)
                    CancelActionUI.Show(toggleButtonRect, ctrl.ToggleSelecting);
                else
                    CancelActionUI.Hide();
            }
            ToggleMenu();
        });

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.text = "\u0421\u0440\u0443\u0431\u0438\u0442\u044C"; // "Срубить"
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = Color.black;
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        RectTransform brt = obj.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(160f, 30f);

        if (ctrl != null)
            ctrl.AssignButton(img, brt);
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
