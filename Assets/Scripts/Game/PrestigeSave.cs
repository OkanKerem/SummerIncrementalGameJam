using System;
using UnityEngine;

namespace Universes.Game
{
    [Serializable]
    public class PrestigeSaveData
    {
        public double universeDna;
        public int totalCollapses;
        public int[] upgradeLevels = Array.Empty<int>();
    }

    public static class PrestigeSave
    {
        private const string SaveKey = "PrototypePrestige_v1";
        public static void Save(PrestigeState prestige)
        {
            if (prestige == null)
                return;

            var data = new PrestigeSaveData
            {
                universeDna = prestige.UniverseDna,
                totalCollapses = prestige.TotalCollapses,
                upgradeLevels = prestige.GetAllLevelsArray()
            };

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static void Load(PrestigeState prestige)
        {
            if (prestige == null || !PlayerPrefs.HasKey(SaveKey))
                return;

            var json = PlayerPrefs.GetString(SaveKey);
            if (string.IsNullOrEmpty(json))
                return;

            var data = JsonUtility.FromJson<PrestigeSaveData>(json);
            if (data == null)
                return;

            prestige.LoadFromSave(data.universeDna, data.totalCollapses, data.upgradeLevels);
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }
}
