using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Universes.Prototype
{
    public class PrototypePrestigePanel : MonoBehaviour
    {
        [SerializeField] private PrototypeGameController controller;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private PrototypePrestigeUpgradeRow[] upgradeRows;
        [SerializeField] private bool standaloneSceneMode;
        [SerializeField] private Text universeDnaText;
        [SerializeField] private Button backToGameButton;
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private Text tooltipText;
        [SerializeField] private string gameSceneName = "PrototypeScene";

        private readonly PrototypePrestigeState _standalonePrestige = new();
        private const string ReturnSceneKey = "PrototypePrestigeReturnScene";
        private PrototypePrestigeState ActivePrestige => standaloneSceneMode ? _standalonePrestige : controller?.Prestige;

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<PrototypeGameController>();

            if (panelRoot == null)
                panelRoot = gameObject;
        }

        private void Start()
        {
            if (standaloneSceneMode)
                PrototypePrestigeSave.Load(_standalonePrestige);

            if (upgradeRows == null || upgradeRows.Length == 0)
                upgradeRows = GetComponentsInChildren<PrototypePrestigeUpgradeRow>(true);

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
                Hide();
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

            if (upgradeRows == null)
                return;

            foreach (var row in upgradeRows)
            {
                if (row != null)
                    row.Refresh();
            }
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
