using UnityEngine;
using UnityEngine.UI;

public class ColonistInfoCard : MonoBehaviour
{
    private Canvas canvas;
    private GameObject panel;

    private Text nameText;
    private Slider moodSlider;
    private Slider healthSlider;
    private Text activityText;

    private Slider hungerSlider;
    private Slider fatigueSlider;
    private Slider stressSlider;
    private Slider socialSlider;

    private Colonist current;
    private ColonistMenuController menuController;
    private ColonistControlUI controlUI;
    private Colonist pendingManualMove;
    private bool awaitingManualMove;

    public bool IsVisible => panel != null && panel.activeSelf;

    void Start()
    {
        SetupCanvas();
        CreateUI();
        EnsureDependencies();
        Hide();
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

    void CreateUI()
    {
        panel = new GameObject("ColonistInfoCard");
        panel.transform.SetParent(canvas.transform, false);
        Image img = panel.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.95f);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(20f, 20f);
        rt.sizeDelta = new Vector2(320f, 170f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(5, 5, 5, 5);
        layout.spacing = 2f;

        // Block 1 - Info
        GameObject block1 = new GameObject("Info");
        block1.transform.SetParent(panel.transform, false);
        VerticalLayoutGroup b1Layout = block1.AddComponent<VerticalLayoutGroup>();
        b1Layout.spacing = 2f;

        nameText = CreateLabel(block1, "Colonist");
        CreateStat(block1, "\ud83d\ude42", out moodSlider); // mood
        CreateStat(block1, "\u2764", out healthSlider); // health
        activityText = CreateLabel(block1, "Idle");

        // Block 2 - Needs
        GameObject block2 = new GameObject("Needs");
        block2.transform.SetParent(panel.transform, false);
        VerticalLayoutGroup b2Layout = block2.AddComponent<VerticalLayoutGroup>();
        b2Layout.spacing = 2f;

        CreateStat(block2, "\ud83c\udf72", out hungerSlider);
        CreateStat(block2, "\ud83d\ude34", out fatigueSlider);
        CreateStat(block2, "\ud83e\udd2a", out stressSlider);
        CreateStat(block2, "\ud83d\udcac", out socialSlider);

        // Block 3 - Actions
        GameObject block3 = new GameObject("Actions");
        block3.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup b3Layout = block3.AddComponent<HorizontalLayoutGroup>();
        b3Layout.spacing = 4f;

        CreateButton(block3, "\u274c", HandleCancelTasks);
        CreateButton(block3, "\u26cf", HandleManualPriority);
        CreateButton(block3, "\ud83d\udcfd", HandleForceRest);
        CreateButton(block3, "\ud83c\udfaf", HandleManualMove);
    }

    Text CreateLabel(GameObject parent, string text)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent.transform, false);
        Text t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.black;
        t.text = text;
        t.alignment = TextAnchor.MiddleLeft;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 18f);
        return t;
    }

    void CreateStat(GameObject parent, string icon, out Slider slider)
    {
        GameObject row = new GameObject("Row");
        row.transform.SetParent(parent.transform, false);
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 4f;
        CreateLabel(row, icon);
        GameObject sObj = new GameObject("Slider");
        sObj.transform.SetParent(row.transform, false);
        slider = sObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        RectTransform rt = sObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 18f);
    }

    void CreateButton(GameObject parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject bObj = new GameObject("Button");
        bObj.transform.SetParent(parent.transform, false);
        Image img = bObj.AddComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        Button btn = bObj.AddComponent<Button>();
        btn.targetGraphic = img;
        bObj.AddComponent<ButtonPressEffect>();
        btn.onClick.AddListener(action);
        Text txt = CreateLabel(bObj, label);
        txt.alignment = TextAnchor.MiddleCenter;
        RectTransform rt = bObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(40f, 25f);
    }

    void Update()
    {
        if (current != null && panel.activeSelf)
        {
            nameText.text = current.name;
            moodSlider.value = current.mood;
            healthSlider.value = current.health;
            activityText.text = current.activity;
            hungerSlider.value = current.hunger;
            fatigueSlider.value = current.fatigue;
            stressSlider.value = current.stress;
            socialSlider.value = current.social;
        }

        if (awaitingManualMove && pendingManualMove != null && Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
                world.z = 0f;
                pendingManualMove.TryAssignTask(new Task(world));
            }
            awaitingManualMove = false;
            pendingManualMove = null;
        }
    }

    public void Show(Colonist c)
    {
        EnsureDependencies();
        current = c;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
        current = null;
    }

    void EnsureDependencies()
    {
        if (menuController == null)
            menuController = FindObjectOfType<ColonistMenuController>();
        if (controlUI == null)
            controlUI = FindObjectOfType<ColonistControlUI>();
    }

    void HandleCancelTasks()
    {
        if (current == null)
            return;

        EnsureDependencies();
        if (awaitingManualMove && pendingManualMove == current)
        {
            awaitingManualMove = false;
            pendingManualMove = null;
        }

        controlUI?.CancelManualMove(current);
        current.CancelTasks();
    }

    void HandleManualPriority()
    {
        if (current == null)
            return;

        EnsureDependencies();
        menuController?.FocusColonist(current);
    }

    void HandleForceRest()
    {
        if (current == null)
            return;

        EnsureDependencies();
        Bed bed = Bed.FindAvailable(current.transform.position);
        if (bed == null)
        {
            Debug.LogWarning($"No available bed found for {current.name}.");
            return;
        }

        controlUI?.CancelManualMove(current);
        if (awaitingManualMove && pendingManualMove == current)
        {
            awaitingManualMove = false;
            pendingManualMove = null;
        }

        current.CancelTasks();
        float restDuration = Mathf.Lerp(3f, 6f, Mathf.Clamp01(current.fatigue));
        if (!current.TryAssignTask(new RestTask(bed, restDuration)))
            Debug.LogWarning($"{current.name} could not reach the selected bed for rest.");
    }

    void HandleManualMove()
    {
        if (current == null)
            return;

        EnsureDependencies();
        current.CancelTasks();

        if (controlUI != null)
        {
            controlUI.BeginManualMove(current);
            awaitingManualMove = false;
            pendingManualMove = null;
        }
        else
        {
            awaitingManualMove = true;
            pendingManualMove = current;
        }

        Hide();
    }
}
