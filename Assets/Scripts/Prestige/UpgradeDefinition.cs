using UnityEngine;

namespace Universes.Prestige
{
    [CreateAssetMenu(fileName = "Upgrade", menuName = "Universes/Upgrade Definition")]
    public class UpgradeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public int maxLevel = 5;
        public UpgradeEffectType effectType;
        public double effectPerLevel = 0.1;
        public double baseCost = 10;
        public double costScale = 1.75;

        public double GetCost(int currentLevel)
        {
            if (currentLevel >= maxLevel)
                return double.MaxValue;
            return baseCost * System.Math.Pow(costScale, currentLevel);
        }
    }
}
