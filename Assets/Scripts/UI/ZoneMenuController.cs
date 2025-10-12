using UnityEngine;
using UnityEngine.UI;

public class ZoneMenuController : MonoBehaviour
{
    private Image toggleButtonImage;
    private RectTransform toggleButtonRect;
    private ManagementTabController tabs;
    private GameObject zoneSection;

    void Start()
    {
        tabs = ManagementTabController.FindOrCreate();
        BuildZoneTab();
    }

    void BuildZoneTab()
    {
        zoneSection = tabs.CreateSection(ManagementTabController.HealthTabId, "\u0417\u043e\u043d\u044b");
        tabs.CreateLabel(zoneSection.transform, "\u041e\u0440\u0433\u0430\u043d\u0438\u0437\u0443\u0439\u0442\u0435 \u0441\u043a\u043b\u0430\u0434\u044b \u0438 \u0436\u0438\u043b\u044b\u0435 \u043f\u043e\u043c\u0435\u0449\u0435\u043d\u0438\u044f", TextAnchor.MiddleLeft, 16);

        GameObject grid = new GameObject("ZoneButtonGrid");
        grid.transform.SetParent(zoneSection.transform, false);
        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(160f, 46f);
        layout.spacing = new Vector2(6f, 6f);

        CreateStockpileButton(grid);
        CreateFutureZonePlaceholder(grid, "\u0413\u0438\u0434\u0440\u043e\u043f\u043e\u043d\u0438\u043a\u0430");
        CreateFutureZonePlaceholder(grid, "\u041c\u0435\u0434\u043f\u0443\u043d\u043a\u0442");
        CreateFutureZonePlaceholder(grid, "\u041e\u0442\u0434\u044b\u0445");
    }

    void CreateStockpileButton(GameObject parent)
    {
        tabs.CreateActionButton(parent, "\u0421\u043a\u043b\u0430\u0434", () =>
        {
            StockpileZoneController ctrl = FindObjectOfType<StockpileZoneController>();
            if (ctrl != null)
            {
                ctrl.TogglePlacing();
                if (ctrl.IsPlacing)
                    global::CancelActionUI.Show(toggleButtonRect, ctrl.TogglePlacing);
                else
                    global::CancelActionUI.Hide();
            }
            ToggleMenu();
        });
    }

    void CreateFutureZonePlaceholder(GameObject parent, string label)
    {
        tabs.CreateActionButton(parent, label, () =>
        {
            EventLogUI.AddEntry($"\u0417\u043e\u043d\u0430 \"{label}\" \u043f\u043e\u043a\u0430 \u043d\u0435 \u0433\u043e\u0442\u043e\u0432\u0430, \u043d\u043e \u043a\u043e\u043b\u043e\u043d\u0438\u0441\u0442\u044b \u043f\u043b\u0430\u043d\u0438\u0440\u0443\u044e\u0442 \u0435\u0451 \u0437\u0430\u0441\u0442\u0440\u043e\u0439\u043a\u0443.");
        });
    }

    public void AssignToggleButton(Image img, RectTransform rect)
    {
        toggleButtonImage = img;
        toggleButtonRect = rect;
        if (toggleButtonImage != null)
            tabs?.NotifyToggleRegistered(toggleButtonImage);
    }

    public RectTransform ToggleButtonRect => toggleButtonRect;

    public void ToggleMenu()
    {
        if (tabs == null)
            return;
        tabs.ToggleMenu(this, ManagementTabController.HealthTabId, toggleButtonImage);
    }
}
