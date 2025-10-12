using System.Linq;
using FallowEarth.Saving;
using UnityEngine;

public static class ColonistBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        TrySubscribeToWorldData();
        SpawnIfNeeded();
    }

    static void TrySubscribeToWorldData()
    {
        if (!WorldDataManager.HasInstance)
            return;

        WorldDataManager.Instance.WorldDataLoaded -= HandleWorldDataLoaded;
        WorldDataManager.Instance.WorldDataLoaded += HandleWorldDataLoaded;
    }

    static void HandleWorldDataLoaded(WorldSaveData data)
    {
        if (HasColonistsInSave(data))
            return;

        SpawnIfNeeded();
    }

    static bool HasColonistsInSave(WorldSaveData data)
    {
        if (data?.regions == null)
            return false;

        string colonistType = typeof(Colonist).AssemblyQualifiedName;
        return data.regions.Any(region => region?.creatures != null && region.creatures.Any(record => record?.type == colonistType));
    }

    static void SpawnIfNeeded()
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
