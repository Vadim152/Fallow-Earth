using UnityEngine;

public static class OrdersMenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<OrdersMenuController>() == null)
            new GameObject("OrdersMenuController").AddComponent<OrdersMenuController>();
    }
}
