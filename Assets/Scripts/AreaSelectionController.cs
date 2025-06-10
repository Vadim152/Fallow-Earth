using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AreaSelectionController : MonoBehaviour
{
    public static bool IsSelecting { get; private set; }

    public float holdDelay = 0.3f;
    public float moveTolerance = 10f;

    private Canvas canvas;
    private GameObject boxObj;
    private RectTransform boxRect;

    private bool isHolding;
    private bool selecting;
    private float holdTimer;
    private Vector2 startScreen;
    private bool moved;

    void Start()
    {
        SetupCanvas();
        CreateSelectionBox();
    }

    void SetupCanvas()
    {
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("Canvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            c.AddComponent<CanvasScaler>();
            c.AddComponent<GraphicRaycaster>();
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    void CreateSelectionBox()
    {
        boxObj = new GameObject("SelectionBox");
        boxObj.transform.SetParent(canvas.transform, false);
        Image img = boxObj.AddComponent<Image>();
        img.color = new Color(0f, 1f, 0f, 0.2f);
        boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxObj.SetActive(false);
    }

    void Update()
    {
        if (Input.touchSupported)
            HandleTouch();
        else
            HandleMouse();
    }

    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isHolding = true;
            holdTimer = 0f;
            startScreen = Input.mousePosition;
            moved = false;
        }

        if (isHolding)
        {
            holdTimer += Time.unscaledDeltaTime;
            if (!Input.GetMouseButton(0))
            {
                ResetSelection();
            }
            else if (!selecting && Vector2.Distance(Input.mousePosition, startScreen) > moveTolerance)
            {
                moved = true;
                ResetSelection();
            }
            else if (!selecting && holdTimer >= holdDelay && !moved)
            {
                selecting = true;
                IsSelecting = true;
                boxObj.SetActive(true);
            }
        }

        if (selecting)
        {
            UpdateBox(startScreen, Input.mousePosition);
            if (Input.GetMouseButtonUp(0))
            {
                LogSelection(Input.mousePosition);
                ResetSelection();
            }
        }
    }

    void HandleTouch()
    {
        if (Input.touchCount == 0)
            return;
        Touch t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Began)
        {
            isHolding = true;
            holdTimer = 0f;
            startScreen = t.position;
            moved = false;
        }

        if (isHolding)
        {
            holdTimer += Time.unscaledDeltaTime;
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                ResetSelection();
            }
            else if (!selecting && Vector2.Distance(t.position, startScreen) > moveTolerance)
            {
                moved = true;
                ResetSelection();
            }
            else if (!selecting && holdTimer >= holdDelay && !moved)
            {
                selecting = true;
                IsSelecting = true;
                boxObj.SetActive(true);
            }
        }

        if (selecting)
        {
            UpdateBox(startScreen, t.position);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                LogSelection(t.position);
                ResetSelection();
            }
        }
    }

    void UpdateBox(Vector2 start, Vector2 end)
    {
        RectTransform parent = canvas.transform as RectTransform;
        Vector2 s, e;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, start, canvas.worldCamera, out s);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, end, canvas.worldCamera, out e);
        Vector2 center = (s + e) / 2f;
        boxRect.localPosition = center;
        boxRect.sizeDelta = new Vector2(Mathf.Abs(e.x - s.x), Mathf.Abs(e.y - s.y));
    }

    void LogSelection(Vector2 endScreen)
    {
        Vector3 startWorld = Camera.main.ScreenToWorldPoint(startScreen);
        Vector3 endWorld = Camera.main.ScreenToWorldPoint(endScreen);
        startWorld.z = 0f;
        endWorld.z = 0f;
        Debug.Log($"Selected area from {startWorld} to {endWorld}");

        MapGenerator map = FindObjectOfType<MapGenerator>();
        if (map == null)
            return;

        map.BeginNewZone();

        int xMin = Mathf.FloorToInt(Mathf.Min(startWorld.x, endWorld.x));
        int xMax = Mathf.FloorToInt(Mathf.Max(startWorld.x, endWorld.x));
        int yMin = Mathf.FloorToInt(Mathf.Min(startWorld.y, endWorld.y));
        int yMax = Mathf.FloorToInt(Mathf.Max(startWorld.y, endWorld.y));

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                map.SetZone(x, y);
            }
        }
    }

    void ResetSelection()
    {
        isHolding = false;
        selecting = false;
        IsSelecting = false;
        moved = false;
        boxObj.SetActive(false);
    }
}

