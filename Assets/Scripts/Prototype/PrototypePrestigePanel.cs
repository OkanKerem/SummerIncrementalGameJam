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

            foreach (var row in upgradeRows)
            {
                if (row == null)
                    continue;

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

        private void LoadGameScene()
        {
            var sceneName = PlayerPrefs.GetString(ReturnSceneKey, gameSceneName);
            if (!string.IsNullOrWhiteSpace(sceneName))
                SceneManager.LoadScene(sceneName);
        }
    }
}
