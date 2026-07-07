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

        public PrototypePrestigeUpgradeDefinition Definition => definition;

        public void Initialize(PrototypeGameController controller)
        {
            _controller = controller;

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
            if (_controller != null && definition != null && _controller.TryPurchasePrestigeUpgrade(definition))
                Refresh();
        }

        public void Refresh()
        {
            if (_controller == null || definition == null)
                return;

            var prestige = _controller.Prestige;
            var level = prestige.GetLevel(definition);
            var cost = prestige.GetCost(definition);
            var maxed = definition.maxLevel > 0 && level >= definition.maxLevel;

            if (titleText != null)
                titleText.text = $"{definition.displayName} (Lv {level})";

            if (effectText != null)
                effectText.text = PrototypePrestigeModifiers.DescribeEffect(definition, level);

            if (costText != null)
                costText.text = maxed ? "Maxed" : $"Buy ({cost} DNA)";

            if (buyButton != null)
            {
                var canBuy = _controller.IsRunEnded && !maxed && prestige.UniverseDna >= cost;
                buyButton.interactable = canBuy;
                if (buttonImage != null)
                    buttonImage.color = canBuy ? enabledButtonColor : disabledButtonColor;
            }
        }
    }
}
