using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BottomLeftMenuController : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform panel;

    void Start()
    {
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;
        CreatePanel();
        StartCoroutine(SetupButtons());
    }

    void CreatePanel()
    {
        GameObject panelObj = new GameObject("BottomLeftPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        panel = panelObj.AddComponent<RectTransform>();
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 0f);
        panel.pivot = new Vector2(0f, 0f);
        panel.anchoredPosition = new Vector2(20f, 20f);
        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.LowerLeft;
        // Force the layout group to give each button a size so they're visible
        // immediately when the panel is created. Without this Unity may collapse
        // the children to zero height which hides the buttons until the user
        // manually toggles these options in the inspector.
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.reverseArrangement = true;
    }

    IEnumerator SetupButtons()
    {
        yield return null; // wait a frame so other controllers create their buttons
        ReparentButton("ChopTreesButton");
        ReparentButton("BuildButton");
        ReparentButton("ZoneButton");
    }

    void ReparentButton(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null || panel == null)
            return;
        RectTransform rt = obj.GetComponent<RectTransform>();
        obj.transform.SetParent(panel, false);
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
