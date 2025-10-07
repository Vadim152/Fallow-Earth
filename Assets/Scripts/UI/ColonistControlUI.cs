using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColonistControlUI : MonoBehaviour
{
    private readonly List<Colonist> colonists = new List<Colonist>();
    private readonly Dictionary<Colonist, Button> colonistButtons = new Dictionary<Colonist, Button>();

    private RectTransform panel;
    private Colonist selected;
    private ColonistInfoCard infoCard;

    void Awake()
    {
        SetupCanvas();
        CreatePanel();
    }

    void Start()
    {
        colonists.AddRange(FindObjectsOfType<Colonist>());
        RefreshButtons();
        infoCard = FindObjectOfType<ColonistInfoCard>();
    }

    void SetupCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        transform.SetParent(canvas.transform, false);
    }

    void CreatePanel()
    {
        GameObject panelObj = new GameObject("ColonistControlPanel", typeof(RectTransform));
        panelObj.transform.SetParent(transform, false);

        panel = panelObj.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 1f);
        panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -20f);

        Image background = panelObj.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.85f);

        HorizontalLayoutGroup layout = panelObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        ContentSizeFitter fitter = panelObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void RefreshButtons()
    {
        foreach (Transform child in panel)
        {
            Destroy(child.gameObject);
        }

        colonistButtons.Clear();

        foreach (Colonist colonist in colonists)
        {
            CreateColonistButton(colonist);
        }
    }

    void CreateColonistButton(Colonist colonist)
    {
        GameObject buttonObj = new GameObject(colonist.name + "Button", typeof(RectTransform));
        buttonObj.transform.SetParent(panel, false);

        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        Button btn = buttonObj.AddComponent<Button>();
        btn.targetGraphic = img;
        buttonObj.AddComponent<ButtonPressEffect>();

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(90f, 30f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = colonist.name;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Colonist capturedColonist = colonist;
        btn.onClick.AddListener(() => HandleColonistButton(capturedColonist));

        colonistButtons[colonist] = btn;
    }

    void HandleColonistButton(Colonist colonist)
    {
        if (colonist == null)
            return;

        if (infoCard == null)
            infoCard = FindObjectOfType<ColonistInfoCard>();

        infoCard?.Show(colonist);

        if (selected != null && selected != colonist)
            CancelManualMove(selected);
    }

    void Update()
    {
        if (selected != null && Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
                world.z = 0f;
                selected.SetTask(new Task(world));
            }

            CancelActionUI.Hide();
            infoCard?.Hide();
            selected = null;
        }
    }

    public void BeginManualMove(Colonist colonist)
    {
        if (colonist == null)
            return;

        if (infoCard == null)
            infoCard = FindObjectOfType<ColonistInfoCard>();

        selected = colonist;

        if (colonistButtons.TryGetValue(colonist, out Button btn))
        {
            RectTransform rect = btn.GetComponent<RectTransform>();
            CancelActionUI.Show(rect, () => CancelManualMove(colonist));
        }
        else if (panel != null)
        {
            CancelActionUI.Show(panel, () => CancelManualMove(colonist));
        }
    }

    public void CancelManualMove(Colonist colonist)
    {
        if (colonist == null)
        {
            CancelActionUI.Hide();
            return;
        }

        if (selected == colonist)
            selected = null;

        CancelActionUI.Hide();

        if (infoCard == null)
            infoCard = FindObjectOfType<ColonistInfoCard>();

        if (infoCard != null && !infoCard.IsVisible)
            infoCard.Show(colonist);
    }
}
