using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Universes.Game
{
    public class UpgradeRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private UpgradeDefinition definition;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text costText;
        [SerializeField] private Image buttonImage;
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private Text tooltipText;
        [SerializeField] private Color enabledButtonColor = new(0.18f, 0.32f, 0.5f);
        [SerializeField] private Color disabledButtonColor = new(0.15f, 0.15f, 0.18f);

        private GameController _controller;
        private bool _hovering;

        public UpgradeDefinition Definition => definition;

        public void ConfigureTooltip(GameObject root, Text text)
        {
            tooltipRoot = root;
            tooltipText = text;
        }

        public void Initialize(GameController controller)
        {
            _controller = controller;

            if (definition == null)
            {
                Debug.LogWarning($"UpgradeRow on {name} has no upgrade definition assigned.", this);
                return;
            }

            if (descriptionText != null)
                descriptionText.gameObject.SetActive(false);

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

            if (definition.upgradeType == UpgradeType.ExpandUniverse)
            {
                if (_controller.TryExpandUniverse(definition))
                    Refresh();
                return;
            }

            if (definition.upgradeType == UpgradeType.ExpandCosmic)
            {
                if (_controller.TryExpandCosmic(definition))
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

            if (definition.upgradeType == UpgradeType.ExpandUniverse)
            {
                RefreshExpandUniverseRow(hideMaxed);
                return;
            }

            if (definition.upgradeType == UpgradeType.ExpandCosmic)
            {
                RefreshExpandCosmicRow(hideMaxed);
                return;
            }

            RefreshStandardRow(hideMaxed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovering = true;
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
            if (tooltipRoot != null)
                tooltipRoot.SetActive(false);
        }

        private void ShowTooltip()
        {
            if (tooltipRoot == null || tooltipText == null || _controller == null || definition == null)
                return;

            tooltipText.text = BuildTooltipText();
            PositionTooltipAtRow();
            tooltipRoot.SetActive(true);
        }

        private void PositionTooltipAtRow()
        {
            var rowRect = transform as RectTransform;
            var tooltipRect = tooltipRoot.transform as RectTransform;
            if (rowRect == null || tooltipRect == null)
                return;

            var parentRect = tooltipRect.parent as RectTransform;
            if (parentRect == null)
                return;

            var canvas = tooltipRect.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            var rowCenterWorld = rowRect.TransformPoint(rowRect.rect.center);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    RectTransformUtility.WorldToScreenPoint(camera, rowCenterWorld),
                    camera,
                    out var localPoint))
                return;

            var pos = tooltipRect.anchoredPosition;
            tooltipRect.anchoredPosition = new Vector2(pos.x, localPoint.y);
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

            RefreshIcon();

            var step2Ready = GameplayFeatures.Step2ExpansionAvailable;
            var cost = _controller.Upgrades.GetCost(definition);
            var canAfford = _controller.Stardust >= cost;

            if (costText != null)
                costText.text = step2Ready ? $"Buy ({cost:0})" : "Soon";

            if (buyButton != null)
            {
                var canBuy = !_controller.IsRunEnded && (step2Ready ? canAfford : true);
                buyButton.interactable = canBuy;
                if (buttonImage != null)
                    buttonImage.color = canBuy ? enabledButtonColor : disabledButtonColor;
            }

            if (_hovering)
                ShowTooltip();
        }

        private void RefreshExpandCosmicRow(bool hideMaxed)
        {
            var show = _controller.GameplayMode == GameplayMode.MultiStarSystemAge;
            if (hideMaxed && definition.maxLevel > 0 && _controller.Upgrades.GetLevel(definition) >= definition.maxLevel)
                show = false;

            gameObject.SetActive(show);
            if (!show)
                return;

            if (titleText != null)
                titleText.text = definition.displayName;

            RefreshIcon();

            var step3Ready = GameplayFeatures.Step3ExpansionAvailable && _controller.Upgrades.IsUniverseExpanded;
            var cost = _controller.Upgrades.GetCost(definition);
            var canAfford = _controller.Stardust >= cost;

            if (costText != null)
                costText.text = step3Ready ? $"Buy ({cost:0})" : "Locked";

            if (buyButton != null)
            {
                var canBuy = !_controller.IsRunEnded && (step3Ready ? canAfford : false);
                buyButton.interactable = canBuy;
                if (buttonImage != null)
                    buttonImage.color = canBuy ? enabledButtonColor : disabledButtonColor;
            }

            if (_hovering)
                ShowTooltip();
        }

        private void RefreshStandardRow(bool hideMaxed)
        {
            var level = _controller.Upgrades.GetLevel(definition);
            var cost = _controller.Upgrades.GetCost(definition);
            var unlocked = definition.ArePrerequisitesMet(_controller, out _);
            var atMax = definition.maxLevel > 0 && level >= definition.maxLevel;

            if (hideMaxed && atMax)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = GetDisplayTitle(level);

            RefreshIcon();

            if (costText != null)
            {
                if (definition.upgradeType == UpgradeType.EntropyEqualization && !atMax)
                    costText.text = unlocked ? $"Activate ({cost:0})" : "Locked";
                else
                    costText.text = atMax ? "Max" : unlocked ? $"Buy ({cost:0})" : "Locked";
            }

            if (buyButton != null)
            {
                var canBuy = unlocked && !_controller.IsRunEnded && !atMax && _controller.Stardust >= cost;
                buyButton.interactable = canBuy;
                if (buttonImage != null)
                    buttonImage.color = canBuy ? enabledButtonColor : disabledButtonColor;
            }

            if (_hovering)
                ShowTooltip();
        }

        private string GetDisplayTitle(int level) => $"{definition.displayName} (Lv {level})";

        private void RefreshIcon()
        {
            if (iconImage == null || definition == null)
                return;

            iconImage.sprite = definition.iconSprite;
            iconImage.enabled = definition.iconSprite != null;
            iconImage.preserveAspect = true;
        }

        private string BuildTooltipText()
        {
            if (definition.upgradeType == UpgradeType.ExpandUniverse)
                return BuildExpandUniverseTooltip();

            if (definition.upgradeType == UpgradeType.ExpandCosmic)
                return BuildExpandCosmicTooltip();

            return BuildStandardTooltip();
        }

        private string BuildExpandUniverseTooltip()
        {
            var step2Ready = GameplayFeatures.Step2ExpansionAvailable;
            var cost = _controller.Upgrades.GetCost(definition);
            var status = step2Ready ? $"Cost: {cost:0} Stardust" : "Unlocks with Step 2 expansion";
            return $"{definition.displayName}\n{definition.description}\n\n{status}";
        }

        private string BuildExpandCosmicTooltip()
        {
            var step3Ready = GameplayFeatures.Step3ExpansionAvailable && _controller.Upgrades.IsUniverseExpanded;
            var cost = _controller.Upgrades.GetCost(definition);
            var status = step3Ready ? $"Cost: {cost:0} Stardust" : "Requires universe expansion first";
            return $"{definition.displayName}\n{definition.description}\n\n{status}";
        }

        private string BuildStandardTooltip()
        {
            var level = _controller.Upgrades.GetLevel(definition);
            var cost = _controller.Upgrades.GetCost(definition);
            var unlocked = definition.ArePrerequisitesMet(_controller, out var requirementText);
            var atMax = definition.maxLevel > 0 && level >= definition.maxLevel;
            var description = BuildDescription(level, atMax, unlocked, requirementText);
            var status = atMax ? "Maxed"
                : unlocked ? $"Cost: {cost:0} Stardust"
                : $"Locked: Requires {requirementText}";

            return $"{definition.displayName} (Lv {level})\n{description}\n\n{status}";
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
                UpgradeType.ClickPower => FormatFlat("Click Power",
                    GetCumulativeBonus(level, _controller.SingleStarBalance.upgrades.clickPowerBaseIncrement),
                    GetCumulativeBonus(nextLevel, _controller.SingleStarBalance.upgrades.clickPowerBaseIncrement)),
                UpgradeType.ClickPowerPercent => FormatPercent("Click Multiplier",
                    GetCompoundedPercent(level, _controller.SingleStarBalance.upgrades.clickRewardPercentPerLevel),
                    GetCompoundedPercent(nextLevel, _controller.SingleStarBalance.upgrades.clickRewardPercentPerLevel)),
                UpgradeType.PassiveProduction => FormatFlat("Passive Power",
                    GetCumulativeBonus(level, _controller.SingleStarBalance.upgrades.passiveProductionBaseIncrement),
                    GetCumulativeBonus(nextLevel, _controller.SingleStarBalance.upgrades.passiveProductionBaseIncrement)),
                UpgradeType.StarStability => FormatPercent("Star Age Speed",
                    GetAgeSpeedPercent(level, _controller.Upgrades.AdvancedStarStabilityLevel),
                    GetAgeSpeedPercent(nextLevel, _controller.Upgrades.AdvancedStarStabilityLevel),
                    lowerIsBetter: true),
                UpgradeType.AdvancedStarStability => FormatPercent("Star Age Speed",
                    GetAgeSpeedPercent(_controller.Upgrades.StarStabilityLevel, level),
                    GetAgeSpeedPercent(_controller.Upgrades.StarStabilityLevel, nextLevel),
                    lowerIsBetter: true),
                UpgradeType.SupernovaBonus => FormatFlat("Supernova Stardust",
                    level * _controller.SingleStarBalance.upgrades.supernovaBonusPerLevel,
                    nextLevel * _controller.SingleStarBalance.upgrades.supernovaBonusPerLevel),
                UpgradeType.ClickCollectRadius => FormatDecimal("Collect Radius",
                    _controller.SingleStarBalance.upgrades.baseClickCollectRadius +
                    level * _controller.SingleStarBalance.upgrades.clickCollectRadiusPerLevel,
                    _controller.SingleStarBalance.upgrades.baseClickCollectRadius +
                    nextLevel * _controller.SingleStarBalance.upgrades.clickCollectRadiusPerLevel),
                UpgradeType.MaxPlanetCount => FormatFlat("Max Planets",
                    _controller.SingleStarBalance.baseMaxPlanets + level,
                    _controller.SingleStarBalance.baseMaxPlanets + nextLevel),
                UpgradeType.AutoPlanetFormation => FormatPercent("Auto Planet Chance",
                    _controller.SingleStarBalance.baseAutoFormationChance +
                    level * _controller.SingleStarBalance.autoFormationChancePerLevel,
                    _controller.SingleStarBalance.baseAutoFormationChance +
                    nextLevel * _controller.SingleStarBalance.autoFormationChancePerLevel),
                UpgradeType.PlanetDnaChance => FormatPercent("Planet DNA Chance Bonus",
                    level * _controller.SingleStarBalance.planetDnaChancePerLevel,
                    nextLevel * _controller.SingleStarBalance.planetDnaChancePerLevel),
                UpgradeType.PlanetClickValue => FormatDecimal("Planet Click Stardust",
                    level * _controller.SingleStarBalance.planetClickValuePerLevel,
                    nextLevel * _controller.SingleStarBalance.planetClickValuePerLevel),
                UpgradeType.HabitablePlanetChance => FormatPercent("Habitability Chance Bonus",
                    _controller.SingleStarBalance.baseHabitableRollBonus +
                    level * _controller.SingleStarBalance.habitableChancePerLevel,
                    _controller.SingleStarBalance.baseHabitableRollBonus +
                    nextLevel * _controller.SingleStarBalance.habitableChancePerLevel),
                UpgradeType.MaxStarCount => FormatFlat("Star Slots",
                    Mathf.Clamp(_controller.SingleStarBalance.multiStar.initialStarSlots + level, 1,
                        _controller.SingleStarBalance.multiStar.maxStarSlots),
                    Mathf.Clamp(_controller.SingleStarBalance.multiStar.initialStarSlots + nextLevel, 1,
                        _controller.SingleStarBalance.multiStar.maxStarSlots)),
                UpgradeType.PlanetPassiveProduction => FormatPercent("Planet Passive Multiplier",
                    GetCompoundedPercent(level,
                        _controller.SingleStarBalance.multiStar.planetPassiveProductionPercentPerLevel),
                    GetCompoundedPercent(nextLevel,
                        _controller.SingleStarBalance.multiStar.planetPassiveProductionPercentPerLevel)),
                UpgradeType.StarPlanetClickValue => FormatDecimal("Star Click Per Planet",
                    level * _controller.SingleStarBalance.multiStar.starClickValuePerPlanetPerUpgradeLevel,
                    nextLevel * _controller.SingleStarBalance.multiStar.starClickValuePerPlanetPerUpgradeLevel),
                UpgradeType.StarPassiveProductionPercent => FormatPercent("Passive Multiplier",
                    GetCompoundedPercent(level,
                        _controller.SingleStarBalance.upgrades.starPassiveProductionPercentPerLevel),
                    GetCompoundedPercent(nextLevel,
                        _controller.SingleStarBalance.upgrades.starPassiveProductionPercentPerLevel)),
                UpgradeType.CollisionDnaProduction => FormatDecimal("Collision DNA Potential",
                    level * _controller.SingleStarBalance.upgrades.collisionDnaPotentialPerLevel,
                    nextLevel * _controller.SingleStarBalance.upgrades.collisionDnaPotentialPerLevel),
                UpgradeType.EntropyReduction => FormatPercent("Entropy Reduction",
                    Mathf.Clamp01(level * _controller.SingleStarBalance.upgrades.entropyReductionPerLevel),
                    Mathf.Clamp01(nextLevel * _controller.SingleStarBalance.upgrades.entropyReductionPerLevel)),
                UpgradeType.SpeciesDnaProduction => FormatDecimal("Species DNA Per Tick",
                    level * _controller.SingleStarBalance.upgrades.speciesDnaPotentialPerLevel,
                    nextLevel * _controller.SingleStarBalance.upgrades.speciesDnaPotentialPerLevel),
                UpgradeType.CollisionAttraction => FormatPercent("Star Drift Speed",
                    1f + level * _controller.SingleStarBalance.upgrades.collisionAttractionDriftMultiplierPerLevel,
                    1f + nextLevel * _controller.SingleStarBalance.upgrades.collisionAttractionDriftMultiplierPerLevel),
                UpgradeType.BlackHoleStabilization => FormatPercent("Black Hole Entropy",
                    1f - Mathf.Clamp01(level * _controller.SingleStarBalance.upgrades.blackHoleEntropyReductionPerLevel),
                    1f - Mathf.Clamp01(nextLevel * _controller.SingleStarBalance.upgrades.blackHoleEntropyReductionPerLevel),
                    lowerIsBetter: true),
                UpgradeType.BlackHoleMemory => FormatPercent("Black Hole DNA Bonus",
                    level * _controller.SingleStarBalance.upgrades.blackHoleMemoryDnaMultiplierPerLevel,
                    nextLevel * _controller.SingleStarBalance.upgrades.blackHoleMemoryDnaMultiplierPerLevel),
                UpgradeType.SpaceAgeDna => FormatPercent("Space Age DNA Bonus",
                    _controller.GetSpaceAgeDnaMultiplierForLevel(level) - 1f,
                    _controller.GetSpaceAgeDnaMultiplierForLevel(nextLevel) - 1f),
                UpgradeType.SpaceAgeProgression => FormatPercent("Space Age Progress Bonus",
                    _controller.GetSpaceAgeProgressionMultiplierForLevel(level) - 1f,
                    _controller.GetSpaceAgeProgressionMultiplierForLevel(nextLevel) - 1f),
                UpgradeType.CosmicEventDna => FormatPercent("Cosmic DNA Chance",
                    level * _controller.SingleStarBalance.upgrades.cosmicEventDnaChancePerLevel,
                    nextLevel * _controller.SingleStarBalance.upgrades.cosmicEventDnaChancePerLevel),
                UpgradeType.OrbitalDna => FormatPercent("Orbital DNA Bonus",
                    _controller.GetOrbitalDnaMultiplierForLevel(level) - 1f,
                    _controller.GetOrbitalDnaMultiplierForLevel(nextLevel) - 1f),
                UpgradeType.AutoStarFormation => FormatPercent("Auto Star Chance",
                    _controller.GetAutoStarFormationChanceForLevel(level),
                    _controller.GetAutoStarFormationChanceForLevel(nextLevel)),
                UpgradeType.EntropyEqualization => "Stabilizes entropy and ends the run in victory",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            return atMax ? "Max level" : line;
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

        private static string FormatFlat(string label, int current, int next) =>
            $"{label}: {current:0} -> {next:0}";

        private static string FormatDecimal(string label, float current, float next) =>
            $"{label}: {current:0.##} -> {next:0.##}";

        private static string FormatPercent(string label, float current, float next, bool lowerIsBetter = false) =>
            $"{label}: {current * 100f:0.#}% -> {next * 100f:0.#}%";

        private bool IsUpgradeTierVisible()
        {
            if (definition.upgradeTier <= 1)
                return true;

            if (_controller.IsSingleStarMode)
                return false;

            if (definition.upgradeTier >= 3)
                return _controller.Upgrades.IsCosmicExpanded ||
                       _controller.GameplayMode == GameplayMode.FullCosmic;

            return true;
        }
    }
}
