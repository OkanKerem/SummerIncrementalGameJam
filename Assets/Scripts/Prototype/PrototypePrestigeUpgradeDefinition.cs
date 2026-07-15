using UnityEngine;

namespace Universes.Prototype
{
    [System.Serializable]
    public class PrototypePrestigeUpgradeRequirement
    {
        public PrototypePrestigeUpgradeType upgradeType;
        [Min(1)] public int requiredLevel = 1;

        public string GetDescription() =>
            $"{PrototypePrestigeUpgradeUtility.GetDisplayName(upgradeType)} Level {requiredLevel}";
    }

    [CreateAssetMenu(fileName = "PrototypePrestigeUpgrade", menuName = "Universes/Prototype Prestige Upgrade")]
    public class PrototypePrestigeUpgradeDefinition : ScriptableObject
    {
        public PrototypePrestigeUpgradeType upgradeType;
        public string displayName = "Prestige Upgrade";
        public Sprite iconSprite;
        [TextArea] public string description;
        public int maxLevel;
        public PrototypePrestigeUpgradeRequirement[] prerequisites =
            System.Array.Empty<PrototypePrestigeUpgradeRequirement>();

        public bool ArePrerequisitesMet(PrototypePrestigeState prestige, out string requirementText)
        {
            requirementText = string.Empty;
            if (prestige == null)
                return false;

            if (prerequisites == null || prerequisites.Length == 0)
                return true;

            var unmet = new System.Collections.Generic.List<string>();
            foreach (var requirement in prerequisites)
            {
                if (requirement == null)
                    continue;

                if (prestige.GetLevel(requirement.upgradeType) < requirement.requiredLevel)
                    unmet.Add(requirement.GetDescription());
            }

            requirementText = string.Join(", ", unmet);
            return unmet.Count == 0;
        }
    }

    public static class PrototypePrestigeUpgradeUtility
    {
        public static string GetDisplayName(PrototypePrestigeUpgradeType type) => type switch
        {
            PrototypePrestigeUpgradeType.StrongerBigBang => "Stronger Big Bang",
            PrototypePrestigeUpgradeType.StablePhysics => "Stable Physics",
            PrototypePrestigeUpgradeType.LongerStarLifespan => "Longer Star Lifespan",
            PrototypePrestigeUpgradeType.SupernovaMemory => "Supernova Memory",
            PrototypePrestigeUpgradeType.BlackHoleMemory => "Black Hole Memory",
            PrototypePrestigeUpgradeType.CosmicEfficiency => "Cosmic Efficiency",
            PrototypePrestigeUpgradeType.ParticleEvolution => "Particle Evolution",
            PrototypePrestigeUpgradeType.ParallelEcho => "Parallel Echo",
            _ => type.ToString()
        };
    }
}
