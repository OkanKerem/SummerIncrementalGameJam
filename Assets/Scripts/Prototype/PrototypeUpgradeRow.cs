using UnityEngine;
using UnityEngine.UI;

namespace Universes.Prototype
{
    public class PrototypeUpgradeRow : MonoBehaviour
    {
        [SerializeField] private PrototypeUpgradeDefinition definition;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text costText;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Color enabledButtonColor = new(0.18f, 0.32f, 0.5f);
        [SerializeField] private Color disabledButtonColor = new(0.15f, 0.15f, 0.18f);

        private PrototypeGameController _controller;

        public PrototypeUpgradeDefinition Definition => definition;

        public void Initialize(PrototypeGameController controller)
        {
            _controller = controller;

            if (definition == null)
            {
                Debug.LogWarning($"PrototypeUpgradeRow on {name} has no upgrade definition assigned.", this);
                return;
            }

            if (descriptionText != null && !string.IsNullOrEmpty(definition.description))
                descriptionText.text = definition.description;

            RefreshIcon();

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyClicked);
            }

            Refresh();
        }

        private void OnBuyClicked()
        {
            if (_controller == null || definition == null)
                return;

            if (definition.upgradeType == PrototypeUpgradeType.ExpandUniverse)
            {
                if (_controller.TryExpandUniverse(definition))
                    Refresh();
                return;
            }

            if (_controller.TryPurchaseUpgrade(definition))
                Refresh();
        }

        public void Refresh() => Refresh(hideMaxed: false);

        public void Refresh(bool hideMaxed)
        {
            if (_controller == null || definition == null)
                return;

            if (!IsUpgradeTierVisible())
            {
                gameObject.SetActive(false);
                return;
            }

            if (definition.upgradeType == PrototypeUpgradeType.ExpandUniverse)
            {
                RefreshExpandUniverseRow(hideMaxed);
                return;
            }

            RefreshStandardRow(hideMaxed);
        }

        private void RefreshExpandUniverseRow(bool hideMaxed)
        {
            var show = _controller.IsSingleStarMode && !_controller.Upgrades.IsUniverseExpanded;
            if (hideMaxed && definition.maxLevel > 0 && _controller.Upgrades.GetLevel(definition) >= definition.maxLevel)
                show = false;

            gameObject.SetActive(show);
            if (!show)
                return;

            if (titleText != null)
                titleText.text = definition.displayName;

            if (descriptionText != null && !string.IsNullOrEmpty(definition.description))
                descriptionText.text = definition.description;

            RefreshIcon();

            var step2Ready = PrototypeGameplayFeatures.Step2ExpansionAvailable;
            var cost = _controller.Upgrades.GetCost(definition);
            var canAfford = _controller.Stardust >= cost;

            if (costText != null)
            {
                costText.text = step2Ready ? $"Buy ({cost:0})" : "Soon";
            }

            if (buyButton != null)
            {
                var canBuy = !_controller.IsRunEnded && (step2Ready ? canAfford : true);
                buyButton.interactable = canBuy;
                if (buttonImage != null)
                    buttonImage.color = canBuy ? enabledButtonColor : disabledButtonColor;
            }
        }

        private void RefreshStandardRow(bool hideMaxed)
        {
            var level = _controller.Upgrades.GetLevel(definition);
            var cost = _controller.Upgrades.GetCost(definition);
            var unlocked = definition.ArePrerequisitesMet(_controller, out var requirementText);
            var atMax = definition.maxLevel > 0 && level >= definition.maxLevel;

            if (hideMaxed && atMax)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = GetDisplayTitle(level);

            if (descriptionText != null)
            {
                descriptionText.text = BuildDescription(level, atMax, unlocked, requirementText);
            }

            RefreshIcon();

            if (costText != null)
                costText.text = atMax ? "Max" : unlocked ? $"Buy ({cost:0})" : "Locked";

            if (buyButton != null)
            {
                var canBuy = unlocked && !_controller.IsRunEnded && !atMax && _controller.Stardust >= cost;
                buyButton.interactable = canBuy;
                if (buttonImage != null)
                    buttonImage.color = canBuy ? enabledButtonColor : disabledButtonColor;
            }
        }

        private string GetDisplayTitle(int level)
        {
            var tierPrefix = definition.upgradeTier > 1 ? $"Level {definition.upgradeTier} - " : string.Empty;
            return $"{tierPrefix}{definition.displayName} (Lv {level})";
        }

        private void RefreshIcon()
        {
            if (iconImage == null || definition == null)
                return;

            iconImage.sprite = definition.iconSprite;
            iconImage.enabled = definition.iconSprite != null;
            iconImage.preserveAspect = true;
        }

        private string BuildDescription(int level, bool atMax, bool unlocked, string requirementText)
        {
            var text = definition.description ?? string.Empty;
            var preview = GetEffectPreview(level, atMax);
            if (!string.IsNullOrWhiteSpace(preview))
                text = string.IsNullOrWhiteSpace(text) ? preview : $"{text}\n{preview}";

            if (!unlocked && !string.IsNullOrWhiteSpace(requirementText))
                text = $"{text}\nLocked: Requires {requirementText}";

            return text;
        }

        private string GetEffectPreview(int level, bool atMax)
        {
            if (_controller == null || definition == null)
                return string.Empty;

            var nextLevel = atMax ? level : level + 1;
            var line = definition.upgradeType switch
            {
                PrototypeUpgradeType.ClickPower => FormatFlat("Click Power",
                    GetCumulativeBonus(level, _controller.SingleStarBalance.upgrades.clickPowerBaseIncrement),
                    GetCumulativeBonus(nextLevel, _controller.SingleStarBalance.upgrades.clickPowerBaseIncrement)),
                PrototypeUpgradeType.ClickPowerPercent => FormatPercent("Click Multiplier",
                    GetCompoundedPercent(level, _controller.SingleStarBalance.upgrades.clickRewardPercentPerLevel),
                    GetCompoundedPercent(nextLevel, _controller.SingleStarBalance.upgrades.clickRewardPercentPerLevel)),
                PrototypeUpgradeType.PassiveProduction => FormatFlat("Passive Stardust",
                    GetCumulativeBonus(level, _controller.SingleStarBalance.upgrades.passiveProductionBaseIncrement),
                    GetCumulativeBonus(nextLevel, _controller.SingleStarBalance.upgrades.passiveProductionBaseIncrement)),
                PrototypeUpgradeType.StarStability => FormatPercent("Star Age Speed",
                    GetAgeSpeedPercent(level, _controller.Upgrades.AdvancedStarStabilityLevel),
                    GetAgeSpeedPercent(nextLevel, _controller.Upgrades.AdvancedStarStabilityLevel),
                    lowerIsBetter: true),
                PrototypeUpgradeType.AdvancedStarStability => FormatPercent("Star Age Speed",
                    GetAgeSpeedPercent(_controller.Upgrades.StarStabilityLevel, level),
                    GetAgeSpeedPercent(_controller.Upgrades.StarStabilityLevel, nextLevel),
                    lowerIsBetter: true),
                PrototypeUpgradeType.SupernovaBonus => FormatFlat("Supernova Stardust",
                    level * _controller.SingleStarBalance.upgrades.supernovaBonusPerLevel,
                    nextLevel * _controller.SingleStarBalance.upgrades.supernovaBonusPerLevel),
                PrototypeUpgradeType.ClickCollectRadius => FormatDecimal("Collect Radius",
                    _controller.SingleStarBalance.upgrades.baseClickCollectRadius +
                    level * _controller.SingleStarBalance.upgrades.clickCollectRadiusPerLevel,
                    _controller.SingleStarBalance.upgrades.baseClickCollectRadius +
                    nextLevel * _controller.SingleStarBalance.upgrades.clickCollectRadiusPerLevel),
                PrototypeUpgradeType.MaxPlanetCount => FormatFlat("Max Planets",
                    _controller.SingleStarBalance.baseMaxPlanets + level,
                    _controller.SingleStarBalance.baseMaxPlanets + nextLevel),
                PrototypeUpgradeType.AutoPlanetFormation => FormatPercent("Auto Planet Chance",
                    _controller.SingleStarBalance.baseAutoFormationChance +
                    level * _controller.SingleStarBalance.autoFormationChancePerLevel,
                    _controller.SingleStarBalance.baseAutoFormationChance +
                    nextLevel * _controller.SingleStarBalance.autoFormationChancePerLevel),
                PrototypeUpgradeType.PlanetDnaChance => FormatPercent("Planet DNA Chance Bonus",
                    level * _controller.SingleStarBalance.planetDnaChancePerLevel,
                    nextLevel * _controller.SingleStarBalance.planetDnaChancePerLevel),
                PrototypeUpgradeType.PlanetClickValue => FormatDecimal("Planet Click Stardust",
                    level * _controller.SingleStarBalance.planetClickValuePerLevel,
                    nextLevel * _controller.SingleStarBalance.planetClickValuePerLevel),
                PrototypeUpgradeType.HabitablePlanetChance => FormatPercent("Habitability Chance Bonus",
                    _controller.SingleStarBalance.baseHabitableRollBonus +
                    level * _controller.SingleStarBalance.habitableChancePerLevel,
                    _controller.SingleStarBalance.baseHabitableRollBonus +
                    nextLevel * _controller.SingleStarBalance.habitableChancePerLevel),
                PrototypeUpgradeType.MaxStarCount => FormatFlat("Star Slots",
                    Mathf.Clamp(_controller.SingleStarBalance.multiStar.initialStarSlots + level, 1,
                        _controller.SingleStarBalance.multiStar.maxStarSlots),
                    Mathf.Clamp(_controller.SingleStarBalance.multiStar.initialStarSlots + nextLevel, 1,
                        _controller.SingleStarBalance.multiStar.maxStarSlots)),
                PrototypeUpgradeType.PlanetPassiveProduction => FormatDecimal("Planet Passive Stardust",
                    _controller.SingleStarBalance.multiStar.basePlanetPassiveStardust +
                    level * _controller.SingleStarBalance.multiStar.planetPassiveStardustPerUpgradeLevel,
                    _controller.SingleStarBalance.multiStar.basePlanetPassiveStardust +
                    nextLevel * _controller.SingleStarBalance.multiStar.planetPassiveStardustPerUpgradeLevel),
                PrototypeUpgradeType.StarPlanetClickValue => FormatDecimal("Star Click Per Planet",
                    level * _controller.SingleStarBalance.multiStar.starClickValuePerPlanetPerUpgradeLevel,
                    nextLevel * _controller.SingleStarBalance.multiStar.starClickValuePerPlanetPerUpgradeLevel),
                PrototypeUpgradeType.StarPassiveProductionPercent => FormatPercent("Star Passive Multiplier",
                    GetCompoundedPercent(level,
                        _controller.SingleStarBalance.upgrades.starPassiveProductionPercentPerLevel),
                    GetCompoundedPercent(nextLevel,
                        _controller.SingleStarBalance.upgrades.starPassiveProductionPercentPerLevel)),
                PrototypeUpgradeType.CollisionDnaProduction => FormatDecimal("Collision DNA Potential",
                    level * _controller.SingleStarBalance.upgrades.collisionDnaPotentialPerLevel,
                    nextLevel * _controller.SingleStarBalance.upgrades.collisionDnaPotentialPerLevel),
                PrototypeUpgradeType.EntropyReduction => FormatPercent("Entropy Reduction",
                    Mathf.Clamp01(level * _controller.SingleStarBalance.upgrades.entropyReductionPerLevel),
                    Mathf.Clamp01(nextLevel * _controller.SingleStarBalance.upgrades.entropyReductionPerLevel)),
                PrototypeUpgradeType.SpeciesDnaProduction => FormatDecimal("Species DNA Per Tick",
                    level * _controller.SingleStarBalance.upgrades.speciesDnaPotentialPerLevel,
                    nextLevel * _controller.SingleStarBalance.upgrades.speciesDnaPotentialPerLevel),
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            return atMax ? $"Max level" : $"{line}";
        }

        private float GetAgeSpeedPercent(int stabilityLevel, int advancedLevel)
        {
            var balance = _controller.SingleStarBalance.upgrades;
            var multiplier = 1f / (1f +
                                   stabilityLevel * balance.starStabilityAgeGainReductionPerLevel +
                                   advancedLevel * balance.advancedStarStabilityAgeGainReductionPerLevel);
            return multiplier;
        }

        private static int GetCumulativeBonus(int level, int baseIncrement)
        {
            if (level <= 0 || baseIncrement <= 0)
                return 0;

            return level * (level + 1) / 2 * baseIncrement;
        }

        private static float GetCompoundedPercent(int level, float percentPerLevel) =>
            Mathf.Pow(1f + percentPerLevel, level) - 1f;

        private static string FormatFlat(string label, int current, int next)
        {
            return $"{label}: {current:0} -> {next:0}";
        }

        private static string FormatDecimal(string label, float current, float next)
        {
            return $"{label}: {current:0.##} -> {next:0.##}";
        }

        private static string FormatPercent(string label, float current, float next, bool lowerIsBetter = false)
        {
            return $"{label}: {current * 100f:0.#}% -> {next * 100f:0.#}%";
        }

        private bool IsUpgradeTierVisible()
        {
            if (definition.upgradeTier <= 1)
                return true;

            return !_controller.IsSingleStarMode;
        }
    }
}
