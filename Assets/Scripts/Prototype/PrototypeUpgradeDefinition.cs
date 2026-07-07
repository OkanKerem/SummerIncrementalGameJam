using UnityEngine;

namespace Universes.Prototype
{
    public enum PrototypeUpgradeStatRequirementType
    {
        StarsCreated,
        ActiveStars,
        PlanetsCreated,
        LifePlanetsReached,
        SupernovaCount
    }

    [System.Serializable]
    public class PrototypeUpgradeLevelRequirement
    {
        public PrototypeUpgradeType upgradeType;
        [Min(1)] public int requiredLevel = 1;

        public string GetDescription() =>
            $"{PrototypeUpgradeUtility.GetDisplayName(upgradeType)} Level {requiredLevel}";
    }

    [System.Serializable]
    public class PrototypeUpgradeStatRequirement
    {
        public PrototypeUpgradeStatRequirementType statType;
        [Min(1)] public int requiredValue = 1;

        public bool IsMet(PrototypeGameController controller)
        {
            if (controller == null)
                return false;

            return GetCurrentValue(controller) >= requiredValue;
        }

        public string GetDescription() => statType switch
        {
            PrototypeUpgradeStatRequirementType.StarsCreated => $"{requiredValue} stars created",
            PrototypeUpgradeStatRequirementType.ActiveStars => $"{requiredValue} active stars",
            PrototypeUpgradeStatRequirementType.PlanetsCreated => $"{requiredValue} planets created",
            PrototypeUpgradeStatRequirementType.LifePlanetsReached => $"{requiredValue} planet with life",
            PrototypeUpgradeStatRequirementType.SupernovaCount => $"{requiredValue} supernova",
            _ => $"{statType} {requiredValue}"
        };

        private int GetCurrentValue(PrototypeGameController controller) => statType switch
        {
            PrototypeUpgradeStatRequirementType.StarsCreated => controller.RunStats.StarsCreated,
            PrototypeUpgradeStatRequirementType.ActiveStars => controller.ActiveStarCount,
            PrototypeUpgradeStatRequirementType.PlanetsCreated => controller.RunStats.PlanetsCreated,
            PrototypeUpgradeStatRequirementType.LifePlanetsReached => controller.RunStats.LifePlanetsReached,
            PrototypeUpgradeStatRequirementType.SupernovaCount => controller.RunStats.SupernovaCount,
            _ => 0
        };
    }

    [CreateAssetMenu(fileName = "PrototypeUpgrade", menuName = "Universes/Prototype Upgrade")]
    public class PrototypeUpgradeDefinition : ScriptableObject
    {
        public PrototypeUpgradeType upgradeType;
        public string displayName = "Upgrade";
        [TextArea] public string description;
        public double baseCost = 15;
        public double costScale = 1.55;
        [Tooltip("0 = unlimited levels. 1 = one-time purchase.")]
        public int maxLevel;
        public PrototypeUpgradeLevelRequirement[] upgradePrerequisites = System.Array.Empty<PrototypeUpgradeLevelRequirement>();
        public PrototypeUpgradeStatRequirement[] statPrerequisites = System.Array.Empty<PrototypeUpgradeStatRequirement>();

        public double GetCost(int currentLevel) =>
            baseCost * System.Math.Pow(costScale, currentLevel);

        public bool ArePrerequisitesMet(PrototypeGameController controller, out string requirementText)
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

    public static class PrototypeUpgradeUtility
    {
        public static string GetDisplayName(PrototypeUpgradeType type) => type switch
        {
            PrototypeUpgradeType.ClickPower => "Click Power",
            PrototypeUpgradeType.PassiveProduction => "Passive Star Production",
            PrototypeUpgradeType.StarStability => "Star Stability",
            PrototypeUpgradeType.SupernovaBonus => "Supernova Bonus",
            PrototypeUpgradeType.ClickCollectRadius => "Click Collect",
            PrototypeUpgradeType.MaxPlanetCount => "Max Planet Count",
            PrototypeUpgradeType.AutoPlanetFormation => "Auto Planet Formation",
            PrototypeUpgradeType.PlanetDnaChance => "Planet DNA Chance",
            PrototypeUpgradeType.PlanetClickValue => "Planet Click Value",
            PrototypeUpgradeType.HabitablePlanetChance => "Habitable Planet Chance",
            PrototypeUpgradeType.ExpandUniverse => "Expand Universe",
            PrototypeUpgradeType.MaxStarCount => "Max Star Count",
            PrototypeUpgradeType.AdvancedStarStability => "Advanced Star Stability",
            _ => type.ToString()
        };
    }
}
