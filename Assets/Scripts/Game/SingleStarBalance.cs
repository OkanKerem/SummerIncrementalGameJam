using UnityEngine;

namespace Universes.Game
{
    [CreateAssetMenu(fileName = "SingleStarBalance", menuName = "Universes/Single Star Balance")]
    public class SingleStarBalance : ScriptableObject
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
        [Min(0f)] public float createPlanetCostIncreasePercent = 15f;
        [Min(1)] public int baseMaxPlanets = 2;
        [Min(0.1f)] public float baseOrbitRadius = 1.4f;
        [Min(0.05f)] public float orbitRadiusStep = 0.55f;
        [Min(1f)] public float orbitSpeed = 28f;
        [Min(0.1f)] public float basePlanetClickDamage = 8f;
        [Min(0f)] public float planetSpawnDelay = 0.45f;

        [Header("Planet Orbit Lines")]
        public bool showOrbitLines = true;
        public Color orbitLineColor = new(0.55f, 0.75f, 1f, 0.22f);
        [Min(0.001f)] public float orbitLineWidth = 0.025f;
        [Range(16, 160)] public int orbitLineSegments = 72;
        public int orbitLineSortingOrder = 0;

        [Header("Camera Drag")]
        public bool cameraDragEnabled = true;
        [Range(0, 2)] public int cameraDragMouseButton = 1;
        [Min(0f)] public float cameraDragSpeed = 1f;

        [Header("Camera Zoom")]
        public bool cameraZoomEnabled = true;
        [Min(0f)] public float cameraZoomSpeed = 2f;

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
        public SpeciesBalanceConfig species = new();
        public PlanetBalanceConfig planets = new();
        public CivilizationBalanceConfig civilization = new();
        public UpgradeBalanceConfig upgrades = new();
        public MultiStarBalanceConfig multiStar = new();

        private void OnEnable() => EnsureNestedConfigs();

        public void EnsureNestedConfigs()
        {
            species ??= new SpeciesBalanceConfig();
            planets ??= new PlanetBalanceConfig();
            civilization ??= new CivilizationBalanceConfig();
            upgrades ??= new UpgradeBalanceConfig();
            multiStar ??= new MultiStarBalanceConfig();
        }

        public static SingleStarBalance CreateRuntimeDefault()
        {
            var balance = CreateInstance<SingleStarBalance>();
            balance.name = "RuntimeSingleStarBalance";
            balance.EnsureNestedConfigs();
            return balance;
        }
    }
}
