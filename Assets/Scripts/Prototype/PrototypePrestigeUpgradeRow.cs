using UnityEngine;
using UnityEngine.UI;

namespace Universes.Prototype
{
    public class PrototypePrestigeUpgradeRow : MonoBehaviour
    {
        [SerializeField] private PrototypePrestigeUpgradeDefinition definition;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text effectText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text costText;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Color enabledButtonColor = new(0.22f, 0.38f, 0.28f);
        [SerializeField] private Color disabledButtonColor = new(0.15f, 0.15f, 0.18f);

        private PrototypeGameController _controller;
        private PrototypePrestigeState _prestige;
        private bool _allowPurchase;

        public PrototypePrestigeUpgradeDefinition Definition => definition;

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

            if (titleText != null)
                titleText.text = $"{definition.displayName} (Lv {level})";

            if (effectText != null)
                effectText.text = PrototypePrestigeModifiers.DescribeEffect(definition, level);

            if (costText != null)
                costText.text = maxed ? "Maxed" : $"Buy ({cost} DNA)";

            if (buyButton != null)
            {
                var canBuy = (_allowPurchase || (_controller != null && _controller.IsRunEnded)) &&
                             !maxed &&
                             _prestige.UniverseDna >= cost;
                buyButton.interactable = canBuy;
                if (buttonImage != null)
                    buttonImage.color = canBuy ? enabledButtonColor : disabledButtonColor;
            }
        }
    }
}
