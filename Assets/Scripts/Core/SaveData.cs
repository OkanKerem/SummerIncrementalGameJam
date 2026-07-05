using System;
using System.Collections.Generic;

namespace Universes.Core
{
    [Serializable]
    public class UpgradeSaveEntry
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class SaveData
    {
        public double universeDna;
        public string activeVariantId = "stable";
        public List<UpgradeSaveEntry> upgradeLevels = new();
        public int totalCollapses;
        public double totalDnaEarned;
        public double totalStardustLifetime;
        public string lastSaveUtc;
    }
}
