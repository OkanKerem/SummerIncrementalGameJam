namespace Universes.Prototype
{
    public enum PrototypeUpgradeType
    {
        ClickPower,
        PassiveProduction,
        StarStability,
        SupernovaBonus,
        ClickCollectRadius,
        MaxPlanetCount,
        AutoPlanetFormation,
        PlanetDnaChance,
        PlanetClickValue,
        HabitablePlanetChance,
        ExpandUniverse
    }

    public class PrototypeUpgrades
    {
        public int ClickPowerLevel { get; private set; }
        public int PassiveProductionLevel { get; private set; }
        public int StarStabilityLevel { get; private set; }
        public int SupernovaBonusLevel { get; private set; }
        public int ClickCollectRadiusLevel { get; private set; }
        public int MaxPlanetCountLevel { get; private set; }
        public int AutoPlanetFormationLevel { get; private set; }
        public int PlanetDnaChanceLevel { get; private set; }
        public int PlanetClickValueLevel { get; private set; }
        public int HabitablePlanetChanceLevel { get; private set; }
        public int UniverseExpandedLevel { get; private set; }

        public bool IsUniverseExpanded => UniverseExpandedLevel > 0;

        public int GetLevel(PrototypeUpgradeDefinition definition) =>
            definition != null ? GetLevel(definition.upgradeType) : 0;

        public int GetLevel(PrototypeUpgradeType type) =>
            type switch
            {
                PrototypeUpgradeType.ClickPower => ClickPowerLevel,
                PrototypeUpgradeType.PassiveProduction => PassiveProductionLevel,
                PrototypeUpgradeType.StarStability => StarStabilityLevel,
                PrototypeUpgradeType.SupernovaBonus => SupernovaBonusLevel,
                PrototypeUpgradeType.ClickCollectRadius => ClickCollectRadiusLevel,
                PrototypeUpgradeType.MaxPlanetCount => MaxPlanetCountLevel,
                PrototypeUpgradeType.AutoPlanetFormation => AutoPlanetFormationLevel,
                PrototypeUpgradeType.PlanetDnaChance => PlanetDnaChanceLevel,
                PrototypeUpgradeType.PlanetClickValue => PlanetClickValueLevel,
                PrototypeUpgradeType.HabitablePlanetChance => HabitablePlanetChanceLevel,
                PrototypeUpgradeType.ExpandUniverse => UniverseExpandedLevel,
                _ => 0
            };

        public double GetCost(PrototypeUpgradeDefinition definition) =>
            definition != null ? definition.GetCost(GetLevel(definition)) : double.MaxValue;

        public bool TryPurchase(PrototypeUpgradeDefinition definition, ref double stardust)
        {
            if (definition == null)
                return false;

            var currentLevel = GetLevel(definition);
            if (definition.maxLevel > 0 && currentLevel >= definition.maxLevel)
                return false;

            var cost = GetCost(definition);
            if (stardust < cost)
                return false;

            stardust -= cost;
            switch (definition.upgradeType)
            {
                case PrototypeUpgradeType.ClickPower: ClickPowerLevel++; break;
                case PrototypeUpgradeType.PassiveProduction: PassiveProductionLevel++; break;
                case PrototypeUpgradeType.StarStability: StarStabilityLevel++; break;
                case PrototypeUpgradeType.SupernovaBonus: SupernovaBonusLevel++; break;
                case PrototypeUpgradeType.ClickCollectRadius: ClickCollectRadiusLevel++; break;
                case PrototypeUpgradeType.MaxPlanetCount: MaxPlanetCountLevel++; break;
                case PrototypeUpgradeType.AutoPlanetFormation: AutoPlanetFormationLevel++; break;
                case PrototypeUpgradeType.PlanetDnaChance: PlanetDnaChanceLevel++; break;
                case PrototypeUpgradeType.PlanetClickValue: PlanetClickValueLevel++; break;
                case PrototypeUpgradeType.HabitablePlanetChance: HabitablePlanetChanceLevel++; break;
                case PrototypeUpgradeType.ExpandUniverse: UniverseExpandedLevel++; break;
            }

            return true;
        }

        public float GetAgeGainMultiplier(PrototypeUpgradeBalanceConfig balance) =>
            1f / (1f + StarStabilityLevel *
                (balance ?? new PrototypeUpgradeBalanceConfig()).starStabilityAgeGainReductionPerLevel);

        public float GetClickCollectRadius(PrototypeUpgradeBalanceConfig balance)
        {
            balance ??= new PrototypeUpgradeBalanceConfig();
            return balance.baseClickCollectRadius + ClickCollectRadiusLevel * balance.clickCollectRadiusPerLevel;
        }

        public void Reset()
        {
            ClickPowerLevel = 0;
            PassiveProductionLevel = 0;
            StarStabilityLevel = 0;
            SupernovaBonusLevel = 0;
            ClickCollectRadiusLevel = 0;
            MaxPlanetCountLevel = 0;
            AutoPlanetFormationLevel = 0;
            PlanetDnaChanceLevel = 0;
            PlanetClickValueLevel = 0;
            HabitablePlanetChanceLevel = 0;
            UniverseExpandedLevel = 0;
        }

        public void SetUniverseExpanded(bool expanded) =>
            UniverseExpandedLevel = expanded ? 1 : 0;
    }
}
