using UnityEngine;

public static class ManagementTabBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        ManagementTabController.FindOrCreate();

        if (Object.FindObjectOfType<TacticalOverlayController>() == null)
            new GameObject(nameof(TacticalOverlayController)).AddComponent<TacticalOverlayController>();
        if (Object.FindObjectOfType<ColonistCardsPanel>() == null)
            new GameObject(nameof(ColonistCardsPanel)).AddComponent<ColonistCardsPanel>();
        if (Object.FindObjectOfType<EventLogUI>() == null)
            new GameObject(nameof(EventLogUI)).AddComponent<EventLogUI>();
        if (Object.FindObjectOfType<ResearchMenuController>() == null)
            new GameObject(nameof(ResearchMenuController)).AddComponent<ResearchMenuController>();
    }
}
