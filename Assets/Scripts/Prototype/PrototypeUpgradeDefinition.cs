using UnityEngine;

namespace Universes.Prototype
{
    [CreateAssetMenu(fileName = "PrototypeUpgrade", menuName = "Universes/Prototype Upgrade")]
    public class PrototypeUpgradeDefinition : ScriptableObject
    {
        public PrototypeUpgradeType upgradeType;
        public string displayName = "Upgrade";
        [TextArea] public string description;
        public double baseCost = 15;
        public double costScale = 1.55;

        public double GetCost(int currentLevel) =>
            baseCost * System.Math.Pow(costScale, currentLevel);
    }
}
