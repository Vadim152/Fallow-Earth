using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight event journal that keeps players informed about colony changes.
/// </summary>
public class EventLogUI : MonoBehaviour
{
    private static EventLogUI instance;
    private readonly Queue<string> entries = new Queue<string>();
    private const int MaxEntries = 30;

    private ManagementTabController tabs;
    private Text logText;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        tabs = ManagementTabController.FindOrCreate();
        GameObject section = tabs.CreateSection(ManagementTabController.ResearchTabId, "\u0416\u0443\u0440\u043d\u0430\u043b \u0441\u043e\u0431\u044b\u0442\u0438\u0439");
        tabs.CreateLabel(section.transform, "\u041f\u043e\u0441\u043b\u0435\u0434\u043d\u0438\u0435 \u0441\u043e\u0431\u044b\u0442\u0438\u044f \u043a\u043e\u043b\u043e\u043d\u0438\u0438", TextAnchor.MiddleLeft, 16);

        GameObject box = new GameObject("LogBox");
        box.transform.SetParent(section.transform, false);
        Image bg = box.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.15f);
        RectTransform rt = box.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 220f);

        logText = tabs.CreateLabel(box.transform, string.Empty, TextAnchor.UpperLeft, 14);
        logText.alignment = TextAnchor.UpperLeft;
        logText.supportRichText = true;

        AddEntry("\u0416\u0443\u0440\u043d\u0430\u043b \u0437\u0430\u043f\u0443\u0449\u0435\u043d. \u041a\u043e\u043b\u043e\u043d\u0438\u0441\u0442\u044b \u043d\u0430\u0447\u0438\u043d\u0430\u044e\u0442 \u043d\u043e\u0432\u0443\u044e \u0433\u043b\u0430\u0432\u0443 \u0438\u0441\u0442\u043e\u0440\u0438\u0438.");
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void AddEntry(string message)
    {
        if (instance == null)
            return;
        instance.InternalAdd(message);
    }

    void InternalAdd(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        if (entries.Count >= MaxEntries)
            entries.Dequeue();
        entries.Enqueue($"[{System.DateTime.Now:HH:mm}] {message}");
        RefreshText();
    }

    void RefreshText()
    {
        StringBuilder builder = new StringBuilder();
        foreach (string entry in entries)
        {
            builder.AppendLine(entry);
        }
        logText.text = builder.ToString();
    }
}
