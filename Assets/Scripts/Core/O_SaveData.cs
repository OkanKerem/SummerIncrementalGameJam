using System;
using System.Collections.Generic;

namespace Universes.Core
{
    [Serializable]
    public class O_UpgradeSaveEntry
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class O_SaveData
    {
        public double universeDna;
        public string activeVariantId = "stable";
        public List<O_UpgradeSaveEntry> upgradeLevels = new();
        public int totalCollapses;
        public double totalDnaEarned;
        public double totalStardustLifetime;
        public string lastSaveUtc;
    }
}
