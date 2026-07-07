using UnityEngine;
using UnityEngine.UI;

namespace Universes.Prototype
{
    public class PrototypeUpgradeRow : MonoBehaviour
    {
        [SerializeField] private PrototypeUpgradeDefinition definition;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
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

        public void Refresh()
        {
            if (_controller == null || definition == null)
                return;

            if (definition.upgradeType == PrototypeUpgradeType.ExpandUniverse)
            {
                RefreshExpandUniverseRow();
                return;
            }

            RefreshStandardRow();
        }

        private void RefreshExpandUniverseRow()
        {
            var show = _controller.IsSingleStarMode && !_controller.Upgrades.IsUniverseExpanded;
            gameObject.SetActive(show);
            if (!show)
                return;

            if (titleText != null)
                titleText.text = definition.displayName;

            if (descriptionText != null && !string.IsNullOrEmpty(definition.description))
                descriptionText.text = definition.description;

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

        private void RefreshStandardRow()
        {
            var level = _controller.Upgrades.GetLevel(definition);
            var cost = _controller.Upgrades.GetCost(definition);
            var unlocked = definition.ArePrerequisitesMet(_controller, out var requirementText);

            if (titleText != null)
                titleText.text = $"{definition.displayName} (Lv {level})";

            if (descriptionText != null)
            {
                descriptionText.text = unlocked || string.IsNullOrWhiteSpace(requirementText)
                    ? definition.description
                    : $"{definition.description}\nLocked: Requires {requirementText}";
            }

            if (costText != null)
                costText.text = unlocked ? $"Buy ({cost:0})" : "Locked";

            if (buyButton != null)
            {
                var atMax = definition.maxLevel > 0 && level >= definition.maxLevel;
                var canBuy = unlocked && !_controller.IsRunEnded && !atMax && _controller.Stardust >= cost;
                buyButton.interactable = canBuy;
                if (buttonImage != null)
                    buttonImage.color = canBuy ? enabledButtonColor : disabledButtonColor;
            }
        }
    }
}
