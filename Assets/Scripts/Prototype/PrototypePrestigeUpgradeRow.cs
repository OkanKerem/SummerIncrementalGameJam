using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Universes.Prototype
{
    public class PrototypePrestigeUpgradeRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private PrototypePrestigeUpgradeDefinition definition;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text effectText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text costText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image buttonImage;
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private Text tooltipText;
        [SerializeField] private Color enabledButtonColor = new(0.22f, 0.38f, 0.28f);
        [SerializeField] private Color disabledButtonColor = new(0.15f, 0.15f, 0.18f);

        private PrototypeGameController _controller;
        private PrototypePrestigeState _prestige;
        private bool _allowPurchase;
        private bool _hovering;

        public PrototypePrestigeUpgradeDefinition Definition => definition;

        public void ConfigureTooltip(GameObject root, Text text)
        {
            tooltipRoot = root;
            tooltipText = text;
        }

        public void Initialize(PrototypeGameController controller)
        {
            _controller = controller;
            _prestige = controller != null ? controller.Prestige : null;
            _allowPurchase = false;
            InitializeCommon();
        }

        public void Initialize(PrototypePrestigeState prestige, bool allowPurchase)
        {
            _controller = null;
            _prestige = prestige;
            _allowPurchase = allowPurchase;
            InitializeCommon();
        }

        private void InitializeCommon()
        {
            if (definition == null)
            {
                Debug.LogWarning($"PrototypePrestigeUpgradeRow on {name} has no definition assigned.", this);
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
            if (definition == null)
                return;

            if (_controller != null && _controller.TryPurchasePrestigeUpgrade(definition))
            {
                Refresh();
                return;
            }

            if (_controller == null && _prestige != null && _allowPurchase && _prestige.TryPurchase(definition))
                Refresh();
        }

        public void Refresh()
        {
            if (_prestige == null || definition == null)
                return;

            var level = _prestige.GetLevel(definition);
            var cost = _prestige.GetCost(definition);
            var maxed = definition.maxLevel > 0 && level >= definition.maxLevel;
            var unlocked = definition.ArePrerequisitesMet(_prestige, out var requirementText);

            if (titleText != null)
                titleText.text = $"{definition.displayName} (Lv {level})";

            RefreshIcon();

            if (descriptionText != null)
            {
                descriptionText.text = unlocked || string.IsNullOrWhiteSpace(requirementText)
                    ? definition.description
                    : $"{definition.description}\nLocked: Requires {requirementText}";
            }

            if (effectText != null)
                effectText.text = PrototypePrestigeModifiers.DescribeEffect(definition, level);

            if (costText != null)
                costText.text = maxed ? "Maxed" : unlocked ? $"Buy ({cost} DNA)" : "Locked";

            if (buyButton != null)
            {
                var canBuy = (_allowPurchase || (_controller != null && _controller.IsRunEnded)) &&
                             unlocked &&
                             !maxed &&
                             _prestige.UniverseDna >= cost;
                buyButton.interactable = canBuy;
                if (buttonImage != null)
                    buttonImage.color = canBuy ? enabledButtonColor : disabledButtonColor;
            }

            if (_hovering)
                ShowTooltip();
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
            if (tooltipRoot == null || tooltipText == null || _prestige == null || definition == null)
                return;

            tooltipText.text = BuildTooltipText();
            tooltipRoot.SetActive(true);
        }

        private string BuildTooltipText()
        {
            var level = _prestige.GetLevel(definition);
            var cost = _prestige.GetCost(definition);
            var maxed = definition.maxLevel > 0 && level >= definition.maxLevel;
            var unlocked = definition.ArePrerequisitesMet(_prestige, out var requirementText);
            var effect = PrototypePrestigeModifiers.DescribeEffect(definition, level);
            var status = maxed ? "Maxed" : unlocked ? $"Cost: {cost} Universe DNA" : $"Locked: Requires {requirementText}";

            return $"{definition.displayName} (Lv {level})\n" +
                   $"{definition.description}\n\n" +
                   $"{effect}\n" +
                   status;
        }

        private void RefreshIcon()
        {
            if (iconImage == null || definition == null)
                return;

            iconImage.sprite = definition.iconSprite;
            iconImage.enabled = definition.iconSprite != null;
            iconImage.preserveAspect = true;
        }
    }
}
