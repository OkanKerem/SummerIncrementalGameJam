using System;
using UnityEngine;

namespace Universes.Prototype
{
    [Serializable]
    public class PrototypeSpeciesBalanceConfig
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
        public PrototypeCivilizationStage selfDamageMinimumStage = PrototypeCivilizationStage.IndustrialAge;
        public PrototypeCivilizationStage selfDestructionMinimumStage = PrototypeCivilizationStage.IndustrialAge;
        [Range(0f, 1f)] public float mediumAggressionSelfDamageChance = 0.0025f;
        [Range(0f, 1f)] public float highAggressionSelfDamageChance = 0.01f;
        [Range(0f, 1f)] public float highAggressionSelfDestructionChance = 0.001f;
        [Min(0f)] public float aggressionSelfDamageAmount = 4f;
        public PrototypeCivilizationStage selfDestructionRegressToStage = PrototypeCivilizationStage.Life;

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
    public class PrototypePlanetTypeBalanceConfig
    {
        public PrototypePlanetType planetType;
        [Min(0)] public int baseClickReward = 3;
        [Range(0f, 1f)] public float habitabilityValue = 0.55f;
        [Range(0f, 1f)] public float dnaChance = 0.12f;
        [Min(0f)] public float baseDurability = 100f;
        public bool canCivilize = true;
        [Min(0f)] public float planetTypeWeight = 10f;
    }

    [Serializable]
    public class PrototypePlanetBalanceConfig
    {
        [Header("Fallbacks")]
        [Min(0f)] public float fallbackBaseDurability = 100f;
        [Min(0)] public int fallbackBaseClickReward = 1;
        public PrototypePlanetTypeBalanceConfig[] planetTypes =
        {
            new()
            {
                planetType = PrototypePlanetType.Rocky,
                baseClickReward = 3,
                habitabilityValue = 0.55f,
                dnaChance = 0.12f,
                baseDurability = 100f,
                canCivilize = true,
                planetTypeWeight = 18f
            },
            new()
            {
                planetType = PrototypePlanetType.Ocean,
                baseClickReward = 2,
                habitabilityValue = 0.85f,
                dnaChance = 0.15f,
                baseDurability = 90f,
                canCivilize = true,
                planetTypeWeight = 14f
            },
            new()
            {
                planetType = PrototypePlanetType.Lava,
                baseClickReward = 6,
                habitabilityValue = 0.15f,
                dnaChance = 0.08f,
                baseDurability = 70f,
                canCivilize = false,
                planetTypeWeight = 12f
            },
            new()
            {
                planetType = PrototypePlanetType.Ice,
                baseClickReward = 2,
                habitabilityValue = 0.35f,
                dnaChance = 0.1f,
                baseDurability = 120f,
                canCivilize = true,
                planetTypeWeight = 12f
            },
            new()
            {
                planetType = PrototypePlanetType.GasGiant,
                baseClickReward = 7,
                habitabilityValue = 0f,
                dnaChance = 0.05f,
                baseDurability = 110f,
                canCivilize = false,
                planetTypeWeight = 10f
            },
            new()
            {
                planetType = PrototypePlanetType.Toxic,
                baseClickReward = 4,
                habitabilityValue = 0.2f,
                dnaChance = 0.28f,
                baseDurability = 80f,
                canCivilize = true,
                planetTypeWeight = 8f
            },
            new()
            {
                planetType = PrototypePlanetType.Crystal,
                baseClickReward = 8,
                habitabilityValue = 0.4f,
                dnaChance = 0.35f,
                baseDurability = 55f,
                canCivilize = true,
                planetTypeWeight = 4f
            },
            new()
            {
                planetType = PrototypePlanetType.Desert,
                baseClickReward = 3,
                habitabilityValue = 0.45f,
                dnaChance = 0.1f,
                baseDurability = 95f,
                canCivilize = true,
                planetTypeWeight = 12f
            },
            new()
            {
                planetType = PrototypePlanetType.Forest,
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

        public float GetHabitabilityBonus(PrototypeStarStage stage) =>
            stage switch
            {
                PrototypeStarStage.Yellow => yellowStarHabitabilityBonus,
                PrototypeStarStage.Orange => orangeStarHabitabilityBonus,
                _ => unstableStarHabitabilityBonus
            };

        public float GetCivilizationSpeedMultiplier(PrototypeStarStage stage) =>
            stage switch
            {
                PrototypeStarStage.Yellow => yellowStarCivilizationSpeedMultiplier,
                PrototypeStarStage.Orange => orangeStarCivilizationSpeedMultiplier,
                PrototypeStarStage.RedGiant => redGiantCivilizationSpeedMultiplier,
                _ => unstableStarCivilizationSpeedMultiplier
            };

        public PrototypePlanetTypeBalanceConfig GetPlanetTypeConfig(PrototypePlanetType type)
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

        public PrototypePlanetType RollFallbackPlanetType()
        {
            if (planetTypes == null || planetTypes.Length == 0)
                return PrototypePlanetType.Rocky;

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
    public class PrototypeCivilizationStageConfig
    {
        public PrototypeCivilizationStage stage;
        public string stageName;
        [Min(0.01f)] public float progressRequirement = 1f;
        [Min(0f)] public float stageDnaValue;
        [Min(0f)] public float stageClickBonusMultiplier = 1f;
        [Min(0f)] public float stageSelfDestructionRiskMultiplier = 1f;
    }

    [Serializable]
    public class PrototypeCivilizationBalanceConfig
    {
        public PrototypeCivilizationStageConfig[] stages =
        {
            new()
            {
                stage = PrototypeCivilizationStage.NoLife,
                stageName = "No Life",
                progressRequirement = 1f,
                stageDnaValue = 0f,
                stageClickBonusMultiplier = 1f,
                stageSelfDestructionRiskMultiplier = 0f
            },
            new()
            {
                stage = PrototypeCivilizationStage.Life,
                stageName = "Life",
                progressRequirement = 1f,
                stageDnaValue = 0.2f,
                stageClickBonusMultiplier = 1.05f,
                stageSelfDestructionRiskMultiplier = 0f
            },
            new()
            {
                stage = PrototypeCivilizationStage.PrimitiveLife,
                stageName = "Primitive Life",
                progressRequirement = 1f,
                stageDnaValue = 0.35f,
                stageClickBonusMultiplier = 1.05f,
                stageSelfDestructionRiskMultiplier = 0f
            },
            new()
            {
                stage = PrototypeCivilizationStage.Tribe,
                stageName = "Tribe",
                progressRequirement = 1f,
                stageDnaValue = 0.55f,
                stageClickBonusMultiplier = 1.1f,
                stageSelfDestructionRiskMultiplier = 0f
            },
            new()
            {
                stage = PrototypeCivilizationStage.Civilization,
                stageName = "Civilization",
                progressRequirement = 1f,
                stageDnaValue = 0.85f,
                stageClickBonusMultiplier = 1.1f,
                stageSelfDestructionRiskMultiplier = 0.5f
            },
            new()
            {
                stage = PrototypeCivilizationStage.IndustrialAge,
                stageName = "Industrial Age",
                progressRequirement = 1f,
                stageDnaValue = 1.25f,
                stageClickBonusMultiplier = 1.15f,
                stageSelfDestructionRiskMultiplier = 1f
            },
            new()
            {
                stage = PrototypeCivilizationStage.SpaceAge,
                stageName = "Space Age",
                progressRequirement = 1f,
                stageDnaValue = 2f,
                stageClickBonusMultiplier = 1.15f,
                stageSelfDestructionRiskMultiplier = 1.5f
            }
        };

        public PrototypeCivilizationStageConfig GetStageConfig(PrototypeCivilizationStage stage)
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

        public string GetStageName(PrototypeCivilizationStage stage) =>
            GetStageConfig(stage)?.stageName ?? stage.ToString();

        public float GetStageDnaValue(PrototypeCivilizationStage stage) =>
            GetStageConfig(stage)?.stageDnaValue ?? 0f;

        public float GetStageProgressRequirement(PrototypeCivilizationStage stage) =>
            Mathf.Max(0.01f, GetStageConfig(stage)?.progressRequirement ?? 1f);

        public float GetStageClickBonusMultiplier(PrototypeCivilizationStage stage) =>
            Mathf.Max(0f, GetStageConfig(stage)?.stageClickBonusMultiplier ?? 1f);

        public float GetSelfDestructionRiskMultiplier(PrototypeCivilizationStage stage) =>
            Mathf.Max(0f, GetStageConfig(stage)?.stageSelfDestructionRiskMultiplier ?? 0f);

        public bool CanProgress(PrototypeCivilizationStage stage) =>
            stage < PrototypeCivilizationStage.SpaceAge;
    }

    [Serializable]
    public class PrototypeUpgradeBalanceConfig
    {
        [Min(0)] public int supernovaBonusPerLevel = 15;
        [Min(0f)] public float starStabilityAgeGainReductionPerLevel = 0.25f;
        [Min(0f)] public float baseClickCollectRadius = 1.1f;
        [Min(0f)] public float clickCollectRadiusPerLevel = 0.45f;
    }
}
