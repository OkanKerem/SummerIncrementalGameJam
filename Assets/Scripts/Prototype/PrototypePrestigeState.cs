using System;

namespace Universes.Prototype
{
    public class PrototypePrestigeState
    {
        private readonly int[] _levels = new int[8];

        public double UniverseDna { get; private set; }
        public int TotalCollapses { get; private set; }

        public event Action OnChanged;

        public int GetLevel(PrototypePrestigeUpgradeType type) => _levels[(int)type];

        public int GetLevel(PrototypePrestigeUpgradeDefinition definition) =>
            definition != null ? GetLevel(definition.upgradeType) : 0;

        public int GetCost(PrototypePrestigeUpgradeDefinition definition) =>
            definition != null ? PrototypePrestigeModifiers.GetUpgradeCost(GetLevel(definition)) : int.MaxValue;

        public bool TryPurchase(PrototypePrestigeUpgradeDefinition definition)
        {
            if (definition == null)
                return false;

            var level = GetLevel(definition);
            if (definition.maxLevel > 0 && level >= definition.maxLevel)
                return false;

            if (!definition.ArePrerequisitesMet(this, out _))
                return false;

            var cost = PrototypePrestigeModifiers.GetUpgradeCost(level);
            if (UniverseDna < cost)
                return false;

            UniverseDna -= cost;
            _levels[(int)definition.upgradeType] = level + 1;
            OnChanged?.Invoke();
            PrototypePrestigeSave.Save(this);
            return true;
        }

        public void AddDna(int amount)
        {
            if (amount <= 0)
                return;

            UniverseDna += amount;
            OnChanged?.Invoke();
            PrototypePrestigeSave.Save(this);
        }

        public void RecordCollapse()
        {
            TotalCollapses++;
            OnChanged?.Invoke();
            PrototypePrestigeSave.Save(this);
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
