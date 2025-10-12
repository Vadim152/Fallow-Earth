using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates compact cards for every colonist inside the health tab so the
/// player can monitor critical stats at a glance.
/// </summary>
public class ColonistCardsPanel : MonoBehaviour
{
    private class Card
    {
        public Colonist colonist;
        public GameObject root;
        public Text nameText;
        public Slider moodSlider;
        public Slider healthSlider;
        public Text activityText;
        public bool lowMoodNotified;
        public bool lowHealthNotified;
    }

    private readonly List<Card> cards = new List<Card>();
    private readonly Dictionary<Colonist, Card> lookup = new Dictionary<Colonist, Card>();

    private ManagementTabController tabs;
    private GameObject grid;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        tabs = ManagementTabController.FindOrCreate();
        GameObject section = tabs.CreateSection(ManagementTabController.HealthTabId, "\u041a\u0430\u0440\u0442\u043e\u0447\u043a\u0438 \u043a\u043e\u043b\u043e\u043d\u0438\u0441\u0442\u043e\u0432");
        tabs.CreateLabel(section.transform, "\u041d\u0430\u0436\u043c\u0438\u0442\u0435 \u043d\u0430 \u043f\u0430\u043d\u0435\u043b\u044c \u043a\u043e\u043b\u043e\u043d\u0438\u0441\u0442\u0430 \u0432 \u0440\u0430\u0441\u0448\u0438\u0440\u0435\u043d\u043d\u043e\u0435 \u043c\u0435\u043d\u044e \u0434\u043b\u044f \u0440\u0443\u0447\u043d\u043e\u0433\u043e \u0443\u043f\u0440\u0430\u0432\u043b\u0435\u043d\u0438\u044f.", TextAnchor.MiddleLeft, 14);

        grid = new GameObject("ColonistCardGrid");
        grid.transform.SetParent(section.transform, false);
        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(180f, 120f);
        layout.spacing = new Vector2(8f, 8f);
        layout.childAlignment = TextAnchor.UpperLeft;
    }

    void Update()
    {
        RefreshColonists();
        UpdateCards();
    }

    void RefreshColonists()
    {
        Colonist[] colonists = FindObjectsOfType<Colonist>();
        HashSet<Colonist> seen = new HashSet<Colonist>(colonists);

        // Remove cards for colonists that no longer exist
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            Card card = cards[i];
            if (!seen.Contains(card.colonist) || card.colonist == null)
            {
                Destroy(card.root);
                lookup.Remove(card.colonist);
                cards.RemoveAt(i);
            }
        }

        // Add new cards
        foreach (Colonist colonist in colonists)
        {
            if (lookup.ContainsKey(colonist))
                continue;
            CreateCard(colonist);
        }
    }

    void UpdateCards()
    {
        foreach (Card card in cards)
        {
            if (card.colonist == null)
                continue;

            card.nameText.text = card.colonist.name;
            card.moodSlider.value = card.colonist.mood;
            card.healthSlider.value = card.colonist.health;
            card.activityText.text = card.colonist.activity;

            if (!card.lowMoodNotified && card.colonist.mood < 0.3f)
            {
                EventLogUI.AddEntry($"\u041d\u0430\u0441\u0442\u0440\u043e\u0435\u043d\u0438\u0435 {card.colonist.name} \u043d\u0430 \u0433\u0440\u0430\u043d\u0438 \u0441\u0440\u044b\u0432\u0430!");
                card.lowMoodNotified = true;
            }
            else if (card.lowMoodNotified && card.colonist.mood > 0.45f)
            {
                EventLogUI.AddEntry($"{card.colonist.name} \u0441\u043d\u043e\u0432\u0430 \u0447\u0443\u0432\u0441\u0442\u0432\u0443\u0435\u0442 \u0441\u0435\u0431\u044f \u043b\u0443\u0447\u0448\u0435.");
                card.lowMoodNotified = false;
            }

            if (!card.lowHealthNotified && card.colonist.health < 0.35f)
            {
                EventLogUI.AddEntry($"\u0417\u0434\u043e\u0440\u043e\u0432\u044c\u0435 {card.colonist.name} \u0442\u0440\u0435\u0431\u0443\u0435\u0442 \u043d\u0435\u043c\u0435\u0434\u043b\u0438\u0442\u0435\u043b\u044c\u043d\u043e\u0433\u043e \u0432\u043d\u0438\u043c\u0430\u043d\u0438\u044f!");
                card.lowHealthNotified = true;
            }
            else if (card.lowHealthNotified && card.colonist.health > 0.6f)
            {
                EventLogUI.AddEntry($"{card.colonist.name} \u043f\u0440\u0438\u043d\u043e\u0432\u0438\u043b\u0441\u044f \u043a \u043f\u043e\u0441\u043b\u0435\u0434\u043d\u0435\u043c\u0443 \u0443\u0445\u043e\u0434\u0443.");
                card.lowHealthNotified = false;
            }
        }
    }

    void CreateCard(Colonist colonist)
    {
        Card card = new Card();
        card.colonist = colonist;

        card.root = new GameObject(colonist.name + "Card");
        card.root.transform.SetParent(grid.transform, false);
        Image bg = card.root.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.2f);

        VerticalLayoutGroup layout = card.root.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        card.nameText = tabs.CreateLabel(card.root.transform, colonist.name, TextAnchor.MiddleLeft, 18, FontStyle.Bold);
        card.activityText = tabs.CreateLabel(card.root.transform, colonist.activity, TextAnchor.MiddleLeft, 14);

        card.moodSlider = CreateStatSlider(card.root.transform, new Color(0.2f, 0.6f, 1f, 1f));
        card.healthSlider = CreateStatSlider(card.root.transform, new Color(0.8f, 0.2f, 0.2f, 1f));

        Button btn = card.root.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        btn.onClick.AddListener(() => FocusColonist(colonist));

        cards.Add(card);
        lookup[colonist] = card;
    }

    Slider CreateStatSlider(Transform parent, Color fillColor)
    {
        Slider slider = tabs.CreateProgressBar(parent.gameObject);
        Image fill = slider.fillRect.GetComponent<Image>();
        if (fill != null)
            fill.color = fillColor;
        slider.value = 1f;
        return slider;
    }

    void FocusColonist(Colonist colonist)
    {
        ColonistMenuController menu = FindObjectOfType<ColonistMenuController>();
        if (menu != null)
            menu.FocusColonist(colonist);

        ColonistInfoCard info = FindObjectOfType<ColonistInfoCard>();
        if (info != null)
            info.Show(colonist);
    }
}
