using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Universes.Game
{
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Slider effectsSlider;
        [SerializeField] private Text effectsLabelText;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Text musicLabelText;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text closeButtonText;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private bool pauseGameWhenOpen = true;

        private bool _pausedByUs;
        private float _previousTimeScale = 1f;
        private bool _updatingSliders;
        private bool _wired;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            WireUi();
            HideImmediate();
        }

        public void ConfigureRuntime(
            GameObject root,
            Slider effects,
            Text effectsLabel,
            Slider music,
            Text musicLabel,
            Button mainMenu,
            Button close,
            Text closeLabel)
        {
            panelRoot = root;
            effectsSlider = effects;
            effectsLabelText = effectsLabel;
            musicSlider = music;
            musicLabelText = musicLabel;
            mainMenuButton = mainMenu;
            closeButton = close;
            closeButtonText = closeLabel;
            WireUi();
        }

        private void WireUi()
        {
            if (_wired)
                return;

            if (effectsSlider != null)
            {
                effectsSlider.minValue = 0f;
                effectsSlider.maxValue = 1f;
                effectsSlider.wholeNumbers = false;
                effectsSlider.onValueChanged.RemoveListener(OnEffectsVolumeChanged);
                effectsSlider.onValueChanged.AddListener(OnEffectsVolumeChanged);
            }

            if (musicSlider != null)
            {
                musicSlider.minValue = 0f;
                musicSlider.maxValue = 1f;
                musicSlider.wholeNumbers = false;
                musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (effectsSlider == null && musicSlider == null && mainMenuButton == null && closeButton == null)
                return;

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(GoToMainMenu);
                mainMenuButton.onClick.AddListener(GoToMainMenu);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }

            _wired = true;
        }

        private void OnEnable()
        {
            GameAudioSettings.Changed += RefreshUi;
        }

        private void OnDisable()
        {
            GameAudioSettings.Changed -= RefreshUi;
            RestoreTimeScale();
        }

        private void OnDestroy()
        {
            RestoreTimeScale();
        }

        public void Show(bool fromMainMenu = false)
        {
            WireUi();

            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (mainMenuButton != null)
                mainMenuButton.gameObject.SetActive(!fromMainMenu);

            if (closeButtonText != null)
                closeButtonText.text = fromMainMenu ? "Close" : "Resume";

            if (pauseGameWhenOpen && !fromMainMenu)
            {
                _previousTimeScale = Time.timeScale;
                if (_previousTimeScale <= 0f)
                    _previousTimeScale = 1f;
                Time.timeScale = 0f;
                _pausedByUs = true;
            }

            RefreshUi();
        }

        public void Hide()
        {
            RestoreTimeScale();
            HideImmediate();
        }

        public void Toggle(bool fromMainMenu = false)
        {
            if (IsOpen)
                Hide();
            else
                Show(fromMainMenu);
        }

        private void HideImmediate()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void RestoreTimeScale()
        {
            if (!_pausedByUs)
                return;

            Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
            _pausedByUs = false;
        }

        private void OnEffectsVolumeChanged(float value)
        {
            if (_updatingSliders)
                return;

            GameAudioSettings.EffectsVolume = value;
            ApplyAudioToSfxManager();
            RefreshLabels();
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (_updatingSliders)
                return;

            GameAudioSettings.MusicVolume = value;
            ApplyAudioToSfxManager();
            RefreshLabels();
        }

        private void RefreshUi()
        {
            _updatingSliders = true;

            if (effectsSlider != null)
                effectsSlider.SetValueWithoutNotify(GameAudioSettings.EffectsVolume);

            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(GameAudioSettings.MusicVolume);

            _updatingSliders = false;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (effectsLabelText != null)
                effectsLabelText.text = $"Effects Sound: {Mathf.RoundToInt(GameAudioSettings.EffectsVolume * 100f)}%";

            if (musicLabelText != null)
                musicLabelText.text = $"Music: {Mathf.RoundToInt(GameAudioSettings.MusicVolume * 100f)}%";
        }

        private static void ApplyAudioToSfxManager()
        {
            var sfx = FindAnyObjectByType<SfxManager>();
            sfx?.ApplyAudioSettings();
        }

        private void GoToMainMenu()
        {
            RestoreTimeScale();

            var controller = FindAnyObjectByType<GameController>();
            controller?.SaveCurrentRun();

            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
                return;

            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
