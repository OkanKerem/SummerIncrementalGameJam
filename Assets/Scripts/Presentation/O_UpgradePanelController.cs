using Universes.Core;
using Universes.Prestige;
using UnityEngine;
using UnityEngine.UI;

namespace Universes.Presentation
{
    public class O_UpgradePanelController : MonoBehaviour
    {
        [SerializeField] private O_UniverseRunController runController;
        [SerializeField] private O_UpgradeCatalog catalog;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private GameObject upgradeRowPrefab;

        private void Start()
        {
            if (runController == null)
                runController = FindAnyObjectByType<O_UniverseRunController>();

            if (catalog == null || contentRoot == null)
                return;

            BuildUpgradeList();
            runController.Prestige.OnChanged += RefreshAll;
        }

        private void BuildUpgradeList()
        {
            foreach (Transform child in contentRoot)
                Destroy(child.gameObject);

            if (catalog.upgrades == null)
                return;

            foreach (var def in catalog.upgrades)
            {
                if (def == null)
                    continue;

                var row = upgradeRowPrefab != null
                    ? Instantiate(upgradeRowPrefab, contentRoot)
                    : CreateDefaultRow(contentRoot);

                var controller = row.GetComponent<O_UpgradeRowView>();
                if (controller == null)
                    controller = row.AddComponent<O_UpgradeRowView>();

                controller.Setup(def, runController, RefreshAll);
            }
        }

        private GameObject CreateDefaultRow(Transform parent)
        {
            var row = new GameObject("UpgradeRow");
            row.transform.SetParent(parent, false);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.spacing = 8;
            var rect = row.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var label = labelGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.color = Color.white;

            var btnGo = new GameObject("Buy");
            btnGo.transform.SetParent(row.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(80, 30);
            var btn = btnGo.AddComponent<Button>();
            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.6f);

            var btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 12;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            var btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            return row;
        }

        private void RefreshAll()
        {
            if (contentRoot == null)
                return;

            foreach (Transform child in contentRoot)
            {
                var view = child.GetComponent<O_UpgradeRowView>();
                view?.Refresh();
            }
        }
    }

    public class O_UpgradeRowView : MonoBehaviour
    {
        private O_UpgradeDefinition _def;
        private O_UniverseRunController _run;
        private System.Action _onRefresh;
        private Text _label;
        private Button _button;
        private Text _buttonText;

        public void Setup(O_UpgradeDefinition def, O_UniverseRunController run, System.Action onRefresh)
        {
            _def = def;
            _run = run;
            _onRefresh = onRefresh;

            var texts = GetComponentsInChildren<Text>();
            if (texts.Length > 0)
                _label = texts[0];
            if (texts.Length > 1)
                _buttonText = texts[1];

            _button = GetComponentInChildren<Button>();
            if (_button != null)
                _button.onClick.AddListener(TryBuy);

            Refresh();
        }

        private void TryBuy()
        {
            if (_run.Prestige.TryPurchaseUpgrade(_def, _run.Balance))
            {
                _run.RefreshModifiers();
                _onRefresh?.Invoke();
            }
        }

        public void Refresh()
        {
            if (_def == null || _run == null)
                return;

            var level = _run.Prestige.GetUpgradeLevel(_def.id);
            var cost = _def.GetCost(level);
            var maxed = level >= _def.maxLevel;

            if (_label != null)
                _label.text = $"{_def.displayName} (Lv {level}/{_def.maxLevel})\n{_def.description}";

            if (_buttonText != null)
                _buttonText.text = maxed ? "MAX" : O_NumberFormatHelper.Format(cost);

            if (_button != null)
                _button.interactable = !maxed && _run.Prestige.UniverseDna >= cost;
        }
    }
}
