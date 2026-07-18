using System;
using UnityEngine;

namespace Universes.Game
{
    [Serializable]
    public class SpeciesBalanceConfig
    {
        [Header("Trait Ranges")]
        [Range(0, 100)] public int minimumTraitValue = 0;
        [Range(0, 100)] public int maximumTraitValue = 100;
        [Range(0, 100)] public int lowIntelligenceThreshold = 30;
        [Range(0, 100)] public int highIntelligenceThreshold = 70;
        [Range(0, 100)] public int lowAggressionThreshold = 30;
        [Range(0, 100)] public int highAggressionThreshold = 70;

        [Header("Intelligence")]
        [Min(0f)] public float lowIntelligenceDnaMultiplier = 0.9f;
        [Min(0f)] public float mediumIntelligenceDnaMultiplier = 1f;
        [Min(0f)] public float highIntelligenceDnaMultiplier = 1.1f;
        [Min(0f)] public float lowIntelligenceCivilizationSpeedMultiplier = 1f;
        [Min(0f)] public float mediumIntelligenceCivilizationSpeedMultiplier = 1.05f;
        [Min(0f)] public float highIntelligenceCivilizationSpeedMultiplier = 1.1f;
        [Min(0f)] public float lowIntelligenceClickStardustMultiplier = 1f;
        [Min(0f)] public float mediumIntelligenceClickStardustMultiplier = 1f;
        [Min(0f)] public float highIntelligenceClickStardustMultiplier = 1.05f;

        [Header("Aggression")]
        [Min(0f)] public float lowAggressionDnaMultiplier = 1f;
        [Min(0f)] public float mediumAggressionDnaMultiplier = 1.05f;
        [Min(0f)] public float highAggressionDnaMultiplier = 1.1f;
        [Min(0f)] public float lowAggressionCivilizationSpeedMultiplier = 1f;
        [Min(0f)] public float mediumAggressionCivilizationSpeedMultiplier = 1.02f;
        [Min(0f)] public float highAggressionCivilizationSpeedMultiplier = 1.05f;
        [Min(0f)] public float lowAggressionClickStardustMultiplier = 1f;
        [Min(0f)] public float mediumAggressionClickStardustMultiplier = 1.05f;
        [Min(0f)] public float highAggressionClickStardustMultiplier = 1.1f;

        [Header("Risk")]
        public CivilizationStage selfDamageMinimumStage = CivilizationStage.SpacePhase;
        public CivilizationStage selfDestructionMinimumStage = CivilizationStage.HardSpace;
        [Range(0f, 1f)] public float mediumAggressionSelfDamageChance = 0.0025f;
        [Range(0f, 1f)] public float highAggressionSelfDamageChance = 0.01f;
        [Range(0f, 1f)] public float highAggressionSelfDestructionChance = 0.001f;
        [Min(0f)] public float aggressionSelfDamageAmount = 4f;
        public CivilizationStage selfDestructionRegressToStage = CivilizationStage.PrimitiveLife;

        public int RollTrait() =>
            UnityEngine.Random.Range(Mathf.Min(minimumTraitValue, maximumTraitValue),
                Mathf.Max(minimumTraitValue, maximumTraitValue) + 1);

        public float GetDnaPotentialMultiplier(int intelligence, int aggression) =>
            GetIntelligenceDnaMultiplier(intelligence) * GetAggressionDnaMultiplier(aggression);

        public float GetCivilizationSpeedMultiplier(int intelligence, int aggression) =>
            GetIntelligenceCivilizationSpeedMultiplier(intelligence) *
            GetAggressionCivilizationSpeedMultiplier(aggression);

        public float GetClickStardustMultiplier(int intelligence, int aggression) =>
            GetIntelligenceClickStardustMultiplier(intelligence) *
            GetAggressionClickStardustMultiplier(aggression);

        public float GetSelfDamageChance(int aggression)
        {
            if (aggression > highAggressionThreshold)
                return highAggressionSelfDamageChance;

            if (aggression >= lowAggressionThreshold)
                return mediumAggressionSelfDamageChance;

            return 0f;
        }

        public float GetSelfDestructionChance(int aggression) =>
            aggression > highAggressionThreshold ? highAggressionSelfDestructionChance : 0f;

        private float GetIntelligenceDnaMultiplier(int value)
        {
            if (value < lowIntelligenceThreshold)
                return lowIntelligenceDnaMultiplier;

            return value > highIntelligenceThreshold
                ? highIntelligenceDnaMultiplier
                : mediumIntelligenceDnaMultiplier;
        }

        private float GetIntelligenceCivilizationSpeedMultiplier(int value)
        {
            if (value < lowIntelligenceThreshold)
                return lowIntelligenceCivilizationSpeedMultiplier;

            return value > highIntelligenceThreshold
                ? highIntelligenceCivilizationSpeedMultiplier
                : mediumIntelligenceCivilizationSpeedMultiplier;
        }

        private float GetIntelligenceClickStardustMultiplier(int value)
        {
            if (value < lowIntelligenceThreshold)
                return lowIntelligenceClickStardustMultiplier;

            return value > highIntelligenceThreshold
                ? highIntelligenceClickStardustMultiplier
                : mediumIntelligenceClickStardustMultiplier;
        }

        private float GetAggressionDnaMultiplier(int value)
        {
            if (value < lowAggressionThreshold)
                return lowAggressionDnaMultiplier;

            return value > highAggressionThreshold
                ? highAggressionDnaMultiplier
                : mediumAggressionDnaMultiplier;
        }

        private float GetAggressionCivilizationSpeedMultiplier(int value)
        {
            if (value < lowAggressionThreshold)
                return lowAggressionCivilizationSpeedMultiplier;

            return value > highAggressionThreshold
                ? highAggressionCivilizationSpeedMultiplier
                : mediumAggressionCivilizationSpeedMultiplier;
        }

        private float GetAggressionClickStardustMultiplier(int value)
        {
            if (value < lowAggressionThreshold)
                return lowAggressionClickStardustMultiplier;

            return value > highAggressionThreshold
                ? highAggressionClickStardustMultiplier
                : mediumAggressionClickStardustMultiplier;
        }
    }

    [Serializable]
    public class PlanetTypeBalanceConfig
    {
        public PlanetType planetType;
        [Min(0)] public int baseClickReward = 3;
        [Range(0f, 1f)] public float habitabilityValue = 0.55f;
        [Range(0f, 1f)] public float dnaChance = 0.12f;
        [Min(0f)] public float baseDurability = 100f;
        public bool canCivilize = true;
        [Min(0f)] public float planetTypeWeight = 10f;
    }

    [Serializable]
    public class PlanetBalanceConfig
    {
        [Header("Fallbacks")]
        [Min(0f)] public float fallbackBaseDurability = 100f;
        [Min(0)] public int fallbackBaseClickReward = 1;
        public PlanetTypeBalanceConfig[] planetTypes =
        {
            new()
            {
                planetType = PlanetType.Rocky,
                baseClickReward = 3,
                habitabilityValue = 0.55f,
                dnaChance = 0.12f,
                baseDurability = 100f,
                canCivilize = true,
                planetTypeWeight = 18f
            },
            new()
            {
                planetType = PlanetType.Ocean,
                baseClickReward = 2,
                habitabilityValue = 0.85f,
                dnaChance = 0.15f,
                baseDurability = 90f,
                canCivilize = true,
                planetTypeWeight = 14f
            },
            new()
            {
                planetType = PlanetType.Lava,
                baseClickReward = 6,
                habitabilityValue = 0.15f,
                dnaChance = 0.08f,
                baseDurability = 70f,
                canCivilize = false,
                planetTypeWeight = 12f
            },
            new()
            {
                planetType = PlanetType.Ice,
                baseClickReward = 2,
                habitabilityValue = 0.35f,
                dnaChance = 0.1f,
                baseDurability = 120f,
                canCivilize = true,
                planetTypeWeight = 12f
            },
            new()
            {
                planetType = PlanetType.GasGiant,
                baseClickReward = 7,
                habitabilityValue = 0f,
                dnaChance = 0.05f,
                baseDurability = 110f,
                canCivilize = false,
                planetTypeWeight = 10f
            },
            new()
            {
                planetType = PlanetType.Toxic,
                baseClickReward = 4,
                habitabilityValue = 0.2f,
                dnaChance = 0.28f,
                baseDurability = 80f,
                canCivilize = true,
                planetTypeWeight = 8f
            },
            new()
            {
                planetType = PlanetType.Crystal,
                baseClickReward = 8,
                habitabilityValue = 0.4f,
                dnaChance = 0.35f,
                baseDurability = 55f,
                canCivilize = true,
                planetTypeWeight = 4f
            },
            new()
            {
                planetType = PlanetType.Desert,
                baseClickReward = 3,
                habitabilityValue = 0.45f,
                dnaChance = 0.1f,
                baseDurability = 95f,
                canCivilize = true,
                planetTypeWeight = 12f
            },
            new()
            {
                planetType = PlanetType.Forest,
                baseClickReward = 3,
                habitabilityValue = 0.9f,
                dnaChance = 0.18f,
                baseDurability = 85f,
                canCivilize = true,
                planetTypeWeight = 10f
            }
        };

        [Header("Life")]
        [Min(0f)] public float initialLifeChanceMultiplier = 0.5f;
        [Min(0f)] public float lifeDnaChanceMultiplier = 1.25f;
        [Min(0f)] public float civilizationDnaChanceMultiplier = 0.05f;
        [Min(0f)] public float baseDnaPotentialAmount = 1f;

        [Header("Star Habitability Modifiers")]
        public float yellowStarHabitabilityBonus = 0.1f;
        public float orangeStarHabitabilityBonus = 0.05f;
        public float unstableStarHabitabilityBonus = -0.1f;

        [Header("Star Civilization Speed")]
        [Min(0f)] public float yellowStarCivilizationSpeedMultiplier = 1.1f;
        [Min(0f)] public float orangeStarCivilizationSpeedMultiplier = 1f;
        [Min(0f)] public float redGiantCivilizationSpeedMultiplier = 0.7f;
        [Min(0f)] public float unstableStarCivilizationSpeedMultiplier = 0.3f;

        public float GetHabitabilityBonus(StarStage stage) =>
            stage switch
            {
                StarStage.Yellow => yellowStarHabitabilityBonus,
                StarStage.Orange => orangeStarHabitabilityBonus,
                _ => unstableStarHabitabilityBonus
            };

        public float GetCivilizationSpeedMultiplier(StarStage stage) =>
            stage switch
            {
                StarStage.Yellow => yellowStarCivilizationSpeedMultiplier,
                StarStage.Orange => orangeStarCivilizationSpeedMultiplier,
                StarStage.RedGiant => redGiantCivilizationSpeedMultiplier,
                _ => unstableStarCivilizationSpeedMultiplier
            };

        public PlanetTypeBalanceConfig GetPlanetTypeConfig(PlanetType type)
        {
            if (planetTypes != null)
            {
                foreach (var planetType in planetTypes)
                {
                    if (planetType != null && planetType.planetType == type)
                        return planetType;
                }
            }

            return null;
        }

        public PlanetType RollFallbackPlanetType()
        {
            if (planetTypes == null || planetTypes.Length == 0)
                return PlanetType.Rocky;

            var total = 0f;
            foreach (var planetType in planetTypes)
            {
                if (planetType != null)
                    total += Mathf.Max(0f, planetType.planetTypeWeight);
            }

            if (total <= 0f)
                return planetTypes[0].planetType;

            var roll = UnityEngine.Random.value * total;
            foreach (var planetType in planetTypes)
            {
                if (planetType == null)
                    continue;

                roll -= Mathf.Max(0f, planetType.planetTypeWeight);
                if (roll <= 0f)
                    return planetType.planetType;
            }

            return planetTypes[planetTypes.Length - 1].planetType;
        }
    }

    [Serializable]
    public class CivilizationStageConfig
    {
        public CivilizationStage stage;
        public string stageName;
        [Min(0.01f)] public float progressRequirement = 1f;
        [Min(0f)] public float stageDnaValue;
        [Min(0f)] public float stageClickBonusMultiplier = 1f;
        [Min(0f)] public float stageSelfDestructionRiskMultiplier = 1f;
    }

    [Serializable]
    public class CivilizationBalanceConfig
    {
        public CivilizationStageConfig[] stages =
        {
            new()
            {
                stage = CivilizationStage.NoLife,
                stageName = "No Life",
                progressRequirement = 1f,
                stageDnaValue = 0f,
                stageClickBonusMultiplier = 1f,
                stageSelfDestructionRiskMultiplier = 0f
            },
            new()
            {
                stage = CivilizationStage.PrimitiveLife,
                stageName = "Primitive",
                progressRequirement = 1f,
                stageDnaValue = 0.35f,
                stageClickBonusMultiplier = 1.05f,
                stageSelfDestructionRiskMultiplier = 0f
            },
            new()
            {
                stage = CivilizationStage.CivilizationPhase,
                stageName = "Civilization",
                progressRequirement = 1.25f,
                stageDnaValue = 0.9f,
                stageClickBonusMultiplier = 1.12f,
                stageSelfDestructionRiskMultiplier = 0.5f
            },
            new()
            {
                stage = CivilizationStage.SpacePhase,
                stageName = "Space",
                progressRequirement = 1.5f,
                stageDnaValue = 2f,
                stageClickBonusMultiplier = 1.15f,
                stageSelfDestructionRiskMultiplier = 1.5f
            },
            new()
            {
                stage = CivilizationStage.HardSpace,
                stageName = "Hard Space",
                progressRequirement = 2f,
                stageDnaValue = 3.5f,
                stageClickBonusMultiplier = 1.25f,
                stageSelfDestructionRiskMultiplier = 2f
            }
        };

        [Header("Space Travel")]
        public CivilizationStage spaceshipMinimumStage = CivilizationStage.SpacePhase;
        public GameObject spaceshipPrefab;
        public Sprite[] spaceshipSprites = Array.Empty<Sprite>();
        public GameObject spaceshipAlienPortraitPrefab;
        [Min(0.1f)] public float spaceshipTripInterval = 4f;
        [Min(0.1f)] public float spaceshipSpeed = 2.5f;
        [Min(0f)] public float spaceshipDnaPotentialPerTrip = 1f;
        [Min(1)] public int maxActiveSpaceships = 3;

        [Header("Space Stations")]
        public CivilizationStage spaceStationMinimumStage = CivilizationStage.HardSpace;
        public GameObject spaceStationPrefab;
        public Sprite[] spaceStationSprites = Array.Empty<Sprite>();
        [Min(0.1f)] public float spaceStationSpawnInterval = 6f;
        [Min(1)] public int maxActiveSpaceStations = 2;
        [Min(0.1f)] public float spaceStationTravelSpeed = 2.5f;
        [Min(0.05f)] public float spaceStationOrbitRadius = 0.2f;
        [Min(0.1f)] public float spaceStationOrbitRadiusPlanetMultiplier = 2f;
        [Min(1f)] public float spaceStationOrbitSpeed = 28f;
        [Min(0.1f)] public float spaceStationDnaInterval = 3f;
        [Min(0f)] public float spaceStationDnaPotentialPerTick = 0.75f;

        [Header("Colonization")]
        public bool colonizationEnabled = true;
        public CivilizationStage colonizationMinimumSourceStage = CivilizationStage.SpacePhase;
        [Range(0f, 1f)] public float colonizationBaseChance = 0.2f;
        [Range(0f, 1f)] public float colonizationIntelligenceBonus = 0.003f;
        public CivilizationStage colonizationTargetStage = CivilizationStage.PrimitiveLife;
        public bool colonizationRequiresHabitable = true;

        [Header("Hard Space Destroy Protocol")]
        public bool destroyProtocolEnabled = true;
        [Min(0.1f)] public float destroyRocketSpeed = 4f;
        [Min(1)] public int destroyRocketBarrageCount = 50;
        [Min(1f)] public float destroyRocketTravelDistance = 18f;
        [Min(0f)] public float destroyProtocolCollapseDelay = 1.75f;

        public float GetColonizationChance(Planet source)
        {
            if (source == null || !source.HasSpecies)
                return 0f;

            return Mathf.Clamp01(colonizationBaseChance + source.Intelligence * colonizationIntelligenceBonus);
        }

        public CivilizationStageConfig GetStageConfig(CivilizationStage stage)
        {
            if (stages != null)
            {
                foreach (var stageConfig in stages)
                {
                    if (stageConfig != null && stageConfig.stage == stage)
                        return stageConfig;
                }
            }

            return null;
        }

        public string GetStageName(CivilizationStage stage) =>
            GetStageConfig(stage)?.stageName ?? stage.ToString();

        public float GetStageDnaValue(CivilizationStage stage) =>
            GetStageConfig(stage)?.stageDnaValue ?? 0f;

        public float GetStageProgressRequirement(CivilizationStage stage) =>
            Mathf.Max(0.01f, GetStageConfig(stage)?.progressRequirement ?? 1f);

        public float GetStageClickBonusMultiplier(CivilizationStage stage) =>
            Mathf.Max(0f, GetStageConfig(stage)?.stageClickBonusMultiplier ?? 1f);

        public float GetSelfDestructionRiskMultiplier(CivilizationStage stage) =>
            Mathf.Max(0f, GetStageConfig(stage)?.stageSelfDestructionRiskMultiplier ?? 0f);

        public bool CanProgress(CivilizationStage stage) =>
            stage < CivilizationStage.HardSpace;
    }

    [Serializable]
    public class Phase3BalanceConfig
    {
        [Header("Activation")]
        public bool enterPhase3WhenSpeciesReachesSpace = false;
        [Min(1)] public int maxStarSlots = 20;
        [Min(1f)] public float maxEntropy = 500f;

        [Header("Auto Star Formation")]
        public bool autoStarFormationEnabled = true;
        [Min(0.1f)] public float autoStarFormationCheckInterval = 5f;
        [Range(0f, 1f)] public float autoStarFormationChance = 0f;
        [Range(0f, 1f)] public float autoStarFormationChancePerLevel = 0.08f;

        [Header("Star Layout")]
        public Vector2 spawnAreaMin = new(-15f, -8f);
        public Vector2 spawnAreaMax = new(15f, 8f);
        [Min(0f)] public float minStarSeparation = 2.5f;

        [Header("Black Holes")]
        [Min(0.1f)] public float blackHoleLifetimeSeconds = 45f;
        [Min(0f)] public float blackHoleEntropyPerSecond = 0.42f;
        [Min(0.1f)] public float blackHoleDnaIntervalSeconds = 4f;
        [Min(0f)] public float blackHoleDnaPotentialPerInterval = 1f;
        [Min(0f)] public float blackHoleInitialDnaTimerMultiplier = 0.5f;
        [Range(0f, 1f)] public float blackHoleDnaFragmentChance = 0.35f;
        [Min(0f)] public float blackHoleParticlePullRadius = 2.5f;
        [Range(0f, 1f)] public float blackHoleConsumeDnaChance = 0.15f;
        [Min(0f)] public float blackHoleConsumedStardustMultiplier = 0.5f;
        [Min(0f)] public float blackHoleGravityPullRadius = 2.5f;
        [Min(0f)] public float blackHoleGravityPullSpeed = 1.35f;
        [Min(0f)] public float blackHoleStarConsumeRadius = 0.42f;
        [Min(0f)] public float blackHolePlanetConsumeRadius = 0.38f;
        [Min(0f)] public float blackHolePlanetDamageRadius = 1.15f;
        [Min(0f)] public float blackHolePlanetDamagePerSecond = 12f;
        [Min(0f)] public float blackHoleStarAgeDamagePerSecond = 10f;

        [Header("Star Collisions")]
        [Min(0f)] public float starDriftSpeed = 0.07f;
        [Min(0f)] public float starCollisionDistance = 0.85f;
        [Min(0f)] public float collisionStardustBurst = 80f;
        [Min(0f)] public int collisionParticleBurst = 60;
        [Min(0f)] public float collisionAgeBonus = 8f;
        [Min(0f)] public float collisionEntropy = 15f;
        [Range(0f, 1f)] public float collisionDnaChance = 0.18f;
        [Range(0f, 1f)] public float collisionBlackHoleChance = 0.12f;
        [Min(0f)] public float collisionDnaPotential = 0f;
        [Min(0f)] public float collisionPlanetDamageRadius = 2.5f;
        [Min(0f)] public float collisionPlanetDamage = 65f;

        [Header("Supernovas")]
        [Min(0f)] public int supernovaParticleBurst = 120;
        [Min(0f)] public float supernovaRadius = 2.8f;
        [Min(0f)] public float supernovaAgeBurst = 18f;
        [Min(0f)] public float supernovaPlanetDamageRadius = 2.25f;
        [Min(0f)] public float supernovaPlanetDamage = 45f;
        [Range(0f, 1f)] public float supernovaDnaChance = 0.22f;
        [Range(0f, 1f)] public float supernovaBlackHoleChance = 0.06f;
        [Range(0f, 1f)] public float massiveParticleDnaChance = 0.1f;

        [Header("Advanced Species")]
        [Min(0f)] public float spacePhaseDnaMultiplier = 1.6f;
        [Min(0f)] public float hardSpaceDnaMultiplier = 3f;
        [Min(0f)] public float spacePhasePlanetClickMultiplier = 1.1f;
        [Min(0f)] public float hardSpacePlanetClickMultiplier = 1.25f;
        [Min(0f)] public float hardSpaceProgressPerCivilizationTick = 0.35f;
        [Min(0.01f)] public float hardSpaceCompletionRequirement = 5f;
        [Min(0f)] public float hardSpaceRequiredDnaPotential = 25f;
        [Range(0, 100)] public int hardSpaceRequiredIntelligence = 65;
        public bool hardSpaceRequiresBlackHoleDiscovery = true;

        [Header("End Universe")]
        [Min(0f)] public float endUniverseRequiredDnaPotential = 40f;
        [Min(0f)] public float endUniverseRequiredEntropy = 35f;
        public bool endUniverseRequiresSpaceSpecies = true;
        public bool endUniverseRequiresBlackHoleDiscovery = true;

        [Header("Universe Collapse")]
        [Min(0.5f)] public float universeCollapseDuration = 3.5f;
        [Min(0.1f)] public float universeCollapsePullSpeed = 7f;
        [Min(0.5f)] public float universeCollapseSingularityScale = 3.5f;
    }

    [Serializable]
    public class UpgradeBalanceConfig
    {
        [Min(0)] public int clickPowerBaseIncrement = 1;
        [Min(0)] public int passiveProductionBaseIncrement = 1;
        [Min(0)] public int supernovaBonusPerLevel = 15;
        [Min(0f)] public float clickRewardPercentPerLevel = 0.08f;
        [Min(0f)] public float starStabilityAgeGainReductionPerLevel = 0.25f;
        [Min(0f)] public float advancedStarStabilityAgeGainReductionPerLevel = 0.15f;
        [Min(0f)] public float baseClickCollectRadius = 1.1f;
        [Min(0f)] public float clickCollectRadiusPerLevel = 0.45f;
        [Min(0f)] public float starPassiveProductionPercentPerLevel = 0.25f;
        [Min(0f)] public float collisionDnaPotentialPerLevel = 1f;
        [Range(0f, 1f)] public float entropyReductionPerLevel = 0.08f;
        [Min(0f)] public float speciesDnaPotentialPerLevel = 0.25f;

        [Header("Phase 3 Upgrades")]
        [Min(0f)] public float collisionAttractionDriftMultiplierPerLevel = 0.12f;
        [Min(0f)] public float collisionAttractionDistancePerLevel = 0.08f;
        [Range(0f, 1f)] public float blackHoleEntropyReductionPerLevel = 0.1f;
        [Min(0f)] public float blackHoleMemoryDnaMultiplierPerLevel = 0.2f;
        [Min(0f)] public float spaceAgeDnaMultiplierPerLevel = 0.15f;
        [Min(0f)] public float spaceAgeProgressionMultiplierPerLevel = 0.12f;
        [Range(0f, 1f)] public float cosmicEventDnaChancePerLevel = 0.025f;
        [Min(0f)] public float orbitalDnaMultiplierPerLevel = 0.2f;
    }

    [Serializable]
    public class MultiStarBalanceConfig
    {
        [Min(1)] public int initialStarSlots = 1;
        [Min(1)] public int maxStarSlots = 10;
        [Min(1f)] public float maxEntropy = 100f;
        [Min(0)] public double createStarBaseCost = 75;
        [Min(1)] public double createStarCostScale = 1.65;
        [Min(0f)] public float createStarCostIncreasePercent = 65f;
        [Min(0)] public int supernovaStardustBonus = 50;
        [Range(0f, 1f)] public float supernovaPlanetDestroyChance = 0.35f;
        [Min(0f)] public float supernovaPlanetDamage = 45f;
        [Min(0f)] public float supernovaNearbyStarRadius = 4f;
        [Min(0f)] public float supernovaNearbyStarAgeDamage = 18f;

        [Header("Planet Consequences")]
        [Min(0f)] public float planetDeathStarAgeDamage = 4f;
        [Min(0f)] public float planetDeathDnaPotential = 1f;
        [Min(0f)] public float planetDeathEntropy = 1.5f;
        [Min(0f)] public float planetCollisionDistance = 0.45f;
        [Min(0f)] public float planetCollisionDnaPotential = 4f;
        [Min(0f)] public float planetCollisionEntropy = 1.5f;
        [Min(0f)] public float planetOverloadAgeGainPerPlanet = 0.08f;
        [Min(0)] public int planetCountBeforeOverload = 2;

        [Header("Planet Production")]
        [Min(0f)] public float basePlanetPassiveStardust = 1f;
        [Min(0f)] public float planetPassiveProductionPercentPerLevel = 0.5f;
        [Min(0f)] public float starClickValuePerPlanetPerUpgradeLevel = 0.5f;
        [Min(0f)] public float entropyPerActiveStarPerSecond = 0.015f;
        [Min(0f)] public float entropyPerPlanetPerSecond = 0.01f;

        [Header("Camera")]
        [Min(0.1f)] public float phase1CameraSize = 5f;
        [Min(0.1f)] public float phase2CameraStartSize = 5f;
        [Min(0.1f)] public float phase2CameraTargetSize = 10f;
        [Min(0f)] public float phase2CameraZoomDuration = 1.25f;
        [Min(0.1f)] public float minZoom = 3f;
        [Min(0.1f)] public float phase1MaxZoom = 10f;
        [Min(0.1f)] public float phase2MaxZoom = 10f;
        [Min(0.1f)] public float phase3MaxZoom = 15f;
        [Min(0.1f)] public float phase3CameraStartSize = 10f;
        [Min(0.1f)] public float phase3CameraTargetSize = 15f;
        [Min(0f)] public float phase3CameraZoomDuration = 1.25f;

        [Header("Star Layout")]
        public Vector2 spawnAreaMin = new(-7.5f, -4f);
        public Vector2 spawnAreaMax = new(7.5f, 4f);
        [Min(0f)] public float minStarSeparation = 2.5f;

        [Header("Constellation")]
        [Min(1)] public int constellationStarCount = 5;
        [Min(0.25f)] public float constellationRadius = 2.4f;
        [Min(0f)] public float constellationJitter = 0.18f;

        [Header("Star Movement")]
        [Min(0f)] public float starDriftSpeed = 0.035f;
        public bool moveCentralStarInPhase2 = true;
    }
}
