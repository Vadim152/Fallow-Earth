using System.Text;
using FallowEarth.Infrastructure;
using FallowEarth.ResourcesSystem;
using UnityEngine;
using UnityEngine.UI;

public class ResourceLedgerUI : MonoBehaviour
{
    private Text text;
    private IResourceManager resourceManager;

    void Start()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("Canvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            c.AddComponent<CanvasScaler>();
            c.AddComponent<GraphicRaycaster>();
        }

        GameObject textObj = new GameObject("ResourceLedger");
        textObj.transform.SetParent(canvas.transform, false);
        text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(320f, 140f);
        text.alignment = TextAnchor.UpperRight;
        text.color = Color.black;

        if (GameServices.TryResolve(out resourceManager))
        {
            resourceManager.LedgerChanged += OnLedgerChanged;
            UpdateText(resourceManager.GetSnapshot());
        }
    }

    void OnDestroy()
    {
        if (resourceManager != null)
            resourceManager.LedgerChanged -= OnLedgerChanged;
    }

    void Update()
    {
        if (text == null || resourceManager == null)
            return;
        UpdateText(resourceManager.GetSnapshot());
    }

    void OnLedgerChanged(ResourceManager.ResourceLedgerSnapshot snapshot)
    {
        UpdateText(snapshot);
    }

    void UpdateText(ResourceManager.ResourceLedgerSnapshot snapshot)
    {
        if (text == null)
            return;
        var builder = new StringBuilder();
        builder.AppendLine("Resources");
        float totalMass = 0f;
        foreach (var entry in snapshot.Entries)
        {
            builder.AppendLine($"{entry.Definition.DisplayName} ({entry.Quality.GetDisplayName()}): {entry.Amount}");
            totalMass += entry.TotalMass;
        }
        builder.Append($"Total mass: {totalMass:F1}");
        text.text = builder.ToString();
    }
}
