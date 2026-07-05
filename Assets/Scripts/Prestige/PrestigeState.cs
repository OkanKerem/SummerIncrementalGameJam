using System;
using System.Collections.Generic;
using Universes.Core;

namespace Universes.Prestige
{
    public class PrestigeState
    {
        public double UniverseDna { get; set; }
        public string ActiveVariantId { get; set; } = "stable";
        public int TotalCollapses { get; set; }
        public double TotalDnaEarned { get; set; }
        public double TotalStardustLifetime { get; set; }

        private readonly Dictionary<string, int> _upgradeLevels = new();

        public event Action OnChanged;

        public void NotifyChanged() => OnChanged?.Invoke();

        public int GetUpgradeLevel(string id) =>
            _upgradeLevels.TryGetValue(id, out var level) ? level : 0;

        public void SetUpgradeLevel(string id, int level)
        {
            if (level <= 0)
                _upgradeLevels.Remove(id);
            else
                _upgradeLevels[id] = level;
        }

        public void ClearUpgradeLevels() => _upgradeLevels.Clear();

        public Dictionary<string, int> GetAllUpgradeLevels() => new(_upgradeLevels);

        public bool TryPurchaseUpgrade(UpgradeDefinition def, GameBalance balance)
        {
            var level = GetUpgradeLevel(def.id);
            if (level >= def.maxLevel)
                return false;

            var cost = def.GetCost(level);
            if (UniverseDna < cost)
                return false;

            UniverseDna -= cost;
            SetUpgradeLevel(def.id, level + 1);
            OnChanged?.Invoke();
            return true;
        }

        public void AddDna(double amount)
        {
            if (amount <= 0)
                return;

            UniverseDna += amount;
            TotalDnaEarned += amount;
            OnChanged?.Invoke();
        }
    }
}
