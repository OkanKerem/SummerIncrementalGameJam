using UnityEngine;

namespace Universes.Prototype
{
    [CreateAssetMenu(fileName = "SingleStarBalance", menuName = "Universes/Prototype Single Star Balance")]
    public class PrototypeSingleStarBalance : ScriptableObject
    {
        [Header("Star")]
        [Min(1)] public int maxStarHealth = 100;
        [Min(1)] public int agePerClick = 1;
        [Min(0)] public int agePerPassiveTick = 1;
        [Min(0.1f)] public float passiveTickInterval = 1f;
        [Min(0)] public int baseClickReward = 3;
        [Min(0)] public int basePassivePerSecond = 1;

        [Header("Planets")]
        [Min(0)] public double createPlanetCost = 30;
        [Min(1)] public int baseMaxPlanets = 2;
        [Min(0.1f)] public float baseOrbitRadius = 1.4f;
        [Min(0.05f)] public float orbitRadiusStep = 0.55f;
        [Min(1f)] public float orbitSpeed = 28f;
        [Min(0.1f)] public float basePlanetClickDamage = 8f;
        [Min(0f)] public float planetSpawnDelay = 0.45f;

        [Header("Auto Formation")]
        [Min(0.1f)] public float autoFormationCheckInterval = 3f;
        [Range(0f, 1f)] public float baseAutoFormationChance = 0.01f;
        [Range(0f, 1f)] public float autoFormationChancePerLevel = 0.008f;

        [Header("Life & Civilization")]
        [Min(0.1f)] public float civilizationTickInterval = 2.5f;
        [Range(0f, 1f)] public float lifeProgressBase = 0.08f;
        [Range(0f, 1f)] public float baseHabitableRollBonus = 0.05f;
        [Range(0f, 1f)] public float habitableChancePerLevel = 0.06f;

        [Header("DNA")]
        [Min(0.1f)] public float dnaTickInterval = 1f;
        [Range(0f, 1f)] public float planetDnaChancePerLevel = 0.012f;
        [Min(0f)] public float planetClickValuePerLevel = 1.5f;

        [Header("Balance Configs")]
        public PrototypeSpeciesBalanceConfig species = new();
        public PrototypePlanetBalanceConfig planets = new();
        public PrototypeCivilizationBalanceConfig civilization = new();
        public PrototypeUpgradeBalanceConfig upgrades = new();
        public PrototypeMultiStarBalanceConfig multiStar = new();

        private void OnEnable() => EnsureNestedConfigs();

        public void EnsureNestedConfigs()
        {
            species ??= new PrototypeSpeciesBalanceConfig();
            planets ??= new PrototypePlanetBalanceConfig();
            civilization ??= new PrototypeCivilizationBalanceConfig();
            upgrades ??= new PrototypeUpgradeBalanceConfig();
            multiStar ??= new PrototypeMultiStarBalanceConfig();
        }

        public static PrototypeSingleStarBalance CreateRuntimeDefault()
        {
            var balance = CreateInstance<PrototypeSingleStarBalance>();
            balance.name = "RuntimeSingleStarBalance";
            balance.EnsureNestedConfigs();
            return balance;
        }
    }
}
