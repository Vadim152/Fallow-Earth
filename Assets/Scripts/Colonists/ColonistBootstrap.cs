using UnityEngine;

public static class ColonistBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectsOfType<Colonist>().Length > 0)
            return;

        if (Object.FindObjectOfType<TaskManager>() == null)
            new GameObject("TaskManager").AddComponent<TaskManager>();

        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject($"Colonist{i + 1}");
            go.AddComponent<Colonist>();
            go.transform.position = new Vector3(i * 2, 0, 0);
        }
    }
}
