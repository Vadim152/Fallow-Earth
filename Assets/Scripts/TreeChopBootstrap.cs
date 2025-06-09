using UnityEngine;

public static class TreeChopBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        var controllerType = System.Type.GetType("TreeChopController, Assembly-CSharp");
        if (controllerType == null)
            return;
        if (Object.FindObjectOfType(controllerType) != null)
            return;

        new GameObject("TreeChopController").AddComponent(controllerType);
    }
}
