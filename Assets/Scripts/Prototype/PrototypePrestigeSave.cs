using System;
using UnityEngine;

namespace Universes.Prototype
{
    [Serializable]
    public class PrototypePrestigeSaveData
    {
        public double universeDna;
        public int totalCollapses;
        public int[] upgradeLevels = Array.Empty<int>();
    }

    public static class PrototypePrestigeSave
    {
        private const string SaveKey = "PrototypePrestige_v1";
        private const int UpgradeTypeCount = 8;

        public static void Save(PrototypePrestigeState prestige)
        {
            if (prestige == null)
                return;

            var data = new PrototypePrestigeSaveData
            {
                universeDna = prestige.UniverseDna,
                totalCollapses = prestige.TotalCollapses,
                upgradeLevels = prestige.GetAllLevelsArray()
            };

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static void Load(PrototypePrestigeState prestige)
        {
            if (prestige == null || !PlayerPrefs.HasKey(SaveKey))
                return;

            var json = PlayerPrefs.GetString(SaveKey);
            if (string.IsNullOrEmpty(json))
                return;

            var data = JsonUtility.FromJson<PrototypePrestigeSaveData>(json);
            if (data == null)
                return;

            prestige.LoadFromSave(data.universeDna, data.totalCollapses, data.upgradeLevels);
        }
    }
}
