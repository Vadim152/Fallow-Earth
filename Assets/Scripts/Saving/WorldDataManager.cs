using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FallowEarth.Saving
{
    /// <summary>
    /// Central repository for saveable world data, providing autosave, slot management and
    /// asynchronous region loading.
    /// </summary>
    public class WorldDataManager : MonoBehaviour
    {
        private const string LastSlotKey = "WorldDataManager_LastSlot";

        public static WorldDataManager Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        [Header("Persistence")]
        [Tooltip("Number of world units along one side of a region chunk.")]
        public int regionSize = 32;

        [Tooltip("Seconds between automatic saves when a slot is active. Set to 0 to disable.")]
        public float autoSaveIntervalSeconds = 120f;

        [Tooltip("Automatically load the last used slot on startup.")]
        public bool autoLoadLastSlot = true;

        [Tooltip("Default slot id to use when no previous saves are available.")]
        public string defaultSlotId = "autosave";

        private readonly Dictionary<string, ISaveable> saveables = new Dictionary<string, ISaveable>();
        private readonly Dictionary<SaveCategory, Dictionary<string, ISaveable>> saveablesByCategory = new Dictionary<SaveCategory, Dictionary<string, ISaveable>>();

        private readonly HashSet<Vector2Int> loadedRegions = new HashSet<Vector2Int>();

        private WorldSaveData currentWorld = new WorldSaveData();
        private string currentSlotId;
        private float autoSaveTimer;

        public event Action<WorldSaveData> WorldDataLoaded;
        public event Action<Vector2Int> RegionLoaded;
        public event Action<Vector2Int> RegionUnloaded;

        public WorldSaveData CurrentWorld => currentWorld;
        public string CurrentSlotId => currentSlotId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (SaveCategory category in Enum.GetValues(typeof(SaveCategory)))
            {
                if (!saveablesByCategory.ContainsKey(category))
                {
                    saveablesByCategory[category] = new Dictionary<string, ISaveable>();
                }
            }
        }

        private bool worldDataBroadcasted;

        private IEnumerator Start()
        {
            yield return null; // allow other bootstraps to run

            if (autoLoadLastSlot)
            {
                string slotToLoad = PlayerPrefs.GetString(LastSlotKey, defaultSlotId);
                if (!string.IsNullOrEmpty(slotToLoad))
                {
                    if (!LoadFromSlot(slotToLoad))
                    {
                        currentSlotId = slotToLoad;
                        currentWorld = new WorldSaveData();
                    }
                }
            }

            if (!worldDataBroadcasted)
            {
                WorldDataLoaded?.Invoke(currentWorld);
                worldDataBroadcasted = true;
            }
        }

        private void Update()
        {
            if (autoSaveIntervalSeconds <= 0f)
                return;
            if (string.IsNullOrEmpty(currentSlotId))
                return;

            autoSaveTimer += Time.unscaledDeltaTime;
            if (autoSaveTimer >= autoSaveIntervalSeconds)
            {
                autoSaveTimer = 0f;
                try
                {
                    SaveToSlot(currentSlotId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WorldDataManager] Autosave failed: {ex}");
                }
            }
        }

        public void Register(ISaveable saveable)
        {
            if (saveable == null)
                return;

            if (string.IsNullOrEmpty(saveable.SaveId))
            {
                Debug.LogWarning("[WorldDataManager] Attempted to register saveable without id.");
                return;
            }

            saveables[saveable.SaveId] = saveable;

            if (!saveablesByCategory.TryGetValue(saveable.Category, out var categoryDict))
            {
                categoryDict = new Dictionary<string, ISaveable>();
                saveablesByCategory[saveable.Category] = categoryDict;
            }

            categoryDict[saveable.SaveId] = saveable;
        }

        public void Unregister(ISaveable saveable)
        {
            if (saveable == null)
                return;

            if (saveables.TryGetValue(saveable.SaveId, out var existing) && existing == saveable)
            {
                saveables.Remove(saveable.SaveId);
            }

            if (saveablesByCategory.TryGetValue(saveable.Category, out var categoryDict))
            {
                if (categoryDict.TryGetValue(saveable.SaveId, out var catExisting) && catExisting == saveable)
                {
                    categoryDict.Remove(saveable.SaveId);
                }
            }
        }

        public void NotifyIdentifierChanged(ISaveable saveable, string oldId, string newId)
        {
            if (saveable == null)
                return;

            if (!string.IsNullOrEmpty(oldId))
            {
                saveables.Remove(oldId);
                if (saveablesByCategory.TryGetValue(saveable.Category, out var categoryDict))
                {
                    categoryDict.Remove(oldId);
                }
            }

            if (!string.IsNullOrEmpty(newId))
            {
                saveables[newId] = saveable;
                if (!saveablesByCategory.TryGetValue(saveable.Category, out var dict))
                {
                    dict = new Dictionary<string, ISaveable>();
                    saveablesByCategory[saveable.Category] = dict;
                }
                dict[newId] = saveable;
            }
        }

        public WorldSaveData CaptureWorldState()
        {
            var result = new WorldSaveData();
            foreach (var saveable in saveables.Values)
            {
                try
                {
                    var record = new SaveRecord
                    {
                        id = saveable.SaveId,
                        type = saveable.GetType().AssemblyQualifiedName,
                        category = saveable.Category,
                        position = saveable.SavePosition,
                    };
                    saveable.PopulateSaveData(record.data);

                    Vector2Int regionCoord = WorldToRegion(record.position, regionSize);
                    RegionSaveData region = result.GetOrCreateRegion(regionCoord);

                    switch (record.category)
                    {
                        case SaveCategory.Structure:
                            region.structures.Add(record);
                            break;
                        case SaveCategory.Creature:
                            region.creatures.Add(record);
                            break;
                        case SaveCategory.Zone:
                            region.zones.Add(record);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WorldDataManager] Failed to capture state for {saveable}: {ex}");
                }
            }

            currentWorld = result;
            return result;
        }

        public bool SaveToSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
                throw new ArgumentException("Slot id cannot be null or empty", nameof(slotId));

            WorldSaveData data = CaptureWorldState();
            var slot = new SaveSlotData
            {
                slotId = slotId,
                savedAtIsoUtc = DateTime.UtcNow.ToString("o"),
                world = data
            };

            string json = JsonUtility.ToJson(slot, true);
            string path = GetSlotPath(slotId);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);

            currentSlotId = slotId;
            autoSaveTimer = 0f;

            PlayerPrefs.SetString(LastSlotKey, slotId);
            PlayerPrefs.Save();

            return true;
        }

        public bool LoadFromSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
                return false;

            string path = GetSlotPath(slotId);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[WorldDataManager] Save slot '{slotId}' not found.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                var slot = JsonUtility.FromJson<SaveSlotData>(json);
                if (slot == null || slot.world == null)
                {
                    Debug.LogError($"[WorldDataManager] Failed to deserialize slot '{slotId}'.");
                    return false;
                }

                currentSlotId = slot.slotId ?? slotId;
                PlayerPrefs.SetString(LastSlotKey, currentSlotId);
                PlayerPrefs.Save();

                RestoreWorldState(slot.world);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldDataManager] Error loading slot '{slotId}': {ex}");
                return false;
            }
        }

        public IEnumerable<SaveSlotInfo> GetAvailableSlots()
        {
            string directory = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(directory))
                yield break;

            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                string json = File.ReadAllText(file);
                SaveSlotData slot;
                try
                {
                    slot = JsonUtility.FromJson<SaveSlotData>(json);
                }
                catch
                {
                    continue;
                }

                if (slot == null)
                    continue;

                DateTime.TryParse(slot.savedAtIsoUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime savedAt);
                yield return new SaveSlotInfo
                {
                    slotId = !string.IsNullOrEmpty(slot.slotId) ? slot.slotId : Path.GetFileNameWithoutExtension(file),
                    savedAtUtc = savedAt
                };
            }
        }

        public void RestoreWorldState(WorldSaveData worldData)
        {
            ClearAllSaveables();
            currentWorld = worldData ?? new WorldSaveData();
            loadedRegions.Clear();
            WorldDataLoaded?.Invoke(currentWorld);
            worldDataBroadcasted = true;
        }

        public Coroutine LoadRegionAsync(Vector2Int regionCoord)
        {
            return StartCoroutine(LoadRegionCoroutine(regionCoord));
        }

        private IEnumerator LoadRegionCoroutine(Vector2Int regionCoord)
        {
            if (loadedRegions.Contains(regionCoord))
                yield break;

            RegionSaveData region = currentWorld?.GetRegion(regionCoord);
            if (region == null)
            {
                loadedRegions.Add(regionCoord);
                yield break;
            }

            yield return null; // simulate asynchronous work

            LoadRegionImmediate(region);
            loadedRegions.Add(regionCoord);
            RegionLoaded?.Invoke(regionCoord);
        }

        public void UnloadRegion(Vector2Int regionCoord)
        {
            if (!loadedRegions.Contains(regionCoord))
                return;

            var toRemove = new List<ISaveable>();
            foreach (var saveable in saveables.Values)
            {
                Vector2Int saveableRegion = WorldToRegion(saveable.SavePosition, regionSize);
                if (saveableRegion == regionCoord)
                {
                    toRemove.Add(saveable);
                }
            }

            foreach (var saveable in toRemove)
            {
                DestroySaveable(saveable);
            }

            loadedRegions.Remove(regionCoord);
            RegionUnloaded?.Invoke(regionCoord);
        }

        private void LoadRegionImmediate(RegionSaveData region)
        {
            if (region == null)
                return;

            foreach (var record in region.structures)
            {
                EnsureSaveable(record);
            }

            foreach (var record in region.creatures)
            {
                EnsureSaveable(record);
            }

            foreach (var record in region.zones)
            {
                EnsureSaveable(record);
            }
        }

        private void ClearAllSaveables()
        {
            var existing = new List<ISaveable>(saveables.Values);
            foreach (var saveable in existing)
            {
                DestroySaveable(saveable);
            }

            saveables.Clear();
            foreach (var category in saveablesByCategory.Values)
                category.Clear();
        }

        private void DestroySaveable(ISaveable saveable)
        {
            if (saveable == null)
                return;

            if (saveable is SaveableMonoBehaviour behaviour)
            {
                if (behaviour != null)
                {
                    Destroy(behaviour.gameObject);
                }
            }
            else if (saveable is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Unregister(saveable);
        }

        private ISaveable EnsureSaveable(SaveRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.id))
                return null;

            if (saveables.TryGetValue(record.id, out var existing))
            {
                existing.LoadFromSaveData(record.data);
                return existing;
            }

            Type type = Type.GetType(record.type);
            if (type == null)
            {
                Debug.LogError($"[WorldDataManager] Unable to resolve type '{record.type}' for save record '{record.id}'.");
                return null;
            }

            ISaveable instance = null;
            try
            {
                if (typeof(SaveableMonoBehaviour).IsAssignableFrom(type))
                {
                    var go = new GameObject(type.Name)
                    {
                        hideFlags = HideFlags.None
                    };
                    go.transform.position = record.position;
                    go.SetActive(false);
                    var component = go.AddComponent(type) as SaveableMonoBehaviour;
                    if (component == null)
                    {
                        Destroy(go);
                        return null;
                    }
                    component.SetSaveId(record.id);
                    go.SetActive(true);
                    instance = component;
                }
                else
                {
                    instance = Activator.CreateInstance(type) as ISaveable;
                    if (instance == null)
                    {
                        Debug.LogError($"[WorldDataManager] Could not instantiate '{record.type}'.");
                        return null;
                    }

                    if (instance is IMutableSaveId mutable)
                    {
                        mutable.SetSaveId(record.id);
                    }

                    Register(instance);
                }

                instance.LoadFromSaveData(record.data);
                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldDataManager] Failed to instantiate saveable '{record.type}': {ex}");
                return null;
            }
        }

        private string GetSlotPath(string slotId)
        {
            string directory = Path.Combine(Application.persistentDataPath, "Saves");
            return Path.Combine(directory, slotId + ".json");
        }

        public static Vector2Int WorldToRegion(Vector3 worldPosition, int regionSize)
        {
            if (regionSize <= 0)
                regionSize = 1;

            int x = Mathf.FloorToInt(worldPosition.x / regionSize);
            int y = Mathf.FloorToInt(worldPosition.y / regionSize);
            return new Vector2Int(x, y);
        }

        public Vector2Int WorldToRegion(Vector3 worldPosition)
        {
            return WorldToRegion(worldPosition, regionSize);
        }
    }
}
