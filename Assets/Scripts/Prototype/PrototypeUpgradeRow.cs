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
            if (_controller != null && definition != null && _controller.TryPurchaseUpgrade(definition))
                Refresh();
        }

        public void Refresh()
        {
            if (_controller == null || definition == null)
                return;

            var level = _controller.Upgrades.GetLevel(definition);
            var cost = _controller.Upgrades.GetCost(definition);

            if (titleText != null)
                titleText.text = $"{definition.displayName} (Lv {level})";

            if (costText != null)
                costText.text = $"Buy ({cost:0})";

            if (buyButton != null)
            {
                var canBuy = !_controller.IsCollapsed && _controller.Stardust >= cost;
                buyButton.interactable = canBuy;
                if (buttonImage != null)
                    buttonImage.color = canBuy ? enabledButtonColor : disabledButtonColor;
            }
        }
    }
}
