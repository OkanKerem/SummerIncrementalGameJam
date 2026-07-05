using UnityEngine;

namespace Universes.Prestige
{
    [CreateAssetMenu(fileName = "UpgradeCatalog", menuName = "Universes/Upgrade Catalog")]
    public class UpgradeCatalog : ScriptableObject
    {
        public UpgradeDefinition[] upgrades;
    }
}
