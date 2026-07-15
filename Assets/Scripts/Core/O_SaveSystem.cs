using System;
using System.IO;
using Universes.Prestige;
using UnityEngine;

namespace Universes.Core
{
    public class O_SaveSystem : MonoBehaviour
    {
        private const string SaveFileName = "universes_save.json";

        public static O_SaveSystem Instance { get; private set; }

        public O_SaveData Data { get; private set; } = new();

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void OnApplicationQuit() => Save();
        private void OnApplicationPause(bool pause) { if (pause) Save(); }

        public void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var json = File.ReadAllText(SavePath);
                    Data = JsonUtility.FromJson<O_SaveData>(json) ?? new O_SaveData();
                }
                else
                {
                    Data = new O_SaveData();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save load failed: {e.Message}");
                Data = new O_SaveData();
            }
        }

        public void Save()
        {
            try
            {
                Data.lastSaveUtc = DateTime.UtcNow.ToString("o");
                var json = JsonUtility.ToJson(Data, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save write failed: {e.Message}");
            }
        }

        public void ApplyToPrestige(O_PrestigeState prestige)
        {
            prestige.UniverseDna = Data.universeDna;
            prestige.ActiveVariantId = string.IsNullOrEmpty(Data.activeVariantId) ? "stable" : Data.activeVariantId;
            prestige.TotalCollapses = Data.totalCollapses;
            prestige.TotalDnaEarned = Data.totalDnaEarned;
            prestige.TotalStardustLifetime = Data.totalStardustLifetime;

            prestige.ClearUpgradeLevels();
            foreach (var entry in Data.upgradeLevels)
                prestige.SetUpgradeLevel(entry.id, entry.level);
        }

        public void PullFromPrestige(O_PrestigeState prestige)
        {
            Data.universeDna = prestige.UniverseDna;
            Data.activeVariantId = prestige.ActiveVariantId;
            Data.totalCollapses = prestige.TotalCollapses;
            Data.totalDnaEarned = prestige.TotalDnaEarned;
            Data.totalStardustLifetime = prestige.TotalStardustLifetime;

            Data.upgradeLevels.Clear();
            foreach (var kvp in prestige.GetAllUpgradeLevels())
            {
                Data.upgradeLevels.Add(new O_UpgradeSaveEntry { id = kvp.Key, level = kvp.Value });
            }
        }
    }
}
