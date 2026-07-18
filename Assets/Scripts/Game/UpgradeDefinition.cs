using UnityEngine;

namespace Universes.Game
{
    public enum UpgradeStatRequirementType
    {
        StarsCreated,
        ActiveStars,
        PlanetsCreated,
        LifePlanetsReached,
        SupernovaCount
    }

    [System.Serializable]
    public class UpgradeLevelRequirement
    {
        public UpgradeType upgradeType;
        [Min(1)] public int requiredLevel = 1;

        public string GetDescription() =>
            $"{UpgradeUtility.GetDisplayName(upgradeType)} Level {requiredLevel}";
    }

    [System.Serializable]
    public class UpgradeStatRequirement
    {
        public UpgradeStatRequirementType statType;
        [Min(1)] public int requiredValue = 1;

        public bool IsMet(GameController controller)
        {
            if (controller == null)
                return false;

            return GetCurrentValue(controller) >= requiredValue;
        }

        public string GetDescription() => statType switch
        {
            UpgradeStatRequirementType.StarsCreated => $"{requiredValue} stars created",
            UpgradeStatRequirementType.ActiveStars => $"{requiredValue} active stars",
            UpgradeStatRequirementType.PlanetsCreated => $"{requiredValue} planets created",
            UpgradeStatRequirementType.LifePlanetsReached => $"{requiredValue} planet with life",
            UpgradeStatRequirementType.SupernovaCount => $"{requiredValue} supernova",
            _ => $"{statType} {requiredValue}"
        };

        private int GetCurrentValue(GameController controller) => statType switch
        {
            UpgradeStatRequirementType.StarsCreated => controller.RunStats.StarsCreated,
            UpgradeStatRequirementType.ActiveStars => controller.ActiveStarCount,
            UpgradeStatRequirementType.PlanetsCreated => controller.RunStats.PlanetsCreated,
            UpgradeStatRequirementType.LifePlanetsReached => controller.RunStats.LifePlanetsReached,
            UpgradeStatRequirementType.SupernovaCount => controller.RunStats.SupernovaCount,
            _ => 0
        };
    }

    [CreateAssetMenu(fileName = "Upgrade", menuName = "Universes/Upgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        public UpgradeType upgradeType;
        [Min(1)] public int upgradeTier = 1;
        public string displayName = "Upgrade";
        public Sprite iconSprite;
        [TextArea] public string description;
        public double baseCost = 15;
        public double costScale = 1.55;
        [Tooltip("0 = unlimited levels. 1 = one-time purchase.")]
        public int maxLevel;
        public UpgradeLevelRequirement[] upgradePrerequisites = System.Array.Empty<UpgradeLevelRequirement>();
        public UpgradeStatRequirement[] statPrerequisites = System.Array.Empty<UpgradeStatRequirement>();

        public double GetCost(int currentLevel) =>
            baseCost * System.Math.Pow(costScale, currentLevel);

        public bool ArePrerequisitesMet(GameController controller, out string requirementText)
        {
            requirementText = string.Empty;
            if (controller == null)
                return false;

            var unmet = new System.Collections.Generic.List<string>();

            if (upgradePrerequisites != null)
            {
                foreach (var requirement in upgradePrerequisites)
                {
                    if (requirement == null)
                        continue;

                    if (controller.Upgrades.GetLevel(requirement.upgradeType) < requirement.requiredLevel)
                        unmet.Add(requirement.GetDescription());
                }
            }

            if (statPrerequisites != null)
            {
                foreach (var requirement in statPrerequisites)
                {
                    if (requirement == null)
                        continue;

                    if (!requirement.IsMet(controller))
                        unmet.Add(requirement.GetDescription());
                }
            }

            requirementText = string.Join(", ", unmet);
            return unmet.Count == 0;
        }
    }

    public static class UpgradeUtility
    {
        public static string GetDisplayName(UpgradeType type) => type switch
        {
            UpgradeType.ClickPower => "Click Power",
            UpgradeType.ClickPowerPercent => "Click Power Percent",
            UpgradeType.PassiveProduction => "Passive Star Production",
            UpgradeType.StarStability => "Star Stability",
            UpgradeType.SupernovaBonus => "Supernova Bonus",
            UpgradeType.ClickCollectRadius => "Click Collect",
            UpgradeType.MaxPlanetCount => "Max Planet Count",
            UpgradeType.AutoPlanetFormation => "Auto Planet Formation",
            UpgradeType.PlanetDnaChance => "Planet DNA Chance",
            UpgradeType.PlanetClickValue => "Planet Click Value",
            UpgradeType.HabitablePlanetChance => "Habitable Planet Chance",
            UpgradeType.ExpandUniverse => "Expand Universe",
            UpgradeType.ExpandCosmic => "Expand Cosmos",
            UpgradeType.MaxStarCount => "Max Star Count",
            UpgradeType.AdvancedStarStability => "Advanced Star Stability",
            UpgradeType.PlanetPassiveProduction => "Planet Passive Production Percent",
            UpgradeType.StarPlanetClickValue => "Planet-Powered Clicks",
            UpgradeType.StarPassiveProductionPercent => "Passive Production Percent",
            UpgradeType.CollisionDnaProduction => "Collision DNA Production",
            UpgradeType.EntropyReduction => "Entropy Reduction",
            UpgradeType.SpeciesDnaProduction => "Species DNA Production",
            UpgradeType.CollisionAttraction => "Collision Attraction",
            UpgradeType.BlackHoleStabilization => "Black Hole Stabilization",
            UpgradeType.BlackHoleMemory => "Black Hole Memory",
            UpgradeType.SpaceAgeDna => "Space Age DNA",
            UpgradeType.SpaceAgeProgression => "Space Age Progression",
            UpgradeType.CosmicEventDna => "Cosmic Event DNA",
            UpgradeType.EntropyEqualization => "Entropy Equalization",
            UpgradeType.OrbitalDna => "Orbital DNA",
            UpgradeType.AutoStarFormation => "Auto Star Formation",
            _ => type.ToString()
        };
    }
}
