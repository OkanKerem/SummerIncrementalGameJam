namespace Universes.Game
{
    public enum UpgradeType
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
        ExpandUniverse,
        MaxStarCount,
        AdvancedStarStability,
        ClickPowerPercent,
        PlanetPassiveProduction,
        StarPlanetClickValue,
        StarPassiveProductionPercent,
        CollisionDnaProduction,
        EntropyReduction,
        SpeciesDnaProduction
    }

    public class Upgrades
    {
        public int ClickPowerLevel { get; private set; }
        public int ClickPowerPercentLevel { get; private set; }
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
        public int MaxStarCountLevel { get; private set; }
        public int AdvancedStarStabilityLevel { get; private set; }
        public int PlanetPassiveProductionLevel { get; private set; }
        public int StarPlanetClickValueLevel { get; private set; }
        public int StarPassiveProductionPercentLevel { get; private set; }
        public int CollisionDnaProductionLevel { get; private set; }
        public int EntropyReductionLevel { get; private set; }
        public int SpeciesDnaProductionLevel { get; private set; }

        public bool IsUniverseExpanded => UniverseExpandedLevel > 0;

        public int GetLevel(UpgradeDefinition definition) =>
            definition != null ? GetLevel(definition.upgradeType) : 0;

        public int GetLevel(UpgradeType type) =>
            type switch
            {
                UpgradeType.ClickPower => ClickPowerLevel,
                UpgradeType.ClickPowerPercent => ClickPowerPercentLevel,
                UpgradeType.PassiveProduction => PassiveProductionLevel,
                UpgradeType.StarStability => StarStabilityLevel,
                UpgradeType.SupernovaBonus => SupernovaBonusLevel,
                UpgradeType.ClickCollectRadius => ClickCollectRadiusLevel,
                UpgradeType.MaxPlanetCount => MaxPlanetCountLevel,
                UpgradeType.AutoPlanetFormation => AutoPlanetFormationLevel,
                UpgradeType.PlanetDnaChance => PlanetDnaChanceLevel,
                UpgradeType.PlanetClickValue => PlanetClickValueLevel,
                UpgradeType.HabitablePlanetChance => HabitablePlanetChanceLevel,
                UpgradeType.ExpandUniverse => UniverseExpandedLevel,
                UpgradeType.MaxStarCount => MaxStarCountLevel,
                UpgradeType.AdvancedStarStability => AdvancedStarStabilityLevel,
                UpgradeType.PlanetPassiveProduction => PlanetPassiveProductionLevel,
                UpgradeType.StarPlanetClickValue => StarPlanetClickValueLevel,
                UpgradeType.StarPassiveProductionPercent => StarPassiveProductionPercentLevel,
                UpgradeType.CollisionDnaProduction => CollisionDnaProductionLevel,
                UpgradeType.EntropyReduction => EntropyReductionLevel,
                UpgradeType.SpeciesDnaProduction => SpeciesDnaProductionLevel,
                _ => 0
            };

        public double GetCost(UpgradeDefinition definition) =>
            definition != null ? definition.GetCost(GetLevel(definition)) : double.MaxValue;

        public int GetClickPowerBonus(UpgradeBalanceConfig balance)
        {
            balance ??= new UpgradeBalanceConfig();
            return GetCumulativeLevelBonus(ClickPowerLevel, balance.clickPowerBaseIncrement);
        }

        public int GetPassiveProductionBonus(UpgradeBalanceConfig balance)
        {
            balance ??= new UpgradeBalanceConfig();
            return GetCumulativeLevelBonus(PassiveProductionLevel, balance.passiveProductionBaseIncrement);
        }

        public bool TryPurchase(UpgradeDefinition definition, ref double stardust)
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
                case UpgradeType.ClickPower: ClickPowerLevel++; break;
                case UpgradeType.ClickPowerPercent: ClickPowerPercentLevel++; break;
                case UpgradeType.PassiveProduction: PassiveProductionLevel++; break;
                case UpgradeType.StarStability: StarStabilityLevel++; break;
                case UpgradeType.SupernovaBonus: SupernovaBonusLevel++; break;
                case UpgradeType.ClickCollectRadius: ClickCollectRadiusLevel++; break;
                case UpgradeType.MaxPlanetCount: MaxPlanetCountLevel++; break;
                case UpgradeType.AutoPlanetFormation: AutoPlanetFormationLevel++; break;
                case UpgradeType.PlanetDnaChance: PlanetDnaChanceLevel++; break;
                case UpgradeType.PlanetClickValue: PlanetClickValueLevel++; break;
                case UpgradeType.HabitablePlanetChance: HabitablePlanetChanceLevel++; break;
                case UpgradeType.ExpandUniverse: UniverseExpandedLevel++; break;
                case UpgradeType.MaxStarCount: MaxStarCountLevel++; break;
                case UpgradeType.AdvancedStarStability: AdvancedStarStabilityLevel++; break;
                case UpgradeType.PlanetPassiveProduction: PlanetPassiveProductionLevel++; break;
                case UpgradeType.StarPlanetClickValue: StarPlanetClickValueLevel++; break;
                case UpgradeType.StarPassiveProductionPercent: StarPassiveProductionPercentLevel++; break;
                case UpgradeType.CollisionDnaProduction: CollisionDnaProductionLevel++; break;
                case UpgradeType.EntropyReduction: EntropyReductionLevel++; break;
                case UpgradeType.SpeciesDnaProduction: SpeciesDnaProductionLevel++; break;
            }

            return true;
        }

        public float GetAgeGainMultiplier(UpgradeBalanceConfig balance) =>
            1f / (1f +
                  StarStabilityLevel * (balance ?? new UpgradeBalanceConfig()).starStabilityAgeGainReductionPerLevel +
                  AdvancedStarStabilityLevel * (balance ?? new UpgradeBalanceConfig()).advancedStarStabilityAgeGainReductionPerLevel);

        public float GetClickCollectRadius(UpgradeBalanceConfig balance)
        {
            balance ??= new UpgradeBalanceConfig();
            return balance.baseClickCollectRadius + ClickCollectRadiusLevel * balance.clickCollectRadiusPerLevel;
        }

        private static int GetCumulativeLevelBonus(int level, int baseIncrement)
        {
            if (level <= 0 || baseIncrement <= 0)
                return 0;

            return level * (level + 1) / 2 * baseIncrement;
        }

        public void Reset()
        {
            ClickPowerLevel = 0;
            ClickPowerPercentLevel = 0;
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
            MaxStarCountLevel = 0;
            AdvancedStarStabilityLevel = 0;
            PlanetPassiveProductionLevel = 0;
            StarPlanetClickValueLevel = 0;
            StarPassiveProductionPercentLevel = 0;
            CollisionDnaProductionLevel = 0;
            EntropyReductionLevel = 0;
            SpeciesDnaProductionLevel = 0;
        }

        public void SetUniverseExpanded(bool expanded) =>
            UniverseExpandedLevel = expanded ? 1 : 0;
    }
}
