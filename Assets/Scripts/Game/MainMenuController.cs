using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Universes.Game
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Text gameNameText;
        [SerializeField] private string gameName = "Cosmic Selection";
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private GameObject settingsPanelPrefab;
        [SerializeField] private Transform settingsParent;
        [SerializeField] private string gameSceneName = "Step1";

        private void Awake()
        {
            if (gameNameText != null)
                gameNameText.text = gameName;

            newGameButton?.onClick.AddListener(OnNewGame);
            continueButton?.onClick.AddListener(OnContinue);
            settingsButton?.onClick.AddListener(OnSettings);
            exitButton?.onClick.AddListener(OnExit);

            EnsureSettingsPanel();
            RefreshContinueButton();
        }

        private void OnEnable() => RefreshContinueButton();

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (settingsPanel != null && settingsPanel.IsOpen)
                settingsPanel.Hide();
        }

        private void RefreshContinueButton()
        {
            if (continueButton != null)
                continueButton.interactable = GameSession.HasContinueSave;
        }

        private void OnNewGame()
        {
            RunSave.RequestNewGame(resetPrestige: true);
            GameSession.MarkGameStarted(gameSceneName);
            SceneManager.LoadScene(gameSceneName);
        }

        private void OnContinue()
        {
            if (!GameSession.HasContinueSave)
                return;

            var scene = GameSession.LastGameScene;
            if (string.IsNullOrWhiteSpace(scene))
                scene = gameSceneName;

            SceneManager.LoadScene(scene);
        }

        private void OnSettings()
        {
            EnsureSettingsPanel();
            settingsPanel?.Show(fromMainMenu: true);
        }

        private void OnExit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void EnsureSettingsPanel()
        {
            if (settingsPanel != null)
                return;

            settingsPanel = FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            if (settingsPanel != null)
                return;

            if (settingsPanelPrefab == null)
                return;

            var parent = settingsParent != null ? settingsParent : transform;
            var instance = Instantiate(settingsPanelPrefab, parent);
            instance.name = "SettingsPanel";
            settingsPanel = instance.GetComponent<SettingsPanel>();
        }
    }
}
