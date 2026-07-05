using UnityEngine;

namespace Universes.Prototype
{
    [CreateAssetMenu(fileName = "PrototypePrestigeUpgrade", menuName = "Universes/Prototype Prestige Upgrade")]
    public class PrototypePrestigeUpgradeDefinition : ScriptableObject
    {
        public PrototypePrestigeUpgradeType upgradeType;
        public string displayName = "Prestige Upgrade";
        [TextArea] public string description;
        public int maxLevel;
    }
}
