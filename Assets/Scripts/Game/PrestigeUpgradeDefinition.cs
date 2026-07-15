using UnityEngine;

namespace Universes.Game
{
    [System.Serializable]
    public class PrestigeUpgradeRequirement
    {
        public PrestigeUpgradeType upgradeType;
        [Min(1)] public int requiredLevel = 1;

        public string GetDescription() =>
            $"{PrestigeUpgradeUtility.GetDisplayName(upgradeType)} Level {requiredLevel}";
    }

    [CreateAssetMenu(fileName = "PrestigeUpgrade", menuName = "Universes/Prestige Upgrade")]
    public class PrestigeUpgradeDefinition : ScriptableObject
    {
        public PrestigeUpgradeType upgradeType;
        public string displayName = "Prestige Upgrade";
        public Sprite iconSprite;
        [TextArea] public string description;
        public int maxLevel;
        public PrestigeUpgradeRequirement[] prerequisites =
            System.Array.Empty<PrestigeUpgradeRequirement>();

        public bool ArePrerequisitesMet(PrestigeState prestige, out string requirementText)
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

    public static class PrestigeUpgradeUtility
    {
        public static string GetDisplayName(PrestigeUpgradeType type) => type switch
        {
            PrestigeUpgradeType.StrongerBigBang => "Stronger Big Bang",
            PrestigeUpgradeType.StablePhysics => "Stable Physics",
            PrestigeUpgradeType.LongerStarLifespan => "Longer Star Lifespan",
            PrestigeUpgradeType.SupernovaMemory => "Supernova Memory",
            PrestigeUpgradeType.BlackHoleMemory => "Black Hole Memory",
            PrestigeUpgradeType.CosmicEfficiency => "Cosmic Efficiency",
            PrestigeUpgradeType.ParticleEvolution => "Particle Evolution",
            PrestigeUpgradeType.ParallelEcho => "Parallel Echo",
            PrestigeUpgradeType.PlanetLongevity => "Planet Longevity",
            PrestigeUpgradeType.LifeGenesis => "Life Genesis",
            PrestigeUpgradeType.PlanetaryDetonation => "Planetary Detonation",
            PrestigeUpgradeType.PlanetaryLegacy => "Planetary Legacy",
            PrestigeUpgradeType.CivilizationAcceleration => "Civilization Acceleration",
            _ => type.ToString()
        };
    }
}
