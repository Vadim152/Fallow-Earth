using UnityEngine;
using UnityEngine.UI;

public class TreeChopController : MonoBehaviour
{
    private MapGenerator map;
    private TaskManager taskManager;
    private bool selecting;
    private Image buttonImage;

    void Start()
    {
        map = FindObjectOfType<MapGenerator>();
        taskManager = FindObjectOfType<TaskManager>();
        CreateUI();
    }

    void CreateUI()
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

        GameObject buttonObj = new GameObject("ChopTreesButton");
        buttonObj.transform.SetParent(canvas.transform, false);
        buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        Button btn = buttonObj.AddComponent<Button>();
        btn.targetGraphic = buttonImage;
        btn.onClick.AddListener(ToggleSelecting);

        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(120f, 30f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.text = "Chop Trees";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.color = Color.black;
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    void ToggleSelecting()
    {
        selecting = !selecting;
        if (buttonImage != null)
            buttonImage.color = selecting ? new Color(0.6f, 1f, 0.6f, 0.9f) : new Color(0.8f, 0.8f, 0.8f, 0.9f);
    }

    void Update()
    {
        if (!selecting || map == null || taskManager == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.FloorToInt(world.x);
            int y = Mathf.FloorToInt(world.y);
            if (map.HasTree(x, y))
            {
                int tx = x;
                int ty = y;
                taskManager.AddTask(new Task(new Vector2(tx + 0.5f, ty + 0.5f), c => map.RemoveTree(tx, ty)));
            }
        }
    }
}
