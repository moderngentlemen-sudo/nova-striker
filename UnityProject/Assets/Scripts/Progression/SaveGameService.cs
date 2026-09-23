using System;
using System.IO;
using UnityEngine;

namespace NovaStriker.Progression
{
    public interface ISaveBackend
    {
        bool Exists(string key);
        string ReadText(string key);
        void WriteText(string key, string content);
        void Delete(string key);
    }

    public sealed class LocalFileSaveBackend : ISaveBackend
    {
        private readonly string root;

        public LocalFileSaveBackend(string rootPath)
        {
            root = rootPath;
        }

        public bool Exists(string key)
        {
            return File.Exists(PathFor(key));
        }

        public string ReadText(string key)
        {
            return File.ReadAllText(PathFor(key));
        }

        public void WriteText(string key, string content)
        {
            Directory.CreateDirectory(root);

            string path = PathFor(key);
            string temp = path + ".tmp";

            File.WriteAllText(temp, content);

            if (File.Exists(path))
                File.Delete(path);

            File.Move(temp, path);
        }

        public void Delete(string key)
        {
            string path = PathFor(key);

            if (File.Exists(path))
                File.Delete(path);
        }

        private string PathFor(string key)
        {
            return Path.Combine(root, key + ".json");
        }
    }

    /// <summary>
    /// Versioned save authority with a replaceable backend for console/cloud
    /// integrations. Gameplay code never talks directly to a storefront or
    /// platform save API.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    [DisallowMultipleComponent]
    public sealed class SaveGameService : MonoBehaviour
    {
        [SerializeField] private string saveKey = "nova-striker-save";
        [SerializeField] private bool loadOnAwake = true;

        private ISaveBackend backend;

        public static SaveGameService Active { get; private set; }
        public NovaSaveData Current { get; private set; }

        public event Action<NovaSaveData> Loaded;
        public event Action<NovaSaveData> Saved;

        private void Awake()
        {
            if (Active && Active != this)
            {
                Destroy(this);
                return;
            }

            Active = this;
            DontDestroyOnLoad(gameObject);

            backend =
                new LocalFileSaveBackend(
                    Application.persistentDataPath
                );

            if (loadOnAwake)
                Load();
        }

        private void OnDestroy()
        {
            if (Active == this)
                Active = null;
        }

        public void SetBackend(
            ISaveBackend replacement,
            bool reload = true)
        {
            if (replacement == null)
                return;

            backend = replacement;

            if (reload)
                Load();
        }

        public NovaSaveData Load()
        {
            if (backend == null)
            {
                Current = NovaSaveData.CreateDefault();
                return Current;
            }

            try
            {
                if (!backend.Exists(saveKey))
                {
                    Current = NovaSaveData.CreateDefault();
                    Loaded?.Invoke(Current);
                    return Current;
                }

                string json =
                    backend.ReadText(saveKey);

                NovaSaveData data =
                    JsonUtility.FromJson<NovaSaveData>(json);

                Current =
                    MigrateAndNormalize(data);

                Loaded?.Invoke(Current);
                return Current;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Nova Striker save could not be loaded. " +
                    "A fresh in-memory save will be used.\n" +
                    exception,
                    this
                );

                Current = NovaSaveData.CreateDefault();
                Loaded?.Invoke(Current);
                return Current;
            }
        }

        public bool Save()
        {
            if (backend == null)
                return false;

            if (Current == null)
                Current = NovaSaveData.CreateDefault();

            Current.version =
                NovaSaveData.CurrentVersion;

            try
            {
                string json =
                    JsonUtility.ToJson(
                        Current,
                        true
                    );

                backend.WriteText(
                    saveKey,
                    json
                );

                Saved?.Invoke(Current);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Nova Striker save failed.\n" +
                    exception,
                    this
                );

                return false;
            }
        }

        public void NewGame()
        {
            Current = NovaSaveData.CreateDefault();
            Save();
            Loaded?.Invoke(Current);
        }

        public void DeleteSave()
        {
            backend?.Delete(saveKey);
            Current = NovaSaveData.CreateDefault();
            Loaded?.Invoke(Current);
        }

        private static NovaSaveData MigrateAndNormalize(
            NovaSaveData data)
        {
            if (data == null)
                return NovaSaveData.CreateDefault();

            data.unlockedWeapons ??= new();
            data.unlockedGuardians ??= new();
            data.unlockedSkills ??= new();
            data.weaponMastery ??= new();
            data.ownedEntitlements ??= new();
            data.ownedCosmetics ??= new();
            data.equippedCosmetics ??= new();

            if (!data.unlockedWeapons.Contains("pulse"))
                data.unlockedWeapons.Add("pulse");

            if (!data.unlockedGuardians.Contains("Aegis"))
                data.unlockedGuardians.Add("Aegis");

            data.sectorIndex =
                Mathf.Clamp(
                    data.sectorIndex,
                    0,
                    5
                );

            data.actIndex =
                Mathf.Clamp(
                    data.actIndex,
                    0,
                    2
                );

            data.checkpointIndex =
                Mathf.Max(
                    0,
                    data.checkpointIndex
                );

            data.version =
                NovaSaveData.CurrentVersion;

            return data;
        }
    }
}
