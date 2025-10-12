using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColonistMassManagementUI : MonoBehaviour
{
    private readonly List<Colonist> colonists = new List<Colonist>();
    private readonly Dictionary<Colonist, Toggle> selectionToggles = new Dictionary<Colonist, Toggle>();
    private readonly Dictionary<string, ColonistScheduleActivity[]> scheduleTemplates = new Dictionary<string, ColonistScheduleActivity[]>();
    private readonly List<string> roleOptions = new List<string>();
    private readonly List<string> scheduleOptions = new List<string>();

    private RectTransform panel;
    private RectTransform colonistListContainer;
    private Text roleLabel;
    private Text scheduleLabel;
    private int roleIndex;
    private int scheduleIndex;
    private TaskManager taskManager;

    void Awake()
    {
        SetupCanvas();
        CreatePanel();
        CreateControls();
        InitializeScheduleTemplates();
    }

    void Start()
    {
        taskManager = FindObjectOfType<TaskManager>();
        RefreshColonistList();
        PopulateRoleOptions();
        UpdateRoleLabel();
        UpdateScheduleLabel();
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
        GameObject panelObj = new GameObject("MassManagementPanel", typeof(RectTransform));
        panelObj.transform.SetParent(transform, false);
        panel = panelObj.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(1f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-20f, -20f);
        panel.sizeDelta = new Vector2(320f, 430f);

        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.92f);

        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6f;
    }

    void CreateControls()
    {
        colonistListContainer = CreateSection("ColonistList");
        colonistListContainer.gameObject.AddComponent<VerticalLayoutGroup>().spacing = 4f;

        RectTransform roleRow = CreateSection("RoleRow");
        HorizontalLayoutGroup roleLayout = roleRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        roleLayout.spacing = 6f;
        roleLayout.childForceExpandWidth = false;
        roleLayout.childAlignment = TextAnchor.MiddleCenter;

        CreateButton(roleRow, "◀", PrevRole);
        roleLabel = CreateText(roleRow, "Role", TextAnchor.MiddleCenter);
        roleLabel.rectTransform.sizeDelta = new Vector2(160f, 30f);
        CreateButton(roleRow, "▶", NextRole);
        CreateButton(roleRow, "Apply", HandleApplyRole);

        RectTransform scheduleRow = CreateSection("ScheduleRow");
        HorizontalLayoutGroup scheduleLayout = scheduleRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        scheduleLayout.spacing = 6f;
        scheduleLayout.childForceExpandWidth = false;
        scheduleLayout.childAlignment = TextAnchor.MiddleCenter;

        CreateButton(scheduleRow, "◀", PrevSchedule);
        scheduleLabel = CreateText(scheduleRow, "Schedule", TextAnchor.MiddleCenter);
        scheduleLabel.rectTransform.sizeDelta = new Vector2(160f, 30f);
        CreateButton(scheduleRow, "▶", NextSchedule);
        CreateButton(scheduleRow, "Apply", HandleApplySchedule);

        RectTransform footer = CreateSection("Footer");
        HorizontalLayoutGroup footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 6f;
        footerLayout.childAlignment = TextAnchor.MiddleCenter;

        CreateButton(footer, "Select All", () => SetSelection(true));
        CreateButton(footer, "Clear", () => SetSelection(false));
    }

    RectTransform CreateSection(string name)
    {
        GameObject sectionObj = new GameObject(name, typeof(RectTransform));
        sectionObj.transform.SetParent(panel, false);
        RectTransform rect = sectionObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 0f);
        return rect;
    }

    Button CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObj = new GameObject(label + "Button", typeof(RectTransform));
        buttonObj.transform.SetParent(parent, false);
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        float width = Mathf.Max(60f, label.Length * 12f);
        rect.sizeDelta = new Vector2(width, 30f);
        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(action);
        Text text = CreateText(buttonObj.transform, label, TextAnchor.MiddleCenter);
        text.color = Color.black;
        return button;
    }

    Text CreateText(Transform parent, string content, TextAnchor alignment)
    {
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(parent, false);
        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.black;
        text.text = content;
        text.alignment = alignment;
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }

    void InitializeScheduleTemplates()
    {
        scheduleTemplates.Clear();
        scheduleOptions.Clear();

        ColonistSchedule standard = new ColonistSchedule();
        standard.SetRange(0, 6, ColonistScheduleActivity.Sleep);
        standard.SetRange(6, 22, ColonistScheduleActivity.Work);
        standard.SetRange(22, 24, ColonistScheduleActivity.Recreation);
        scheduleTemplates["Standard Day"] = standard.ToArray();
        scheduleOptions.Add("Standard Day");

        ColonistSchedule night = new ColonistSchedule();
        night.SetRange(0, 2, ColonistScheduleActivity.Recreation);
        night.SetRange(2, 10, ColonistScheduleActivity.Sleep);
        night.SetRange(10, 20, ColonistScheduleActivity.Work);
        night.SetRange(20, 24, ColonistScheduleActivity.Recreation);
        scheduleTemplates["Night Shift"] = night.ToArray();
        scheduleOptions.Add("Night Shift");

        ColonistSchedule recovery = new ColonistSchedule(ColonistScheduleActivity.Medical);
        recovery.SetRange(6, 10, ColonistScheduleActivity.Sleep);
        scheduleTemplates["Medical Rest"] = recovery.ToArray();
        scheduleOptions.Add("Medical Rest");
    }

    void RefreshColonistList()
    {
        colonists.Clear();
        colonists.AddRange(FindObjectsOfType<Colonist>());

        foreach (Transform child in colonistListContainer)
            Destroy(child.gameObject);
        selectionToggles.Clear();

        foreach (Colonist colonist in colonists)
        {
            Toggle toggle = CreateColonistToggle(colonist);
            selectionToggles[colonist] = toggle;
        }
    }

    Toggle CreateColonistToggle(Colonist colonist)
    {
        GameObject toggleObj = new GameObject(colonist.name + "Toggle", typeof(RectTransform));
        toggleObj.transform.SetParent(colonistListContainer, false);
        Image img = toggleObj.AddComponent<Image>();
        img.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.targetGraphic = img;
        RectTransform rect = toggleObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 28f);
        Text label = CreateText(toggleObj.transform, colonist.name, TextAnchor.MiddleLeft);
        label.color = Color.black;
        return toggle;
    }

    void PopulateRoleOptions()
    {
        roleOptions.Clear();
        if (taskManager != null)
        {
            foreach (var role in taskManager.AvailableRoles)
                roleOptions.Add(role.RoleName);
        }
        else
        {
            foreach (var role in ColonistRoleLibrary.DefaultRoles)
                roleOptions.Add(role.RoleName);
        }
        if (roleOptions.Count == 0)
            roleOptions.Add("Generalist");
        roleIndex = 0;
    }

    void PrevRole()
    {
        if (roleOptions.Count == 0)
            return;
        roleIndex = (roleIndex - 1 + roleOptions.Count) % roleOptions.Count;
        UpdateRoleLabel();
    }

    void NextRole()
    {
        if (roleOptions.Count == 0)
            return;
        roleIndex = (roleIndex + 1) % roleOptions.Count;
        UpdateRoleLabel();
    }

    void PrevSchedule()
    {
        if (scheduleOptions.Count == 0)
            return;
        scheduleIndex = (scheduleIndex - 1 + scheduleOptions.Count) % scheduleOptions.Count;
        UpdateScheduleLabel();
    }

    void NextSchedule()
    {
        if (scheduleOptions.Count == 0)
            return;
        scheduleIndex = (scheduleIndex + 1) % scheduleOptions.Count;
        UpdateScheduleLabel();
    }

    void UpdateRoleLabel()
    {
        if (roleLabel == null)
            return;
        if (roleOptions.Count == 0)
            roleLabel.text = "Role";
        else
            roleLabel.text = roleOptions[roleIndex];
    }

    void UpdateScheduleLabel()
    {
        if (scheduleLabel == null)
            return;
        if (scheduleOptions.Count == 0)
            scheduleLabel.text = "Schedule";
        else
            scheduleLabel.text = scheduleOptions[scheduleIndex];
    }

    List<Colonist> GetSelectedColonists()
    {
        List<Colonist> selected = new List<Colonist>();
        foreach (var kvp in selectionToggles)
        {
            if (kvp.Value != null && kvp.Value.isOn)
                selected.Add(kvp.Key);
        }
        return selected;
    }

    void HandleApplyRole()
    {
        if (roleOptions.Count == 0)
            return;
        List<Colonist> selected = GetSelectedColonists();
        if (selected.Count == 0)
            return;

        string roleName = roleOptions[roleIndex];
        ColonistRoleProfile profile = FindRoleProfile(roleName);
        foreach (Colonist colonist in selected)
            colonist.AssignRoleProfile(profile, true);
    }

    void HandleApplySchedule()
    {
        if (scheduleOptions.Count == 0)
            return;
        List<Colonist> selected = GetSelectedColonists();
        if (selected.Count == 0)
            return;

        string scheduleName = scheduleOptions[scheduleIndex];
        if (!scheduleTemplates.TryGetValue(scheduleName, out ColonistScheduleActivity[] template))
            return;

        foreach (Colonist colonist in selected)
        {
            ColonistSchedule schedule = colonist.Schedule ?? new ColonistSchedule();
            schedule.LoadFrom(template);
            colonist.CancelTasks();
        }
    }

    void SetSelection(bool selected)
    {
        foreach (var toggle in selectionToggles.Values)
            if (toggle != null)
                toggle.isOn = selected;
    }

    ColonistRoleProfile FindRoleProfile(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        if (taskManager != null)
        {
            foreach (var role in taskManager.AvailableRoles)
                if (role.RoleName == name)
                    return role;
        }
        foreach (var role in ColonistRoleLibrary.DefaultRoles)
            if (role.RoleName == name)
                return role;
        return null;
    }
}
