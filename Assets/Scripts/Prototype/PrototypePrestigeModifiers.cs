namespace Universes.Prototype
{
    public static class PrototypePrestigeModifiers
    {
        public static int GetUpgradeCost(int currentLevel) =>
            (currentLevel + 1) * (currentLevel + 2) / 2;

        public static double GetStartingStardust(PrototypePrestigeState prestige)
        {
            var level = prestige.GetLevel(PrototypePrestigeUpgradeType.StrongerBigBang);
            return level switch
            {
                0 => 0,
                1 => 10,
                2 => 25,
                3 => 50,
                _ => 50 + (level - 3) * 25
            };
        }

        public static float GetEntropyMultiplier(PrototypePrestigeState prestige)
        {
            var level = prestige.GetLevel(PrototypePrestigeUpgradeType.StablePhysics);
            return 1f / (1f + level * PrototypePrestigeBalance.StablePhysicsEntropyReductionPerLevel);
        }

        public static float GetAgeGainMultiplier(PrototypePrestigeState prestige)
        {
            var level = prestige.GetLevel(PrototypePrestigeUpgradeType.LongerStarLifespan);
            return 1f / (1f + level * PrototypePrestigeBalance.LongerLifespanAgeReductionPerLevel);
        }

        public static float GetProductionMultiplier(PrototypePrestigeState prestige)
        {
            var level = prestige.GetLevel(PrototypePrestigeUpgradeType.CosmicEfficiency);
            return 1f + level * PrototypePrestigeBalance.CosmicEfficiencyPerLevel;
        }

        public static float GetSupernovaDnaChanceBonus(PrototypePrestigeState prestige)
        {
            var level = prestige.GetLevel(PrototypePrestigeUpgradeType.SupernovaMemory);
            return level * PrototypePrestigeBalance.SupernovaMemoryDnaBonusPerLevel;
        }

        public static float GetBlackHoleDnaMultiplier(PrototypePrestigeState prestige)
        {
            var level = prestige.GetLevel(PrototypePrestigeUpgradeType.BlackHoleMemory);
            return 1f + level * PrototypePrestigeBalance.BlackHoleMemoryMultiplierPerLevel;
        }

        public static float GetDoubleStardustChance(PrototypePrestigeState prestige)
        {
            var level = prestige.GetLevel(PrototypePrestigeUpgradeType.ParticleEvolution);
            return level * PrototypePrestigeBalance.DoubleStardustChancePerLevel;
        }

        public static double GetParallelEchoPerSecond(PrototypePrestigeState prestige)
        {
            var level = prestige.GetLevel(PrototypePrestigeUpgradeType.ParallelEcho);
            if (level <= 0 || prestige.TotalCollapses <= 0)
                return 0;

            return prestige.TotalCollapses * level * PrototypePrestigeBalance.ParallelEchoPerCollapsePerLevel;
        }

        public static string DescribeEffect(PrototypePrestigeUpgradeDefinition definition, int level)
        {
            if (definition == null)
                return string.Empty;

            return definition.upgradeType switch
            {
                PrototypePrestigeUpgradeType.StrongerBigBang =>
                    $"New universes start with {GetStartingStardustForLevel(level):0} Stardust",
                PrototypePrestigeUpgradeType.StablePhysics =>
                    $"Entropy gain x{GetEntropyMultiplierForLevel(level):0.00}",
                PrototypePrestigeUpgradeType.LongerStarLifespan =>
                    $"Star aging x{GetAgeGainMultiplierForLevel(level):0.00}",
                PrototypePrestigeUpgradeType.SupernovaMemory =>
                    $"+{level * PrototypePrestigeBalance.SupernovaMemoryDnaBonusPerLevel * 100f:0}% Supernova DNA chance",
                PrototypePrestigeUpgradeType.BlackHoleMemory =>
                    $"Black Hole DNA x{1f + level * PrototypePrestigeBalance.BlackHoleMemoryMultiplierPerLevel:0.00}",
                PrototypePrestigeUpgradeType.CosmicEfficiency =>
                    $"Stardust production x{1f + level * PrototypePrestigeBalance.CosmicEfficiencyPerLevel:0.00}",
                PrototypePrestigeUpgradeType.ParticleEvolution =>
                    $"{level * PrototypePrestigeBalance.DoubleStardustChancePerLevel * 100f:0}% chance for 2x Stardust",
                PrototypePrestigeUpgradeType.ParallelEcho =>
                    $"+{GetParallelEchoPerSecondForLevel(level, 1):0.00} Stardust/s per past collapse",
                _ => definition.description
            };
        }

        private static double GetStartingStardustForLevel(int level) =>
            level switch
            {
                0 => 0,
                1 => 10,
                2 => 25,
                3 => 50,
                _ => 50 + (level - 3) * 25
            };

        private static float GetEntropyMultiplierForLevel(int level) =>
            1f / (1f + level * PrototypePrestigeBalance.StablePhysicsEntropyReductionPerLevel);

        private static float GetAgeGainMultiplierForLevel(int level) =>
            1f / (1f + level * PrototypePrestigeBalance.LongerLifespanAgeReductionPerLevel);

        private static double GetParallelEchoPerSecondForLevel(int level, int collapses) =>
            collapses * level * PrototypePrestigeBalance.ParallelEchoPerCollapsePerLevel;
    }
}
