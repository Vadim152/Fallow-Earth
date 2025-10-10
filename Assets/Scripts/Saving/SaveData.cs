using System;
using System.Collections.Generic;
using UnityEngine;

namespace FallowEarth.Saving
{
    /// <summary>
    /// Flexible serializable container that stores arbitrary save data as JSON strings.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            public string json;
        }

        [SerializeField]
        private List<Entry> entries = new List<Entry>();

        private readonly Dictionary<string, string> lookup = new Dictionary<string, string>();

        public IReadOnlyList<Entry> Entries => entries;

        public void Set<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            string json = JsonUtility.ToJson(value);

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key)
                {
                    entries[i] = new Entry { key = key, json = json };
                    lookup[key] = json;
                    return;
                }
            }

            entries.Add(new Entry { key = key, json = json });
            lookup[key] = json;
        }

        public bool TryGet<T>(string key, out T value)
        {
            EnsureLookup();
            if (lookup.TryGetValue(key, out string json))
            {
                value = JsonUtility.FromJson<T>(json);
                return true;
            }

            value = default;
            return false;
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            return TryGet(key, out T value) ? value : defaultValue;
        }

        public void Clear()
        {
            entries.Clear();
            lookup.Clear();
        }

        private void EnsureLookup()
        {
            if (lookup.Count == entries.Count)
                return;

            lookup.Clear();
            foreach (var entry in entries)
            {
                lookup[entry.key] = entry.json;
            }
        }
    }
}
