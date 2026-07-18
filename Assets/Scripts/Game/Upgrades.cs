using UnityEngine;

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
        SpeciesDnaProduction,
        ExpandCosmic,
        CollisionAttraction,
        BlackHoleStabilization,
        BlackHoleMemory,
        SpaceAgeDna,
        SpaceAgeProgression,
        CosmicEventDna,
        EntropyEqualization,
        OrbitalDna,
        AutoStarFormation
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
        public int CosmicExpandedLevel { get; private set; }
        public int MaxStarCountLevel { get; private set; }
        public int AdvancedStarStabilityLevel { get; private set; }
        public int PlanetPassiveProductionLevel { get; private set; }
        public int StarPlanetClickValueLevel { get; private set; }
        public int StarPassiveProductionPercentLevel { get; private set; }
        public int CollisionDnaProductionLevel { get; private set; }
        public int EntropyReductionLevel { get; private set; }
        public int SpeciesDnaProductionLevel { get; private set; }
        public int CollisionAttractionLevel { get; private set; }
        public int BlackHoleStabilizationLevel { get; private set; }
        public int BlackHoleMemoryLevel { get; private set; }
        public int SpaceAgeDnaLevel { get; private set; }
        public int SpaceAgeProgressionLevel { get; private set; }
        public int CosmicEventDnaLevel { get; private set; }
        public int EntropyEqualizationLevel { get; private set; }
        public int OrbitalDnaLevel { get; private set; }
        public int AutoStarFormationLevel { get; private set; }

        public bool IsUniverseExpanded => UniverseExpandedLevel > 0;
        public bool IsCosmicExpanded => CosmicExpandedLevel > 0;
        public bool IsEntropyEqualized => EntropyEqualizationLevel > 0;

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
                UpgradeType.ExpandCosmic => CosmicExpandedLevel,
                UpgradeType.MaxStarCount => MaxStarCountLevel,
                UpgradeType.AdvancedStarStability => AdvancedStarStabilityLevel,
                UpgradeType.PlanetPassiveProduction => PlanetPassiveProductionLevel,
                UpgradeType.StarPlanetClickValue => StarPlanetClickValueLevel,
                UpgradeType.StarPassiveProductionPercent => StarPassiveProductionPercentLevel,
                UpgradeType.CollisionDnaProduction => CollisionDnaProductionLevel,
                UpgradeType.EntropyReduction => EntropyReductionLevel,
                UpgradeType.SpeciesDnaProduction => SpeciesDnaProductionLevel,
                UpgradeType.CollisionAttraction => CollisionAttractionLevel,
                UpgradeType.BlackHoleStabilization => BlackHoleStabilizationLevel,
                UpgradeType.BlackHoleMemory => BlackHoleMemoryLevel,
                UpgradeType.SpaceAgeDna => SpaceAgeDnaLevel,
                UpgradeType.SpaceAgeProgression => SpaceAgeProgressionLevel,
                UpgradeType.CosmicEventDna => CosmicEventDnaLevel,
                UpgradeType.EntropyEqualization => EntropyEqualizationLevel,
                UpgradeType.OrbitalDna => OrbitalDnaLevel,
                UpgradeType.AutoStarFormation => AutoStarFormationLevel,
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
                case UpgradeType.ExpandCosmic: CosmicExpandedLevel++; break;
                case UpgradeType.MaxStarCount: MaxStarCountLevel++; break;
                case UpgradeType.AdvancedStarStability: AdvancedStarStabilityLevel++; break;
                case UpgradeType.PlanetPassiveProduction: PlanetPassiveProductionLevel++; break;
                case UpgradeType.StarPlanetClickValue: StarPlanetClickValueLevel++; break;
                case UpgradeType.StarPassiveProductionPercent: StarPassiveProductionPercentLevel++; break;
                case UpgradeType.CollisionDnaProduction: CollisionDnaProductionLevel++; break;
                case UpgradeType.EntropyReduction: EntropyReductionLevel++; break;
                case UpgradeType.SpeciesDnaProduction: SpeciesDnaProductionLevel++; break;
                case UpgradeType.CollisionAttraction: CollisionAttractionLevel++; break;
                case UpgradeType.BlackHoleStabilization: BlackHoleStabilizationLevel++; break;
                case UpgradeType.BlackHoleMemory: BlackHoleMemoryLevel++; break;
                case UpgradeType.SpaceAgeDna: SpaceAgeDnaLevel++; break;
                case UpgradeType.SpaceAgeProgression: SpaceAgeProgressionLevel++; break;
                case UpgradeType.CosmicEventDna: CosmicEventDnaLevel++; break;
                case UpgradeType.EntropyEqualization: EntropyEqualizationLevel++; break;
                case UpgradeType.OrbitalDna: OrbitalDnaLevel++; break;
                case UpgradeType.AutoStarFormation: AutoStarFormationLevel++; break;
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
            CosmicExpandedLevel = 0;
            MaxStarCountLevel = 0;
            AdvancedStarStabilityLevel = 0;
            PlanetPassiveProductionLevel = 0;
            StarPlanetClickValueLevel = 0;
            StarPassiveProductionPercentLevel = 0;
            CollisionDnaProductionLevel = 0;
            EntropyReductionLevel = 0;
            SpeciesDnaProductionLevel = 0;
            CollisionAttractionLevel = 0;
            BlackHoleStabilizationLevel = 0;
            BlackHoleMemoryLevel = 0;
            SpaceAgeDnaLevel = 0;
            SpaceAgeProgressionLevel = 0;
            CosmicEventDnaLevel = 0;
            EntropyEqualizationLevel = 0;
            OrbitalDnaLevel = 0;
            AutoStarFormationLevel = 0;
        }

        public void LoadFromSave(RunSaveData data)
        {
            if (data == null)
            {
                Reset();
                return;
            }

            ClickPowerLevel = Mathf.Max(0, data.clickPowerLevel);
            ClickPowerPercentLevel = Mathf.Max(0, data.clickPowerPercentLevel);
            PassiveProductionLevel = Mathf.Max(0, data.passiveProductionLevel);
            StarStabilityLevel = Mathf.Max(0, data.starStabilityLevel);
            SupernovaBonusLevel = Mathf.Max(0, data.supernovaBonusLevel);
            ClickCollectRadiusLevel = Mathf.Max(0, data.clickCollectRadiusLevel);
            MaxPlanetCountLevel = Mathf.Max(0, data.maxPlanetCountLevel);
            AutoPlanetFormationLevel = Mathf.Max(0, data.autoPlanetFormationLevel);
            PlanetDnaChanceLevel = Mathf.Max(0, data.planetDnaChanceLevel);
            PlanetClickValueLevel = Mathf.Max(0, data.planetClickValueLevel);
            HabitablePlanetChanceLevel = Mathf.Max(0, data.habitablePlanetChanceLevel);
            UniverseExpandedLevel = Mathf.Max(0, data.universeExpandedLevel);
            CosmicExpandedLevel = Mathf.Max(0, data.cosmicExpandedLevel);
            MaxStarCountLevel = Mathf.Max(0, data.maxStarCountLevel);
            AdvancedStarStabilityLevel = Mathf.Max(0, data.advancedStarStabilityLevel);
            PlanetPassiveProductionLevel = Mathf.Max(0, data.planetPassiveProductionLevel);
            StarPlanetClickValueLevel = Mathf.Max(0, data.starPlanetClickValueLevel);
            StarPassiveProductionPercentLevel = Mathf.Max(0, data.starPassiveProductionPercentLevel);
            CollisionDnaProductionLevel = Mathf.Max(0, data.collisionDnaProductionLevel);
            EntropyReductionLevel = Mathf.Max(0, data.entropyReductionLevel);
            SpeciesDnaProductionLevel = Mathf.Max(0, data.speciesDnaProductionLevel);
            CollisionAttractionLevel = Mathf.Max(0, data.collisionAttractionLevel);
            BlackHoleStabilizationLevel = Mathf.Max(0, data.blackHoleStabilizationLevel);
            BlackHoleMemoryLevel = Mathf.Max(0, data.blackHoleMemoryLevel);
            SpaceAgeDnaLevel = Mathf.Max(0, data.spaceAgeDnaLevel);
            SpaceAgeProgressionLevel = Mathf.Max(0, data.spaceAgeProgressionLevel);
            CosmicEventDnaLevel = Mathf.Max(0, data.cosmicEventDnaLevel);
            EntropyEqualizationLevel = Mathf.Max(0, data.entropyEqualizationLevel);
            OrbitalDnaLevel = Mathf.Max(0, data.orbitalDnaLevel);
            AutoStarFormationLevel = Mathf.Max(0, data.autoStarFormationLevel);
        }

        public void SetUniverseExpanded(bool expanded) =>
            UniverseExpandedLevel = expanded ? 1 : 0;
    }
}
