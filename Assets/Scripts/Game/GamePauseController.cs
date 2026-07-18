using UnityEngine;
using UnityEngine.UI;

namespace Universes.Game
{
    public class GamePauseController : MonoBehaviour
    {
        private const string SettingsResourcePath = "UI/SettingsPanel";

        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private GameObject settingsPanelPrefab;
        [SerializeField] private Transform settingsParent;
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        public static GamePauseController EnsureExists()
        {
            var existing = FindAnyObjectByType<GamePauseController>();
            if (existing != null)
                return existing;

            var canvas = FindAnyObjectByType<Canvas>();
            var host = canvas != null ? canvas.gameObject : new GameObject("PauseController");
            if (canvas == null)
                DontDestroyOnLoad(host);

            return host.GetComponent<GamePauseController>() ?? host.AddComponent<GamePauseController>();
        }

        private void Awake()
        {
            EnsureSettingsPanel();
        }

        private void Start()
        {
            GameSession.MarkGameStarted(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            EnsureSettingsPanel();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(pauseKey))
                return;

            // Don't steal Esc while typing in input fields.
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null)
            {
                var selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
                if (selected.GetComponent<InputField>() != null)
                    return;
            }

            EnsureSettingsPanel();
            if (settingsPanel == null)
                return;

            settingsPanel.Toggle(fromMainMenu: false);
        }

        private void EnsureSettingsPanel()
        {
            if (settingsPanel != null)
                return;

            settingsPanel = FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            if (settingsPanel != null)
                return;

            if (settingsPanelPrefab == null)
                settingsPanelPrefab = Resources.Load<GameObject>(SettingsResourcePath);

#if UNITY_EDITOR
            if (settingsPanelPrefab == null)
            {
                settingsPanelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/UI/SettingsPanel.prefab");
            }
#endif

            var parent = settingsParent;
            if (parent == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                parent = canvas != null ? canvas.transform : transform;
            }

            if (settingsPanelPrefab != null)
            {
                var instance = Instantiate(settingsPanelPrefab, parent);
                instance.name = "SettingsPanel";
                settingsPanel = instance.GetComponent<SettingsPanel>();
                if (settingsPanel != null)
                    return;
            }

            settingsPanel = BuildFallbackSettingsPanel(parent);
        }

        private static SettingsPanel BuildFallbackSettingsPanel(Transform parent)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("SettingsPanel", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            root.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.88f);

            var card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(root.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(440f, 380f);
            card.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.16f, 0.98f);

            var titleGo = CreateLabel(card.transform, "Settings", font, 28, new Vector2(0f, 150f), new Vector2(360f, 40f));
            titleGo.fontStyle = FontStyle.Bold;

            var effectsLabel = CreateLabel(card.transform, "Effects Sound: 100%", font, 16,
                new Vector2(0f, 90f), new Vector2(320f, 24f));
            var effectsSlider = CreateSlider(card.transform, "EffectsSlider", new Vector2(0f, 55f));
            var musicLabel = CreateLabel(card.transform, "Music: 100%", font, 16,
                new Vector2(0f, 10f), new Vector2(320f, 24f));
            var musicSlider = CreateSlider(card.transform, "MusicSlider", new Vector2(0f, -25f));

            var mainMenuBtn = CreateButton(card.transform, "Main Menu", font, new Vector2(0f, -90f),
                new Color(0.35f, 0.2f, 0.22f));
            var closeBtn = CreateButton(card.transform, "Resume", font, new Vector2(0f, -150f),
                new Color(0.2f, 0.45f, 0.35f));

            var panel = root.AddComponent<SettingsPanel>();
            panel.ConfigureRuntime(
                root,
                effectsSlider,
                effectsLabel,
                musicSlider,
                musicLabel,
                mainMenuBtn,
                closeBtn,
                closeBtn.GetComponentInChildren<Text>());

            root.SetActive(false);
            return panel;
        }

        private static Text CreateLabel(Transform parent, string text, Font font, int size,
            Vector2 position, Vector2 sizeDelta)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;
            var label = go.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            return label;
        }

        private static Button CreateButton(Transform parent, string text, Font font, Vector2 position,
            Color color)
        {
            var go = new GameObject(text.Replace(" ", string.Empty) + "Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300f, 44f);
            go.AddComponent<Image>().color = color;
            var button = go.AddComponent<Button>();
            var label = CreateLabel(go.transform, text, font, 18, Vector2.zero, Vector2.zero);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 position)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = new Vector2(320f, 22f);

            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(root.transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(6f, 4f);
            fillAreaRect.offsetMax = new Vector2(-6f, -4f);

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 0.95f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8f, 0f);
            handleAreaRect.offsetMax = new Vector2(-8f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 18f);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.9f, 0.95f, 1f);

            var slider = root.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }
    }
}
