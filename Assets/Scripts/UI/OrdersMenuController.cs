using UnityEngine;
using UnityEngine.UI;

public class OrdersMenuController : MonoBehaviour
{
    private Image toggleButtonImage;
    private RectTransform toggleButtonRect;
    private ManagementTabController tabs;
    private GameObject ordersSection;

    void Start()
    {
        tabs = ManagementTabController.FindOrCreate();
        BuildWorkTab();
    }

    void BuildWorkTab()
    {
        ordersSection = tabs.CreateSection(ManagementTabController.WorkTabId, "\u041f\u0440\u0438\u043a\u0430\u0437\u044b");
        tabs.CreateLabel(ordersSection.transform, "\u041f\u043b\u0430\u043d\u0438\u0440\u0443\u0439\u0442\u0435 \u0440\u0430\u0431\u043e\u0442\u044b \u043d\u0430 \u0442\u0435\u0440\u0440\u0438\u0442\u043e\u0440\u0438\u0438", TextAnchor.MiddleLeft, 16);
        CreateChopButton();
        CreateHarvestButton();
        CreateCancelButton();
    }

    void CreateChopButton()
    {
        TreeChopController ctrl = FindObjectOfType<TreeChopController>();
        tabs.CreateActionButton(ordersSection, "\u0421\u0440\u0443\u0431\u0438\u0442\u044c \u0434\u0435\u0440\u0435\u0432\u044c\u044f", () =>
        {
            if (ctrl != null)
            {
                ctrl.ToggleSelecting();
                if (ctrl.IsSelecting)
                    global::CancelActionUI.Show(toggleButtonRect, ctrl.ToggleSelecting);
                else
                    global::CancelActionUI.Hide();
            }
            ToggleMenu();
        });

        if (ctrl != null)
        {
            Transform button = ordersSection.transform.Find("\u0421\u0440\u0443\u0431\u0438\u0442\u044c \u0434\u0435\u0440\u0435\u0432\u044c\u044fActionButton");
            if (button != null)
            {
                Image img = button.GetComponent<Image>();
                RectTransform rect = button.GetComponent<RectTransform>();
                if (img != null && rect != null)
                    ctrl.AssignButton(img, rect);
            }
        }
    }

    void CreateHarvestButton()
    {
        tabs.CreateActionButton(ordersSection, "\u0421\u0431\u043e\u0440 \u044f\u0433\u043e\u0434", () =>
        {
            MapGenerator map = FindObjectOfType<MapGenerator>();
            if (map != null)
                EventLogUI.AddEntry("\u041a\u043e\u043b\u043e\u043d\u0438\u0441\u0442\u044b \u043e\u0442\u043c\u0435\u0447\u0430\u044e\u0442 \u043a\u0443\u0441\u0442\u044b \u0441 \u043f\u043b\u043e\u0434\u0430\u043c\u0438 \u0434\u043b\u044f \u0441\u0431\u043e\u0440\u0430.");
        });
    }

    void CreateCancelButton()
    {
        tabs.CreateActionButton(ordersSection, "\u041e\u0442\u043c\u0435\u043d\u0438\u0442\u044c \u0432\u0441\u0451", () =>
        {
            CancelActionUI.Hide();
            EventLogUI.AddEntry("\u0412\u0441\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u043f\u043e\u0440\u0443\u0447\u0435\u043d\u0438\u044f \u0431\u044b\u043b\u0438 \u0441\u043d\u044f\u0442\u044b.");
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
        tabs.ToggleMenu(this, ManagementTabController.WorkTabId, toggleButtonImage);
    }
}
