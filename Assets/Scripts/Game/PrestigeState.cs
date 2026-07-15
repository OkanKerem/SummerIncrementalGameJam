using System;

namespace Universes.Game
{
    public class PrestigeState
    {
        private readonly int[] _levels = new int[System.Enum.GetValues(typeof(PrestigeUpgradeType)).Length];

        public double UniverseDna { get; private set; }
        public int TotalCollapses { get; private set; }

        public event Action OnChanged;

        public int GetLevel(PrestigeUpgradeType type) => _levels[(int)type];

        public int GetLevel(PrestigeUpgradeDefinition definition) =>
            definition != null ? GetLevel(definition.upgradeType) : 0;

        public int GetCost(PrestigeUpgradeDefinition definition) =>
            definition != null ? PrestigeModifiers.GetUpgradeCost(GetLevel(definition)) : int.MaxValue;

        public bool TryPurchase(PrestigeUpgradeDefinition definition)
        {
            if (definition == null)
                return false;

            var level = GetLevel(definition);
            if (definition.maxLevel > 0 && level >= definition.maxLevel)
                return false;

            if (!definition.ArePrerequisitesMet(this, out _))
                return false;

            var cost = PrestigeModifiers.GetUpgradeCost(level);
            if (UniverseDna < cost)
                return false;

            UniverseDna -= cost;
            _levels[(int)definition.upgradeType] = level + 1;
            OnChanged?.Invoke();
            PrestigeSave.Save(this);
            return true;
        }

        public void AddDna(int amount)
        {
            if (amount <= 0)
                return;

            UniverseDna += amount;
            OnChanged?.Invoke();
            PrestigeSave.Save(this);
        }

        public void RecordCollapse()
        {
            TotalCollapses++;
            OnChanged?.Invoke();
            PrestigeSave.Save(this);
        }

        public int[] GetAllLevelsArray()
        {
            var copy = new int[_levels.Length];
            Array.Copy(_levels, copy, _levels.Length);
            return copy;
        }

        public void LoadFromSave(double universeDna, int totalCollapses, int[] upgradeLevels)
        {
            UniverseDna = Math.Max(0, universeDna);
            TotalCollapses = Math.Max(0, totalCollapses);

            if (upgradeLevels == null)
                return;

            var count = Math.Min(upgradeLevels.Length, _levels.Length);
            for (var i = 0; i < count; i++)
                _levels[i] = Math.Max(0, upgradeLevels[i]);
        }
    }
}
