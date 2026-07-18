using UnityEngine;
using UnityEngine.UI;

namespace Universes.Game
{
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private UpgradeRow[] upgradeRows;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private bool hideMaxedUpgrades;
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private Text tooltipText;

        private void Start()
        {
            if (controller == null)
                controller = FindAnyObjectByType<GameController>();

            if (panelRoot == null)
                panelRoot = gameObject;

            if (upgradeRows == null || upgradeRows.Length == 0)
                upgradeRows = GetComponentsInChildren<UpgradeRow>(true);

            EnsureTooltip();

            foreach (var row in upgradeRows)
            {
                if (row == null)
                    continue;

                row.ConfigureTooltip(tooltipRoot, tooltipText);
                row.Initialize(controller);
            }

            if (controller != null)
            {
                controller.OnStateChanged += OnStateChanged;
                controller.OnUniverseCollapsed += OnUniverseCollapsed;
            }

            SetVisible(controller == null || !controller.IsRunEnded);
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.OnStateChanged -= OnStateChanged;
                controller.OnUniverseCollapsed -= OnUniverseCollapsed;
            }
        }

        private void OnStateChanged()
        {
            if (controller == null)
                return;

            if (!controller.IsRunEnded)
                SetVisible(true);

            RefreshAll();
        }

        private void OnUniverseCollapsed(bool _) => SetVisible(false);

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
                panelRoot.SetActive(visible);
        }

        private void RefreshAll()
        {
            if (tooltipRoot != null)
                tooltipRoot.SetActive(false);

            if (upgradeRows == null)
                return;

            foreach (var row in upgradeRows)
            {
                if (row != null)
                    row.Refresh(hideMaxedUpgrades);
            }
        }

        public void SetHideMaxedUpgrades(bool hide)
        {
            hideMaxedUpgrades = hide;
            RefreshAll();
        }

        public void ToggleHideMaxedUpgrades()
        {
            hideMaxedUpgrades = !hideMaxedUpgrades;
            RefreshAll();
        }

        private void EnsureTooltip()
        {
            if (tooltipRoot != null && tooltipText != null)
                return;

            var parent = panelRoot != null ? panelRoot.transform : transform;
            tooltipRoot = new GameObject("UpgradeTooltip", typeof(RectTransform), typeof(Image));
            tooltipRoot.transform.SetParent(parent, false);
            var tooltipRect = tooltipRoot.GetComponent<RectTransform>();
            tooltipRect.anchorMin = new Vector2(0f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0f, 0.5f);
            tooltipRect.pivot = new Vector2(1f, 0.5f);
            tooltipRect.anchoredPosition = new Vector2(12f, 0f);
            tooltipRect.sizeDelta = new Vector2(300f, 220f);
            var tooltipImage = tooltipRoot.GetComponent<Image>();
            tooltipImage.color = new Color(0.04f, 0.055f, 0.08f, 0.96f);
            tooltipImage.raycastTarget = false;

            var textGo = new GameObject("TooltipText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(tooltipRoot.transform, false);
            tooltipText = textGo.GetComponent<Text>();
            tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tooltipText.fontSize = 14;
            tooltipText.alignment = TextAnchor.UpperLeft;
            tooltipText.color = new Color(0.85f, 0.92f, 1f);
            tooltipText.raycastTarget = false;
            var textRect = tooltipText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 10f);
            textRect.offsetMax = new Vector2(-12f, -10f);

            tooltipRoot.SetActive(false);
        }
    }
}
