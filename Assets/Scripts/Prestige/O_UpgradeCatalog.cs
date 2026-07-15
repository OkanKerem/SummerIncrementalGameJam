using UnityEngine;

namespace Universes.Prestige
{
    [CreateAssetMenu(fileName = "O_UpgradeCatalog", menuName = "Universes/Old/Upgrade Catalog")]
    public class O_UpgradeCatalog : ScriptableObject
    {
        public O_UpgradeDefinition[] upgrades;
    }
}
