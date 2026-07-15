namespace Universes.Game
{
    public static class PrestigeModifiers
    {
        public static int GetUpgradeCost(int currentLevel) =>
            (currentLevel + 1) * (currentLevel + 2) / 2;

        public static double GetStartingStardust(PrestigeState prestige)
        {
            var level = prestige.GetLevel(PrestigeUpgradeType.StrongerBigBang);
            return level switch
            {
                0 => 0,
                1 => 10,
                2 => 25,
                3 => 50,
                _ => 50 + (level - 3) * 25
            };
        }

        public static float GetEntropyMultiplier(PrestigeState prestige)
        {
            var level = prestige.GetLevel(PrestigeUpgradeType.StablePhysics);
            return 1f / (1f + level * PrestigeBalance.StablePhysicsEntropyReductionPerLevel);
        }

        public static float GetAgeGainMultiplier(PrestigeState prestige)
        {
            var level = prestige.GetLevel(PrestigeUpgradeType.LongerStarLifespan);
            return 1f / (1f + level * PrestigeBalance.LongerLifespanAgeReductionPerLevel);
        }

        public static float GetProductionMultiplier(PrestigeState prestige)
        {
            var level = prestige.GetLevel(PrestigeUpgradeType.CosmicEfficiency);
            return 1f + level * PrestigeBalance.CosmicEfficiencyPerLevel;
        }

        public static float GetSupernovaDnaChanceBonus(PrestigeState prestige)
        {
            var level = prestige.GetLevel(PrestigeUpgradeType.SupernovaMemory);
            return level * PrestigeBalance.SupernovaMemoryDnaBonusPerLevel;
        }

        public static float GetBlackHoleDnaMultiplier(PrestigeState prestige)
        {
            var level = prestige.GetLevel(PrestigeUpgradeType.BlackHoleMemory);
            return 1f + level * PrestigeBalance.BlackHoleMemoryMultiplierPerLevel;
        }

        public static float GetDoubleStardustChance(PrestigeState prestige)
        {
            var level = prestige.GetLevel(PrestigeUpgradeType.ParticleEvolution);
            return level * PrestigeBalance.DoubleStardustChancePerLevel;
        }

        public static double GetParallelEchoPerSecond(PrestigeState prestige)
        {
            var level = prestige.GetLevel(PrestigeUpgradeType.ParallelEcho);
            if (level <= 0 || prestige.TotalCollapses <= 0)
                return 0;

            return prestige.TotalCollapses * level * PrestigeBalance.ParallelEchoPerCollapsePerLevel;
        }

        public static float GetPlanetLifetimeMultiplier(PrestigeState prestige) =>
            1f + prestige.GetLevel(PrestigeUpgradeType.PlanetLongevity) *
            PrestigeBalance.PlanetLifetimeMultiplierPerLevel;

        public static float GetLifeEmergenceChanceMultiplier(PrestigeState prestige) =>
            1f + prestige.GetLevel(PrestigeUpgradeType.LifeGenesis) *
            PrestigeBalance.LifeEmergenceChanceMultiplierPerLevel;

        public static float GetCivilizationProgressMultiplier(PrestigeState prestige) =>
            1f + prestige.GetLevel(PrestigeUpgradeType.CivilizationAcceleration) *
            PrestigeBalance.CivilizationProgressMultiplierPerLevel;

        public static float GetPlanetDestructionDnaPotential(PrestigeState prestige) =>
            prestige.GetLevel(PrestigeUpgradeType.PlanetaryLegacy) *
            PrestigeBalance.PlanetDestructionDnaPotentialPerLevel;

        public static float GetPlanetExplosionStardust(PrestigeState prestige) =>
            prestige.GetLevel(PrestigeUpgradeType.PlanetaryDetonation) *
            PrestigeBalance.PlanetExplosionStardustPerLevel;

        public static string DescribeEffect(PrestigeUpgradeDefinition definition, int level)
        {
            if (definition == null)
                return string.Empty;

            return definition.upgradeType switch
            {
                PrestigeUpgradeType.StrongerBigBang =>
                    $"New universes start with {GetStartingStardustForLevel(level):0} Stardust",
                PrestigeUpgradeType.StablePhysics =>
                    $"Entropy gain x{GetEntropyMultiplierForLevel(level):0.00}",
                PrestigeUpgradeType.LongerStarLifespan =>
                    $"Star aging x{GetAgeGainMultiplierForLevel(level):0.00}",
                PrestigeUpgradeType.SupernovaMemory =>
                    $"+{level * PrestigeBalance.SupernovaMemoryDnaBonusPerLevel * 100f:0}% Supernova DNA chance",
                PrestigeUpgradeType.BlackHoleMemory =>
                    $"Black Hole DNA x{1f + level * PrestigeBalance.BlackHoleMemoryMultiplierPerLevel:0.00}",
                PrestigeUpgradeType.CosmicEfficiency =>
                    $"Stardust production x{1f + level * PrestigeBalance.CosmicEfficiencyPerLevel:0.00}",
                PrestigeUpgradeType.ParticleEvolution =>
                    $"{level * PrestigeBalance.DoubleStardustChancePerLevel * 100f:0}% chance for 2x Stardust",
                PrestigeUpgradeType.ParallelEcho =>
                    $"+{GetParallelEchoPerSecondForLevel(level, 1):0.00} Stardust/s per past collapse",
                PrestigeUpgradeType.PlanetLongevity =>
                    $"Planet durability x{1f + level * PrestigeBalance.PlanetLifetimeMultiplierPerLevel:0.00}",
                PrestigeUpgradeType.LifeGenesis =>
                    $"Life emergence chance x{1f + level * PrestigeBalance.LifeEmergenceChanceMultiplierPerLevel:0.00}",
                PrestigeUpgradeType.PlanetaryDetonation =>
                    $"+{level * PrestigeBalance.PlanetExplosionStardustPerLevel:0} Stardust per destroyed planet",
                PrestigeUpgradeType.PlanetaryLegacy =>
                    $"+{level * PrestigeBalance.PlanetDestructionDnaPotentialPerLevel:0.##} DNA potential per destroyed planet",
                PrestigeUpgradeType.CivilizationAcceleration =>
                    $"Civilization progress x{1f + level * PrestigeBalance.CivilizationProgressMultiplierPerLevel:0.00}",
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
            1f / (1f + level * PrestigeBalance.StablePhysicsEntropyReductionPerLevel);

        private static float GetAgeGainMultiplierForLevel(int level) =>
            1f / (1f + level * PrestigeBalance.LongerLifespanAgeReductionPerLevel);

        private static double GetParallelEchoPerSecondForLevel(int level, int collapses) =>
            collapses * level * PrestigeBalance.ParallelEchoPerCollapsePerLevel;
    }
}
