using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Universes.Game
{
    public class PrestigePanel : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private PrestigeUpgradeRow[] upgradeRows;
        [SerializeField] private bool standaloneSceneMode;
        [SerializeField] private Text universeDnaText;
        [SerializeField] private Button backToGameButton;
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private Text tooltipText;
        [SerializeField] private string gameSceneName = "GameScene";

        private readonly PrestigeState _standalonePrestige = new();
        private const string ReturnSceneKey = "PrototypePrestigeReturnScene";
        private PrestigeTreeConnection[] _treeConnections;
        private PrestigeState ActivePrestige => standaloneSceneMode ? _standalonePrestige : controller?.Prestige;

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<GameController>();

            if (panelRoot == null)
                panelRoot = gameObject;
        }

        private void Start()
        {
            if (standaloneSceneMode)
                PrestigeSave.Load(_standalonePrestige);

            if (upgradeRows == null || upgradeRows.Length == 0)
                upgradeRows = GetComponentsInChildren<PrestigeUpgradeRow>(true);

            EnsureTreeConnections();
            EnsureTooltip();

            foreach (var row in upgradeRows)
            {
                if (row == null)
                    continue;

                row.ConfigureTooltip(tooltipRoot, tooltipText);

                if (standaloneSceneMode)
                    row.Initialize(_standalonePrestige, allowPurchase: true);
                else
                    row.Initialize(controller);
            }

            if (standaloneSceneMode)
            {
                _standalonePrestige.OnChanged += RefreshAll;
                backToGameButton?.onClick.AddListener(LoadGameScene);
            }
            else if (controller != null)
            {
                controller.Prestige.OnChanged += RefreshAll;
                controller.OnStateChanged += OnStateChanged;
            }

            if (standaloneSceneMode)
                Show();
            else
            {
                RefreshAll();
                Hide();
            }
        }

        private void OnDestroy()
        {
            if (standaloneSceneMode)
            {
                _standalonePrestige.OnChanged -= RefreshAll;
                if (backToGameButton != null)
                    backToGameButton.onClick.RemoveListener(LoadGameScene);
                return;
            }

            if (controller != null)
            {
                controller.Prestige.OnChanged -= RefreshAll;
                controller.OnStateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged()
        {
            if (controller != null && !controller.IsRunEnded)
                Hide();
        }

        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            RefreshAll();
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void RefreshAll()
        {
            if (universeDnaText != null && ActivePrestige != null)
                universeDnaText.text = $"Universe DNA: {ActivePrestige.UniverseDna:0}";

            if (tooltipRoot != null)
                tooltipRoot.SetActive(false);

            if (upgradeRows == null)
                return;

            var prestige = ActivePrestige;
            var definitions = BuildDefinitionMap();

            foreach (var row in upgradeRows)
            {
                if (row == null || row.Definition == null)
                    continue;

                var visible = PrestigeTreeVisibility.IsNodeVisible(prestige, row.Definition);
                row.gameObject.SetActive(visible);
                if (visible)
                    row.Refresh();
            }

            RefreshTreeConnections(prestige, definitions);
        }

        private Dictionary<PrestigeUpgradeType, PrestigeUpgradeDefinition> BuildDefinitionMap()
        {
            var map = new Dictionary<PrestigeUpgradeType, PrestigeUpgradeDefinition>();
            if (upgradeRows == null)
                return map;

            foreach (var row in upgradeRows)
            {
                if (row?.Definition == null)
                    continue;

                map[row.Definition.upgradeType] = row.Definition;
            }

            return map;
        }

        private void EnsureTreeConnections()
        {
            var treeRoot = transform.Find("PrestigeScroll/Viewport/Content/TreeConnections");
            if (treeRoot == null)
            {
                _treeConnections = GetComponentsInChildren<PrestigeTreeConnection>(true);
                return;
            }

            foreach (Transform child in treeRoot)
            {
                if (child.GetComponent<PrestigeTreeConnection>() != null)
                    continue;

                if (!TryParseConnectionName(child.name, out var fromType, out var toType))
                    continue;

                var connection = child.gameObject.AddComponent<PrestigeTreeConnection>();
                connection.Configure(fromType, toType);
            }

            _treeConnections = GetComponentsInChildren<PrestigeTreeConnection>(true);
        }

        private void RefreshTreeConnections(PrestigeState prestige,
            Dictionary<PrestigeUpgradeType, PrestigeUpgradeDefinition> definitions)
        {
            if (_treeConnections == null || _treeConnections.Length == 0)
                _treeConnections = GetComponentsInChildren<PrestigeTreeConnection>(true);

            foreach (var connection in _treeConnections)
            {
                if (connection == null)
                    continue;

                definitions.TryGetValue(connection.FromType, out var fromDefinition);
                definitions.TryGetValue(connection.ToType, out var toDefinition);
                connection.Refresh(prestige, fromDefinition, toDefinition);
            }
        }

        private static bool TryParseConnectionName(string name, out PrestigeUpgradeType fromType,
            out PrestigeUpgradeType toType)
        {
            fromType = default;
            toType = default;
            const string separator = "_To_";
            var index = name.IndexOf(separator, System.StringComparison.Ordinal);
            if (index < 0)
                return false;

            var fromName = name.Substring(0, index);
            var toName = name.Substring(index + separator.Length);
            return System.Enum.TryParse(fromName, out fromType) &&
                   System.Enum.TryParse(toName, out toType);
        }

        private void EnsureTooltip()
        {
            if (tooltipRoot != null && tooltipText != null)
                return;

            var parent = panelRoot != null ? panelRoot.transform : transform;
            tooltipRoot = new GameObject("PrestigeTooltip", typeof(RectTransform), typeof(Image));
            tooltipRoot.transform.SetParent(parent, false);
            var tooltipRect = tooltipRoot.GetComponent<RectTransform>();
            tooltipRect.anchorMin = new Vector2(1f, 0.5f);
            tooltipRect.anchorMax = new Vector2(1f, 0.5f);
            tooltipRect.pivot = new Vector2(1f, 0.5f);
            tooltipRect.anchoredPosition = new Vector2(-12f, 0f);
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

        private void LoadGameScene()
        {
            var sceneName = PlayerPrefs.GetString(ReturnSceneKey, gameSceneName);
            if (!string.IsNullOrWhiteSpace(sceneName))
                SceneManager.LoadScene(sceneName);
        }
    }
}
