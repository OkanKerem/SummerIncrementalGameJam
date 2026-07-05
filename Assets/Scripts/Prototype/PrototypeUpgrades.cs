namespace Universes.Prototype
{
    public enum PrototypeUpgradeType
    {
        ClickPower,
        PassiveProduction,
        StarStability,
        SupernovaBonus,
        ClickCollectRadius
    }

    public class PrototypeUpgrades
    {
        public int ClickPowerLevel { get; private set; }
        public int PassiveProductionLevel { get; private set; }
        public int StarStabilityLevel { get; private set; }
        public int SupernovaBonusLevel { get; private set; }
        public int ClickCollectRadiusLevel { get; private set; }

        public const int SupernovaBonusPerLevel = 15;
        public const float BaseClickCollectRadius = 1.1f;
        public const float ClickCollectRadiusPerLevel = 0.45f;

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
                _ => 0
            };

        public double GetCost(PrototypeUpgradeDefinition definition) =>
            definition != null ? definition.GetCost(GetLevel(definition)) : double.MaxValue;

        public bool TryPurchase(PrototypeUpgradeDefinition definition, ref double stardust)
        {
            if (definition == null)
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
            }

            return true;
        }

        public float GetAgeGainMultiplier() =>
            1f / (1f + StarStabilityLevel * 0.25f);

        public float GetClickCollectRadius() =>
            BaseClickCollectRadius + ClickCollectRadiusLevel * ClickCollectRadiusPerLevel;

        public void Reset()
        {
            ClickPowerLevel = 0;
            PassiveProductionLevel = 0;
            StarStabilityLevel = 0;
            SupernovaBonusLevel = 0;
            ClickCollectRadiusLevel = 0;
        }
    }
}
