using UnityEngine;

namespace Universes.Core
{
    [CreateAssetMenu(fileName = "GameBalance", menuName = "Universes/Game Balance")]
    public class GameBalance : ScriptableObject
    {
        [Header("Starting Run")]
        public int baseStartingStars = 1;
        public double baseStartingStardust = 10;
        public double baseBigBangStardust = 10;

        [Header("Stars")]
        public double createStarBaseCost = 25;
        public double createStarCostScale = 1.15;
        public double basePassiveStardustPerSecond = 1;
        public double clickStardustYield = 5;
        public double clickAgePenalty = 2;
        public double baseAgeRatePerSecond = 0.833;
        public float starSpawnRadiusMin = 1.5f;
        public float starSpawnRadiusMax = 6f;

        [Header("Star Stage Multipliers")]
        public double yellowProductionMultiplier = 1;
        public double orangeProductionMultiplier = 1.1;
        public double redGiantProductionMultiplier = 1.5;
        public double supernovaProductionMultiplier = 0.5;

        [Header("Supernova")]
        public double supernovaStardustReward = 100;
        public float blackHoleSeedChance = 0.15f;
        public double blackHoleSeedDnaBonus = 25;

        [Header("Planets")]
        public double createPlanetBaseCost = 100;
        public int maxPlanetsPerStar = 3;
        public double planetProductionBonus = 0.25;
        public double lifePlanetProductionBonus = 0.5;
        public double lifeChanceYellowPerTick = 0.0005;
        public double lifeChanceOrangePerTick = 0.001;

        [Header("Entropy")]
        public double baseEntropyRatePerSecond = 0.05;
        public double entropyPerStar = 0.01;
        public double entropyPerPlanet = 0.02;
        public double entropyPerLifePlanet = 0.03;
        public double entropySlowdownCost = 50;
        public double entropySlowdownAmount = 2;
        public float earlyCollapseEntropyThreshold = 50f;
        public float earlyCollapsePenalty = 0.5f;

        [Header("DNA Formula")]
        public double dnaK1StardustSqrt = 0.5;
        public double dnaK2Stars = 5;
        public double dnaK3Planets = 10;
        public double dnaK4LifePlanets = 25;
        public double dnaK5Supernovas = 15;
        public double dnaK6SurvivalTime = 0.1;
        public double dnaK7BlackHoleSeeds = 25;

        [Header("Upgrades")]
        public double upgradeBaseCost = 10;
        public double upgradeCostScale = 1.75;
    }
}
