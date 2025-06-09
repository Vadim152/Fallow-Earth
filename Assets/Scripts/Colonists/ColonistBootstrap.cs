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

        var map = Object.FindObjectOfType<MapGenerator>();
        if (map == null)
            map = new GameObject("MapGenerator").AddComponent<MapGenerator>();

        Vector3 center = new Vector3(map.width / 2f, map.height / 2f, 0);

        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject($"Colonist{i + 1}");
            go.AddComponent<Colonist>();
            go.transform.position = center + new Vector3(i, 0, 0);
        }
    }
}
