using Universes.Core;
using Universes.Prestige;
using UnityEngine;
using UnityEngine.UI;

namespace Universes.Presentation
{
    public class O_VariantPickerController : MonoBehaviour
    {
        [SerializeField] private O_UniverseRunController runController;
        [SerializeField] private O_VariantCatalog catalog;
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform cardsRoot;

        private void Start()
        {
            if (runController == null)
                runController = FindAnyObjectByType<O_UniverseRunController>();

            if (panel != null)
                panel.SetActive(false);

            BuildCards();
        }

        private void BuildCards()
        {
            if (catalog?.variants == null || cardsRoot == null)
                return;

            foreach (Transform child in cardsRoot)
                Destroy(child.gameObject);

            foreach (var variant in catalog.variants)
            {
                if (variant == null)
                    continue;

                var card = CreateCard(variant);
                card.transform.SetParent(cardsRoot, false);
            }
        }

        private GameObject CreateCard(O_ParallelVariant variant)
        {
            var card = new GameObject(variant.displayName);
            var rect = card.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 180);

            var img = card.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.22f);

            var btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectVariant(variant.id));

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(card.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 13;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = true;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            text.text =
                $"<b>{variant.displayName}</b>\n\n" +
                $"<color=#88FF88>Bonus:</color> {variant.bonusDescription}\n\n" +
                $"<color=#FF8888>Drawback:</color> {variant.drawbackDescription}";

            return card;
        }

        public void Show()
        {
            if (panel != null)
                panel.SetActive(true);
        }

        private void SelectVariant(string variantId)
        {
            runController.SelectVariant(variantId);

            if (panel != null)
                panel.SetActive(false);
        }
    }
}
