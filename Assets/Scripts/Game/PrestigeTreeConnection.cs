using UnityEngine;

namespace Universes.Game
{
    public class PrestigeTreeConnection : MonoBehaviour
    {
        [SerializeField] private PrestigeUpgradeType fromType;
        [SerializeField] private PrestigeUpgradeType toType;

        public PrestigeUpgradeType FromType => fromType;
        public PrestigeUpgradeType ToType => toType;

        public void Configure(PrestigeUpgradeType from, PrestigeUpgradeType to)
        {
            fromType = from;
            toType = to;
        }

        public void Refresh(PrestigeState prestige, PrestigeUpgradeDefinition fromDefinition,
            PrestigeUpgradeDefinition toDefinition)
        {
            gameObject.SetActive(PrestigeTreeVisibility.IsConnectionVisible(prestige, fromDefinition, toDefinition));
        }
    }
}
