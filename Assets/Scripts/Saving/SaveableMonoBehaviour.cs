using System;
using UnityEngine;

namespace FallowEarth.Saving
{
    /// <summary>
    /// Base behaviour for MonoBehaviours that should be tracked by the save system.
    /// Generates a persistent identifier and registers with the WorldDataManager.
    /// </summary>
    public abstract class SaveableMonoBehaviour : MonoBehaviour, ISaveable, IMutableSaveId
    {
        [SerializeField]
        private string saveId;

        public string SaveId => saveId;

        public virtual Vector3 SavePosition => transform.position;

        protected virtual void Awake()
        {
            EnsureSaveId();
        }

        protected virtual void OnEnable()
        {
            WorldDataManager.Instance?.Register(this);
        }

        protected virtual void OnDisable()
        {
            WorldDataManager.Instance?.Unregister(this);
        }

        public void SetSaveId(string newId)
        {
            if (string.IsNullOrEmpty(newId))
                throw new ArgumentException("Save id cannot be null or empty", nameof(newId));

            if (saveId == newId)
                return;

            string oldId = saveId;
            saveId = newId;
            if (WorldDataManager.HasInstance)
            {
                WorldDataManager.Instance.NotifyIdentifierChanged(this, oldId, newId);
            }
        }

        protected void EnsureSaveId()
        {
            if (string.IsNullOrEmpty(saveId))
            {
                saveId = Guid.NewGuid().ToString();
            }
        }

        public abstract SaveCategory Category { get; }
        public abstract void PopulateSaveData(SaveData saveData);
        public abstract void LoadFromSaveData(SaveData saveData);
    }
}
