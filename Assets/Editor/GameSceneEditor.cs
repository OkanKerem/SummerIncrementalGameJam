#if UNITY_EDITOR
using System.IO;
using Universes.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Universes.Editor
{
    public static class GameSceneEditor
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string PrestigeScenePath = "Assets/Scenes/PrestigeScene.unity";
        private const string PrestigeSceneName = "PrestigeScene";
        private const string GameSceneName = "GameScene";
        private const string PrefabPath = "Assets/Prefabs/Star.prefab";
        private const string UpgradeRowPrefabPath = "Assets/Prefabs/UpgradeRow.prefab";
        private const string UpgradePanelPrefabPath = "Assets/Prefabs/UpgradePanel.prefab";
        private const string StardustParticlePrefabPath = "Assets/Prefabs/StardustParticle.prefab";
        private const string DnaParticlePrefabPath = "Assets/Prefabs/DnaParticle.prefab";
        private const string ClickEffectPrefabPath = "Assets/Prefabs/ClickEffect.prefab";
        private const string StardustEmitEffectPrefabPath = "Assets/Prefabs/StardustEmitEffect.prefab";
        private const string SupernovaEffectPrefabPath = "Assets/Prefabs/SupernovaEffect.prefab";
        private const string UpgradeFolder = "Assets/ScriptableObjects";
        private const string PrestigeFolder = "Assets/ScriptableObjects/Prestige";
        private const string PrestigeRowPrefabPath = "Assets/Prefabs/UI/PrestigeRow.prefab";
        private const string PrestigePanelPrefabPath = "Assets/Prefabs/UI/PrestigePanel.prefab";
        private const string PlanetPrefabPath = "Assets/Prefabs/Planet.prefab";
        private const string SpeciesEntryPrefabPath = "Assets/Prefabs/SpeciesEntry.prefab";
        private const string PlanetTypesFolder = "Assets/ScriptableObjects/Planets";
        private const string PlanetTypeCatalogPath = "Assets/ScriptableObjects/Planets/PlanetTypeCatalog.asset";
        private const string SingleStarBalancePath = "Assets/ScriptableObjects/SingleStarBalance.asset";
        private const string SunSpritePath = "Assets/Art/Sprites/Sun/sun1.png";
        private const string Sun2SpritePath = "Assets/Art/Sprites/Sun/sun2.png";
        private const string ArtPath = "Assets/Art/Sprites";
        private static readonly Vector2 PrestigeTreeContentSize = new(760f, 920f);
        private static readonly Color PrestigeTreeLineColor = new(0.28f, 0.5f, 0.68f, 0.65f);

        [MenuItem("Universes/Add Step 4 UI To Open Scene")]
        public static void AddStep4UiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need Game and Canvas in the open scene.");
                return;
            }

            var hud = canvas.GetComponent<HUD>();
            if (hud == null)
            {
                Debug.LogWarning("HUD not found on Canvas.");
                return;
            }

            var stardust = canvas.transform.Find("StardustText")?.GetComponent<Text>();
            var dna = canvas.transform.Find("DnaText")?.GetComponent<Text>();
            BuildStep4Ui(canvas.gameObject, controller, hud, stardust, dna);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("DNA display, particle collectors, and black hole status added.");
        }

        [MenuItem("Universes/Add Single Star Step UI To Open Scene")]
        public static void AddSingleStarStepUiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need Game and Canvas in the open scene.");
                return;
            }

            var hud = canvas.GetComponent<HUD>();
            if (hud == null)
            {
                Debug.LogWarning("HUD not found on Canvas.");
                return;
            }

            CreateSingleStarUpgradeDefinitions();
            var planetPrefab = LoadOrCreatePlanetPrefab();
            var planetCatalog = LoadOrCreatePlanetTypeCatalog();
            var singleStarBalance = LoadOrCreateSingleStarBalance();
            RebuildSingleStarUpgradePanelPrefab();
            RebuildSpeciesEntryPrefab();

            var planetManager = controller.GetComponent<PlanetManager>();
            if (planetManager == null)
                planetManager = controller.gameObject.AddComponent<PlanetManager>();

            var sfxManager = SfxEditor.EnsureSfxManager(controller);

            SetRef(planetManager, "planetPrefab", planetPrefab);
            SetRef(planetManager, "planetTypeCatalog", planetCatalog);
            SetRef(controller, "planetManager", planetManager);
            SetRef(controller, "planetTypeCatalog", planetCatalog);
            SetRef(controller, "singleStarBalance", singleStarBalance);
            SetRef(controller, "sfxManager", sfxManager);
            SetEnum(controller, "gameplayMode", (int)GameplayMode.SingleStarSystemAge);

            BuildSingleStarStepUi(canvas.gameObject, controller, hud);
            BuildSpeciesUi(canvas.gameObject, controller);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Single Star System Age UI: planet button, upgrades, species panel, and star system end panel.");
        }

        [MenuItem("Universes/Add Species UI To Open Scene")]
        public static void AddSpeciesUiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need Game and Canvas in the open scene.");
                return;
            }

            RebuildSpeciesEntryPrefab();
            BuildSpeciesUi(canvas.gameObject, controller);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Species button and species panel added without changing other scene UI.");
        }

        [MenuItem("Universes/Create Planet Prefab")]
        public static void CreatePlanetPrefabMenu()
        {
            EnsureFolder("Assets/Prefabs");
            LoadOrCreatePlanetPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log($"Planet prefab ready at {PlanetPrefabPath}");
        }

        [MenuItem("Universes/Create Species UI Prefabs")]
        public static void CreateSpeciesUiPrefabsMenu()
        {
            EnsureFolder("Assets/Prefabs");
            RebuildSpeciesEntryPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log($"Species entry prefab ready at {SpeciesEntryPrefabPath}");
        }

        [MenuItem("Universes/Create Single Star Balance Asset")]
        public static void CreateSingleStarBalanceMenu()
        {
            EnsureFolder(UpgradeFolder);
            var balance = LoadOrCreateSingleStarBalance();
            AssetDatabase.SaveAssets();
            Debug.Log($"Single Star balance ready at {SingleStarBalancePath}. Tune star age, click power, orbits, and planet rules in the Inspector.");
            Selection.activeObject = balance;
        }

        [MenuItem("Universes/Create Planet Type Assets")]
        public static void CreatePlanetTypeAssetsMenu()
        {
            EnsureFolder(PlanetTypesFolder);
            var catalog = LoadOrCreatePlanetTypeCatalog();
            AssetDatabase.SaveAssets();
            Debug.Log($"Planet type assets ready under {PlanetTypesFolder}. Edit sprites, colors, and effect prefabs per type in the Inspector.");
            Selection.activeObject = catalog;
        }

        [MenuItem("Universes/Create Prestige Assets")]
        public static void CreatePrestigeAssetsMenu()
        {
            EnsureFolder(PrestigeFolder);
            EnsureFolder("Assets/Prefabs");
            CreateAllPrestigeUpgradeDefinitions();
            RebuildPrestigePanelPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log($"Prestige assets ready under {PrestigeFolder} and {PrestigePanelPrefabPath}.");
        }

        [MenuItem("Universes/Create Prestige Scene")]
        public static void CreatePrestigeSceneMenu()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(PrestigeFolder);
            EnsureFolder("Assets/Prefabs");
            CreateAllPrestigeUpgradeDefinitions();
            RebuildPrestigePanelPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildPrestigeScene();
            EditorSceneManager.SaveScene(scene, PrestigeScenePath);
            EnsureSceneInBuildSettings(ScenePath);
            EnsureSceneInBuildSettings(PrestigeScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"prestige scene saved to {PrestigeScenePath}.");
        }

        [MenuItem("Universes/Add Prestige UI To Open Scene")]
        public static void AddPrestigeUiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need Game and Canvas in the open scene.");
                return;
            }

            var hud = canvas.GetComponent<HUD>();
            if (hud == null)
            {
                Debug.LogWarning("HUD not found on Canvas.");
                return;
            }

            CreateAllPrestigeUpgradeDefinitions();
            BuildStep5Ui(canvas.gameObject, controller, hud);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Universe DNA display, prestige panel, and collapse DNA breakdown added.");
        }

        [MenuItem("Universes/Add Step 3 UI To Open Scene")]
        public static void AddStep3UiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need Game and Canvas in the open scene.");
                return;
            }

            var hud = canvas.GetComponent<HUD>();
            if (hud == null)
            {
                Debug.LogWarning("HUD not found on Canvas.");
                return;
            }

            BuildStep3Ui(canvas.gameObject, controller, hud);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Entropy bar, collapse controls, and run summary panel added.");
        }

        [MenuItem("Universes/Add Step 2 UI To Open Scene")]
        public static void AddStep2UiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need Game and Canvas in the open scene.");
                return;
            }

            var hud = canvas.GetComponent<HUD>();
            if (hud == null)
            {
                Debug.LogWarning("HUD not found on Canvas.");
                return;
            }

            BuildUpgradePanel(canvas.gameObject, controller);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Upgrade panel added. Stats line shows click/passive rates.");
        }

        [MenuItem("Universes/Add VFX To Open Scene")]
        public static void AddVfxToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();

            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need Game and Canvas in the open scene.");
                return;
            }

            var floater = canvas.GetComponent<FloatingTextSpawner>();
            if (floater == null)
                floater = canvas.gameObject.AddComponent<FloatingTextSpawner>();

            SetRef(floater, "canvas", canvas);
            SetRef(controller, "floatingTextSpawner", floater);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Floating stardust text spawner added.");
        }

        [MenuItem("Universes/Create Particle Prefabs")]
        public static void CreateParticlePrefabsMenu()
        {
            EnsureFolder("Assets/Prefabs");
            LoadOrCreateStardustParticlePrefab();
            LoadOrCreateDnaParticlePrefab();
            LoadOrCreateClickEffectPrefab();
            LoadOrCreateStardustEmitEffectPrefab();
            LoadOrCreateSupernovaEffectPrefab();
            LoadOrCreateClickCollectUpgrade();
            AssetDatabase.SaveAssets();
            Debug.Log("Particle prefabs and VFX ready under Assets/Prefabs/. Edit collectible sprites and ParticleSystem bursts in the prefabs.");
        }

        [MenuItem("Universes/Create Upgrade Prefabs")]
        public static void CreateUpgradePrefabsMenu()
        {
            EnsureFolder("Assets/Prefabs");
            LoadOrCreateClickCollectUpgrade();
            RebuildUpgradeRowPrefab();
            RebuildUpgradePanelPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log($"Upgrade prefabs ready at {UpgradeRowPrefabPath} and {UpgradePanelPrefabPath}. Re-run Step 2 UI menu on your scene if the panel was already placed.");
        }

        [MenuItem("Universes/Create Star Prefab")]
        public static void CreateStarPrefabMenu()
        {
            EnsureFolder("Assets/Prefabs");

            if (AssetDatabase.LoadAssetAtPath<StarView>(PrefabPath) != null)
            {
                Debug.Log($"Star prefab already exists at {PrefabPath}. Edit it in the Inspector — setup will not overwrite it.");
                return;
            }

            var sprite = LoadSunSprite() ?? CreateCircleSprite();
            BuildStarPrefab(sprite);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created star prefab at {PrefabPath}. Tune glow scale, sprites, and collider in the prefab.");
        }

        [MenuItem("Universes/Setup Scene")]
        public static void CreateGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildGameScene();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"scene saved to {ScenePath}. Edit objects in the Hierarchy, then press Play.");
        }

        [MenuItem("Universes/Setup In Open Scene")]
        public static void SetupInOpenScene()
        {
            BuildGameScene();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("objects added to the open scene. Edit in the Hierarchy, then press Play.");
        }

        private static void BuildGameScene()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ArtPath);

            var starPrefab = LoadOrCreateStarPrefab();

            SetupCamera();
            var worldRoot = new GameObject("WorldRoot").transform;

            var gameGo = new GameObject("Game");
            var controller = gameGo.AddComponent<GameController>();
            var particles = gameGo.AddComponent<CosmicParticleManager>();
            var effects = gameGo.AddComponent<ParticleEffectManager>();
            SetRef(controller, "starViewPrefab", starPrefab);
            SetRef(controller, "worldRoot", worldRoot);
            SetRef(controller, "particleManager", particles);
            SetRef(controller, "effectManager", effects);
            SetRef(particles, "stardustParticlePrefab", LoadOrCreateStardustParticlePrefab());
            SetRef(particles, "dnaParticlePrefab", LoadOrCreateDnaParticlePrefab());
            SetRef(effects, "clickEffectPrefab", LoadOrCreateClickEffectPrefab());
            SetRef(effects, "stardustEmitEffectPrefab", LoadOrCreateStardustEmitEffectPrefab());
            SetRef(effects, "supernovaEffectPrefab", LoadOrCreateSupernovaEffectPrefab());

            SetupEventSystem();
            var floater = BuildHud(controller);
            SetRef(controller, "floatingTextSpawner", floater);

            Selection.activeGameObject = gameGo;
            EditorGUIUtility.PingObject(gameGo);
        }

        private static void SetupCamera()
        {
            var existing = Camera.main;
            if (existing != null)
            {
                existing.orthographic = true;
                existing.orthographicSize = 5f;
                existing.backgroundColor = new Color(0.04f, 0.04f, 0.1f);
                existing.transform.position = new Vector3(0, 0, -10);
                return;
            }

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.1f);
            cam.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();
        }

        private static void SetupEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private static FloatingTextSpawner BuildHud(GameController controller)
        {
            var existingCanvas = Object.FindAnyObjectByType<Canvas>();
            if (existingCanvas != null && existingCanvas.GetComponent<HUD>() != null)
                Object.DestroyImmediate(existingCanvas.gameObject);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<HUD>();
            var floater = canvasGo.AddComponent<FloatingTextSpawner>();
            SetRef(floater, "canvas", canvas);

            var stardust = CreateText(canvasGo.transform, "Stardust: 0", font, 28);
            stardust.gameObject.name = "StardustText";
            AnchorTop(stardust.rectTransform, 0, -40, 500, 40);

            var dna = CreateText(canvasGo.transform, "DNA Fragments: 0", font, 20);
            dna.gameObject.name = "DnaText";
            dna.color = new Color(0.55f, 1f, 0.75f);
            AnchorTop(dna.rectTransform, 0, -130, 500, 28);

            var stats = CreateText(canvasGo.transform, "", font, 16);
            stats.alignment = TextAnchor.UpperCenter;
            AnchorTop(stats.rectTransform, 0, -165, 520, 55);

            var feedback = CreateText(canvasGo.transform, "", font, 22);
            feedback.color = new Color(1f, 0.9f, 0.4f);
            AnchorCenter(feedback.rectTransform, 0, 120, 400, 40);

            BuildUpgradePanel(canvasGo, controller);

            var btnGo = new GameObject("CreateStarButton");
            btnGo.transform.SetParent(canvasGo.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0, 60);
            btnRect.sizeDelta = new Vector2(240, 44);
            btnGo.AddComponent<Image>().color = new Color(0.2f, 0.35f, 0.55f);
            var btn = btnGo.AddComponent<Button>();

            var btnLabel = CreateText(btnGo.transform, "Create New Star (20)", font, 16);
            Stretch(btnLabel.rectTransform);
            btnLabel.alignment = TextAnchor.MiddleCenter;

            var planetBtnGo = new GameObject("CreatePlanetButton");
            planetBtnGo.transform.SetParent(canvasGo.transform, false);
            var planetBtnRect = planetBtnGo.AddComponent<RectTransform>();
            planetBtnRect.anchorMin = new Vector2(0.5f, 0f);
            planetBtnRect.anchorMax = new Vector2(0.5f, 0f);
            planetBtnRect.anchoredPosition = new Vector2(0, 110);
            planetBtnRect.sizeDelta = new Vector2(260, 40);
            planetBtnGo.AddComponent<Image>().color = new Color(0.18f, 0.38f, 0.28f);
            var planetBtn = planetBtnGo.AddComponent<Button>();
            var planetBtnLabel = CreateText(planetBtnGo.transform, "Create Planet", font, 15);
            Stretch(planetBtnLabel.rectTransform);
            planetBtnLabel.alignment = TextAnchor.MiddleCenter;

            var collapsePanel = BuildStep3Ui(canvasGo, controller, hud);
            BuildStep4Ui(canvasGo, controller, hud, stardust, dna);

            SetRef(hud, "controller", controller);
            SetRef(hud, "stardustText", stardust);
            SetRef(hud, "dnaText", dna);
            SetRef(hud, "statsText", stats);
            SetRef(hud, "feedbackText", feedback);
            SetRef(hud, "stardustCollector", stardust.rectTransform);
            SetRef(hud, "dnaCollector", dna.rectTransform);
            SetRef(hud, "createStarButton", btn);
            SetRef(hud, "createStarButtonText", btnLabel);
            SetRef(hud, "createPlanetButton", planetBtn);
            SetRef(hud, "createPlanetButtonText", planetBtnLabel);
            SetRef(hud, "collapsePanel", collapsePanel);

            return floater;
        }

        private static CollapsePanel BuildStep3Ui(GameObject canvasGo, GameController controller,
            HUD hud)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var existingEntropy = canvasGo.transform.Find("EntropyPanel");
            if (existingEntropy != null)
                Object.DestroyImmediate(existingEntropy.gameObject);

            var existingCollapse = canvasGo.transform.Find("CollapsePanel");
            if (existingCollapse != null)
                Object.DestroyImmediate(existingCollapse.gameObject);

            var existingCollapseBtn = canvasGo.transform.Find("CollapseUniverseButton");
            if (existingCollapseBtn != null)
                Object.DestroyImmediate(existingCollapseBtn.gameObject);

            var entropyPanel = new GameObject("EntropyPanel");
            entropyPanel.transform.SetParent(canvasGo.transform, false);
            var entropyPanelRect = entropyPanel.AddComponent<RectTransform>();
            entropyPanelRect.anchorMin = new Vector2(0f, 1f);
            entropyPanelRect.anchorMax = new Vector2(0f, 1f);
            entropyPanelRect.pivot = new Vector2(0f, 1f);
            entropyPanelRect.anchoredPosition = new Vector2(16, -16);
            entropyPanelRect.sizeDelta = new Vector2(360, 72);

            var entropyText = CreateText(entropyPanel.transform, "Entropy: 0%", font, 18);
            var entropyTextRect = entropyText.rectTransform;
            entropyTextRect.anchorMin = new Vector2(0, 1);
            entropyTextRect.anchorMax = new Vector2(1, 1);
            entropyTextRect.pivot = new Vector2(0.5f, 1);
            entropyTextRect.anchoredPosition = new Vector2(0, 0);
            entropyTextRect.sizeDelta = new Vector2(0, 24);
            entropyText.alignment = TextAnchor.MiddleLeft;

            var statusText = CreateText(entropyPanel.transform, "Stable Universe", font, 14);
            var statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0, 1);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.pivot = new Vector2(0.5f, 1);
            statusRect.anchoredPosition = new Vector2(0, -24);
            statusRect.sizeDelta = new Vector2(0, 20);
            statusText.alignment = TextAnchor.MiddleLeft;
            statusText.color = new Color(0.75f, 0.85f, 1f);

            var sliderGo = new GameObject("EntropySlider");
            sliderGo.transform.SetParent(entropyPanel.transform, false);
            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0);
            sliderRect.anchorMax = new Vector2(1, 0);
            sliderRect.pivot = new Vector2(0.5f, 0);
            sliderRect.anchoredPosition = new Vector2(0, 8);
            sliderRect.sizeDelta = new Vector2(0, 18);

            var sliderBg = new GameObject("Background");
            sliderBg.transform.SetParent(sliderGo.transform, false);
            Stretch(sliderBg.AddComponent<RectTransform>());
            var bgImage = sliderBg.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            Stretch(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(4, 4);
            fillAreaRect.offsetMax = new Vector2(-4, -4);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.25f, 0.75f, 0.95f);

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            var collapseBtnGo = new GameObject("CollapseUniverseButton");
            collapseBtnGo.transform.SetParent(canvasGo.transform, false);
            var collapseBtnRect = collapseBtnGo.AddComponent<RectTransform>();
            collapseBtnRect.anchorMin = new Vector2(0f, 0f);
            collapseBtnRect.anchorMax = new Vector2(0f, 0f);
            collapseBtnRect.anchoredPosition = new Vector2(140, 60);
            collapseBtnRect.sizeDelta = new Vector2(220, 44);
            collapseBtnGo.AddComponent<Image>().color = new Color(0.45f, 0.15f, 0.18f);
            var collapseBtn = collapseBtnGo.AddComponent<Button>();
            var collapseLabel = CreateText(collapseBtnGo.transform, "Collapse Universe", font, 16);
            Stretch(collapseLabel.rectTransform);
            collapseLabel.alignment = TextAnchor.MiddleCenter;

            var collapsePanelGo = new GameObject("CollapsePanel");
            collapsePanelGo.transform.SetParent(canvasGo.transform, false);
            Stretch(collapsePanelGo.AddComponent<RectTransform>());
            var overlay = collapsePanelGo.AddComponent<Image>();
            overlay.color = new Color(0.02f, 0.03f, 0.08f, 0.88f);

            var card = new GameObject("Card");
            card.transform.SetParent(collapsePanelGo.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(180f, 0f);
            cardRect.sizeDelta = new Vector2(520, 520);
            card.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.16f, 0.98f);

            var title = CreateText(card.transform, "Universe Collapsed", font, 28);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(-32, 40);
            title.fontStyle = FontStyle.Bold;

            var summary = CreateText(card.transform, "", font, 18);
            var summaryRect = summary.rectTransform;
            summaryRect.anchorMin = new Vector2(0, 0.25f);
            summaryRect.anchorMax = new Vector2(1, 0.85f);
            summaryRect.offsetMin = new Vector2(24, 0);
            summaryRect.offsetMax = new Vector2(-24, 0);
            summary.alignment = TextAnchor.UpperLeft;

            var newUniverseBtnGo = new GameObject("StartNewUniverseButton");
            newUniverseBtnGo.transform.SetParent(card.transform, false);
            var newBtnRect = newUniverseBtnGo.AddComponent<RectTransform>();
            newBtnRect.anchorMin = new Vector2(0.5f, 0f);
            newBtnRect.anchorMax = new Vector2(0.5f, 0f);
            newBtnRect.anchoredPosition = new Vector2(0, 24);
            newBtnRect.sizeDelta = new Vector2(260, 44);
            newUniverseBtnGo.AddComponent<Image>().color = new Color(0.2f, 0.45f, 0.65f);
            var newUniverseBtn = newUniverseBtnGo.AddComponent<Button>();
            var newUniverseLabel = CreateText(newUniverseBtnGo.transform, "Start New Universe", font, 18);
            Stretch(newUniverseLabel.rectTransform);
            newUniverseLabel.alignment = TextAnchor.MiddleCenter;

            var prestigeBtnGo = new GameObject("OpenPrestigeButton");
            prestigeBtnGo.transform.SetParent(card.transform, false);
            var prestigeBtnRect = prestigeBtnGo.AddComponent<RectTransform>();
            prestigeBtnRect.anchorMin = new Vector2(0.5f, 0f);
            prestigeBtnRect.anchorMax = new Vector2(0.5f, 0f);
            prestigeBtnRect.anchoredPosition = new Vector2(0, 78);
            prestigeBtnRect.sizeDelta = new Vector2(260, 36);
            prestigeBtnGo.AddComponent<Image>().color = new Color(0.16f, 0.28f, 0.22f);
            var prestigeBtn = prestigeBtnGo.AddComponent<Button>();
            var prestigeBtnLabel = CreateText(prestigeBtnGo.transform, "Prestige Upgrades", font, 16);
            Stretch(prestigeBtnLabel.rectTransform);
            prestigeBtnLabel.alignment = TextAnchor.MiddleCenter;

            var collapsePanel = collapsePanelGo.AddComponent<CollapsePanel>();
            SetRef(collapsePanel, "controller", controller);
            SetRef(collapsePanel, "panelRoot", collapsePanelGo);
            SetRef(collapsePanel, "titleText", title);
            SetRef(collapsePanel, "summaryText", summary);
            SetRef(collapsePanel, "prestigeButton", prestigeBtn);
            SetRef(collapsePanel, "startNewUniverseButton", newUniverseBtn);
            collapsePanelGo.SetActive(false);

            if (hud != null)
            {
                SetRef(hud, "entropyText", entropyText);
                SetRef(hud, "universeStatusText", statusText);
                SetRef(hud, "entropySlider", slider);
                SetRef(hud, "entropyFillImage", fillImage);
                SetRef(hud, "collapseUniverseButton", collapseBtn);
                SetRef(hud, "collapsePanel", collapsePanel);
            }

            return collapsePanel;
        }

        private static void BuildStep4Ui(GameObject canvasGo, GameController controller,
            HUD hud, Text stardustText, Text dnaText)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (stardustText == null)
            {
                stardustText = CreateText(canvasGo.transform, "Stardust: 0", font, 28);
                stardustText.gameObject.name = "StardustText";
                AnchorTop(stardustText.rectTransform, 0, -40, 500, 40);
            }

            if (dnaText == null)
            {
                dnaText = CreateText(canvasGo.transform, "DNA Fragments: 0", font, 20);
                dnaText.gameObject.name = "DnaText";
                dnaText.color = new Color(0.55f, 1f, 0.75f);
                AnchorTop(dnaText.rectTransform, 0, -130, 500, 28);
            }

            var existing = canvasGo.transform.Find("BlackHoleStatusText");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var blackHoleStatus = CreateText(canvasGo.transform, "", font, 14);
            blackHoleStatus.gameObject.name = "BlackHoleStatusText";
            blackHoleStatus.alignment = TextAnchor.UpperCenter;
            AnchorTop(blackHoleStatus.rectTransform, 0, -225, 560, 36);

            var particles = controller.GetComponent<CosmicParticleManager>();
            if (particles == null)
                particles = controller.gameObject.AddComponent<CosmicParticleManager>();

            var effects = controller.GetComponent<ParticleEffectManager>();
            if (effects == null)
                effects = controller.gameObject.AddComponent<ParticleEffectManager>();

            SetRef(controller, "particleManager", particles);
            SetRef(controller, "effectManager", effects);
            SetRef(particles, "stardustParticlePrefab", LoadOrCreateStardustParticlePrefab());
            SetRef(particles, "dnaParticlePrefab", LoadOrCreateDnaParticlePrefab());
            SetRef(effects, "clickEffectPrefab", LoadOrCreateClickEffectPrefab());
            SetRef(effects, "stardustEmitEffectPrefab", LoadOrCreateStardustEmitEffectPrefab());
            SetRef(effects, "supernovaEffectPrefab", LoadOrCreateSupernovaEffectPrefab());

            if (hud != null)
            {
                SetRef(hud, "dnaText", dnaText);
                SetRef(hud, "blackHoleStatusText", blackHoleStatus);
                SetRef(hud, "stardustCollector", stardustText.rectTransform);
                SetRef(hud, "dnaCollector", dnaText.rectTransform);
            }
        }

        private static void BuildStep5Ui(GameObject canvasGo, GameController controller, HUD hud)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var universeDna = canvasGo.transform.Find("UniverseDnaText")?.GetComponent<Text>();
            if (universeDna == null)
            {
                universeDna = CreateText(canvasGo.transform, "Universe DNA: 0", font, 20);
                universeDna.gameObject.name = "UniverseDnaText";
                universeDna.color = new Color(0.75f, 0.85f, 1f);
                AnchorTop(universeDna.rectTransform, 0, -100, 500, 28);
            }

            var existingPrestige = canvasGo.transform.Find("PrestigePanel");
            if (existingPrestige != null)
                Object.DestroyImmediate(existingPrestige.gameObject);

            var prestigePrefab = LoadOrCreatePrestigePanelPrefab();
            var prestigeParent = canvasGo.transform.Find("CollapsePanel") ?? canvasGo.transform;
            var prestigeGo = (GameObject)PrefabUtility.InstantiatePrefab(prestigePrefab, prestigeParent);
            prestigeGo.name = "PrestigePanel";
            var prestigeRect = prestigeGo.GetComponent<RectTransform>();
            prestigeRect.anchorMin = new Vector2(0f, 0f);
            prestigeRect.anchorMax = new Vector2(0f, 1f);
            prestigeRect.pivot = new Vector2(0f, 0.5f);
            prestigeRect.anchoredPosition = new Vector2(24, 0);
            prestigeRect.sizeDelta = new Vector2(300, -80);

            var collapsePanel = Object.FindAnyObjectByType<CollapsePanel>(FindObjectsInactive.Include);
            var prestigePanel = prestigeGo.GetComponent<PrestigePanel>();
            if (prestigePanel != null)
                SetRef(prestigePanel, "controller", controller);
            if (collapsePanel != null)
                SetRef(collapsePanel, "prestigePanel", prestigePanel);

            if (hud != null)
                SetRef(hud, "universeDnaText", universeDna);
        }

        private static void BuildPrestigeScene()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.04f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var title = CreateText(canvasGo.transform, "Permanent DNA Upgrades", font, 34);
            title.gameObject.name = "Title";
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.8f, 0.9f, 1f);
            AnchorTop(title.rectTransform, 0, -32, 700, 50);

            var universeDna = CreateText(canvasGo.transform, "Universe DNA: 0", font, 24);
            universeDna.gameObject.name = "UniverseDnaText";
            universeDna.color = new Color(0.55f, 1f, 0.75f);
            AnchorTop(universeDna.rectTransform, 0, -86, 500, 36);

            var backGo = new GameObject("BackToGameButton", typeof(RectTransform));
            backGo.transform.SetParent(canvasGo.transform, false);
            var backRect = backGo.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(24f, -24f);
            backRect.sizeDelta = new Vector2(220f, 44f);
            backGo.AddComponent<Image>().color = new Color(0.16f, 0.22f, 0.34f);
            var backButton = backGo.AddComponent<Button>();
            var backLabel = CreateText(backGo.transform, "Back To Game", font, 16);
            Stretch(backLabel.rectTransform);
            backLabel.alignment = TextAnchor.MiddleCenter;

            var prestigePrefab = LoadOrCreatePrestigePanelPrefab();
            var prestigeGo = (GameObject)PrefabUtility.InstantiatePrefab(prestigePrefab, canvasGo.transform);
            prestigeGo.name = "PrestigePanel";
            var prestigeRect = prestigeGo.GetComponent<RectTransform>();
            prestigeRect.anchorMin = new Vector2(0.5f, 0f);
            prestigeRect.anchorMax = new Vector2(0.5f, 1f);
            prestigeRect.pivot = new Vector2(0.5f, 0.5f);
            prestigeRect.anchoredPosition = new Vector2(0f, -24f);
            prestigeRect.sizeDelta = new Vector2(900f, -170f);

            var prestigePanel = prestigeGo.GetComponent<PrestigePanel>();
            if (prestigePanel != null)
            {
                SetBool(prestigePanel, "standaloneSceneMode", true);
                SetRef(prestigePanel, "universeDnaText", universeDna);
                SetRef(prestigePanel, "backToGameButton", backButton);
                SetString(prestigePanel, "gameSceneName", GameSceneName);
            }
        }

        private static void CreateAllPrestigeUpgradeDefinitions()
        {
            CreatePrestigeDefinition(PrestigeUpgradeType.StrongerBigBang, "stronger_big_bang",
                "Stronger Big Bang", "New universes start with bonus Stardust.");
            CreatePrestigeDefinition(PrestigeUpgradeType.CosmicEfficiency, "cosmic_efficiency",
                "Cosmic Efficiency", "Increases all Stardust production.",
                Req(PrestigeUpgradeType.StrongerBigBang, 1));
            CreatePrestigeDefinition(PrestigeUpgradeType.StablePhysics, "stable_physics",
                "Stable Physics", "Reduces Entropy gain from all sources.",
                Req(PrestigeUpgradeType.StrongerBigBang, 1));
            CreatePrestigeDefinition(PrestigeUpgradeType.LongerStarLifespan, "longer_star_lifespan",
                "Longer Star Lifespan", "Stars age more slowly from clicks and passive production.",
                Req(PrestigeUpgradeType.StablePhysics, 1));
            CreatePrestigeDefinition(PrestigeUpgradeType.SupernovaMemory, "supernova_memory",
                "Supernova Memory", "Supernovas are more likely to create DNA Fragments.",
                Req(PrestigeUpgradeType.StrongerBigBang, 1));
            CreatePrestigeDefinition(PrestigeUpgradeType.BlackHoleMemory, "black_hole_memory",
                "Black Hole Memory", "Black Holes generate more DNA potential while alive.",
                Req(PrestigeUpgradeType.SupernovaMemory, 1));
            CreatePrestigeDefinition(PrestigeUpgradeType.ParticleEvolution, "particle_evolution",
                "Particle Evolution", "Higher chance for Stardust gains to be doubled.",
                Req(PrestigeUpgradeType.CosmicEfficiency, 2));
            CreatePrestigeDefinition(PrestigeUpgradeType.ParallelEcho, "parallel_echo",
                "Parallel Echo", "Past collapsed universes echo passive Stardust into the new universe.",
                Req(PrestigeUpgradeType.BlackHoleMemory, 1),
                Req(PrestigeUpgradeType.LongerStarLifespan, 1));

            CreatePrestigeDefinition(PrestigeUpgradeType.PlanetLongevity, "planet_longevity",
                "Planet Longevity", "Increases the durability and lifetime of newly formed planets.",
                Req(PrestigeUpgradeType.StrongerBigBang, 1));
            CreatePrestigeDefinition(PrestigeUpgradeType.LifeGenesis, "life_genesis",
                "Life Genesis", "Increases the chance for life to emerge on habitable planets.",
                Req(PrestigeUpgradeType.PlanetLongevity, 1));
            CreatePrestigeDefinition(PrestigeUpgradeType.PlanetaryDetonation, "planetary_detonation",
                "Planetary Detonation", "Destroyed planets release additional Stardust.",
                Req(PrestigeUpgradeType.PlanetLongevity, 1));
            CreatePrestigeDefinition(PrestigeUpgradeType.PlanetaryLegacy, "planetary_legacy",
                "Planetary Legacy", "Destroyed planets release additional DNA potential.",
                Req(PrestigeUpgradeType.LifeGenesis, 1));
            CreatePrestigeDefinition(PrestigeUpgradeType.CivilizationAcceleration, "civilization_acceleration",
                "Civilization Acceleration", "Civilizations progress through their stages faster.",
                Req(PrestigeUpgradeType.PlanetaryDetonation, 1));
        }

        private static PrestigeUpgradeDefinition CreatePrestigeDefinition(
            PrestigeUpgradeType type, string assetName, string displayName, string description,
            params PrestigeUpgradeRequirement[] prerequisites)
        {
            var path = $"{PrestigeFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PrestigeUpgradeDefinition>(path);
            if (existing != null)
            {
                ApplyPrestigeDefinitionDefaults(existing, type, displayName, description, prerequisites);
                return existing;
            }

            var def = ScriptableObject.CreateInstance<PrestigeUpgradeDefinition>();
            ApplyPrestigeDefinitionDefaults(def, type, displayName, description, prerequisites);
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static PrestigeUpgradeRequirement Req(PrestigeUpgradeType type, int level) =>
            new()
            {
                upgradeType = type,
                requiredLevel = level
            };

        private static void ApplyPrestigeDefinitionDefaults(PrestigeUpgradeDefinition def,
            PrestigeUpgradeType type, string displayName, string description,
            PrestigeUpgradeRequirement[] prerequisites)
        {
            def.upgradeType = type;
            def.displayName = displayName;
            def.description = description;
            def.maxLevel = 0;
            def.prerequisites = prerequisites ?? System.Array.Empty<PrestigeUpgradeRequirement>();
            EditorUtility.SetDirty(def);
        }

        private static PrestigeUpgradeDefinition LoadPrestigeDefinition(string assetName) =>
            AssetDatabase.LoadAssetAtPath<PrestigeUpgradeDefinition>(
                $"{PrestigeFolder}/{assetName}.asset");

        private static GameObject LoadPrestigeRowPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrestigeRowPrefabPath);
            if (existing != null)
                return existing;

            Debug.LogError(
                $"Prestige row prefab not found at {PrestigeRowPrefabPath}. " +
                "Create or place the icon-only PrestigeRow prefab there before rebuilding prestige UI.");
            return null;
        }

        private static Vector2 GetPrestigeTreeNodeSize(GameObject rowPrefab)
        {
            if (rowPrefab == null)
                return new Vector2(75f, 75f);

            var rect = rowPrefab.GetComponent<RectTransform>();
            return rect != null ? rect.sizeDelta : new Vector2(75f, 75f);
        }

        private static GameObject LoadOrCreatePrestigePanelPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrestigePanelPrefabPath);
            if (existing != null)
                return existing;

            return BuildPrestigePanelPrefab();
        }

        private static GameObject RebuildPrestigePanelPrefab()
        {
            return BuildPrestigePanelPrefab();
        }

        private static GameObject BuildPrestigePanelPrefab()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rowPrefab = LoadPrestigeRowPrefab();
            if (rowPrefab == null)
                return null;

            var treeNodeSize = GetPrestigeTreeNodeSize(rowPrefab);
            CreateAllPrestigeUpgradeDefinitions();

            var panelGo = new GameObject("PrestigePanel");
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(8, 0);
            panelRect.sizeDelta = new Vector2(300, -100);
            panelGo.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.1f, 0.94f);

            var header = CreateText(panelGo.transform, "Prestige Upgrades", font, 18);
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -8);
            headerRect.sizeDelta = new Vector2(-16, 28);
            header.fontStyle = FontStyle.Bold;
            header.color = new Color(0.8f, 0.9f, 1f);

            var scrollGo = new GameObject("PrestigeScroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRectTransform = scrollGo.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(8, 8);
            scrollRectTransform.offsetMax = new Vector2(-8, -40);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            Stretch(viewport.AddComponent<RectTransform>());
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = PrestigeTreeContentSize;

            scroll.content = contentRect;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var tooltipRoot = BuildPrestigeTooltip(panelGo.transform, font, out var tooltipText);

            var defs = new[]
            {
                LoadPrestigeDefinition("stronger_big_bang"),
                LoadPrestigeDefinition("cosmic_efficiency"),
                LoadPrestigeDefinition("stable_physics"),
                LoadPrestigeDefinition("supernova_memory"),
                LoadPrestigeDefinition("longer_star_lifespan"),
                LoadPrestigeDefinition("particle_evolution"),
                LoadPrestigeDefinition("black_hole_memory"),
                LoadPrestigeDefinition("parallel_echo"),
                LoadPrestigeDefinition("planet_longevity"),
                LoadPrestigeDefinition("life_genesis"),
                LoadPrestigeDefinition("planetary_detonation"),
                LoadPrestigeDefinition("planetary_legacy"),
                LoadPrestigeDefinition("civilization_acceleration")
            };

            var rowInstances = new System.Collections.Generic.List<PrestigeUpgradeRow>();
            foreach (var def in defs)
            {
                if (def == null)
                    continue;

                var rowInstance = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, content.transform);
                rowInstance.name = def.upgradeType.ToString();
                var row = rowInstance.GetComponent<PrestigeUpgradeRow>();
                var so = new SerializedObject(row);
                so.FindProperty("definition").objectReferenceValue = def;
                so.FindProperty("tooltipRoot").objectReferenceValue = tooltipRoot;
                so.FindProperty("tooltipText").objectReferenceValue = tooltipText;
                so.ApplyModifiedPropertiesWithoutUndo();
                PositionPrestigeTreeRow(rowInstance.GetComponent<RectTransform>(), def.upgradeType);
                rowInstances.Add(row);
            }

            AddPrestigeTreeLines(content.transform, treeNodeSize);

            var panelComp = panelGo.AddComponent<PrestigePanel>();
            var panelSo = new SerializedObject(panelComp);
            var rowsProp = panelSo.FindProperty("upgradeRows");
            rowsProp.arraySize = rowInstances.Count;
            for (var i = 0; i < rowInstances.Count; i++)
                rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rowInstances[i];
            panelSo.FindProperty("panelRoot").objectReferenceValue = panelGo;
            panelSo.FindProperty("tooltipRoot").objectReferenceValue = tooltipRoot;
            panelSo.FindProperty("tooltipText").objectReferenceValue = tooltipText;
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(panelGo, PrestigePanelPrefabPath);
            Object.DestroyImmediate(panelGo);
            return prefab;
        }

        private static void PositionPrestigeTreeRow(RectTransform rect, PrestigeUpgradeType type)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = GetPrestigeTreePosition(type);
        }

        private static void AddPrestigeTreeLines(Transform content, Vector2 treeNodeSize)
        {
            var linesRoot = new GameObject("TreeConnections", typeof(RectTransform));
            linesRoot.transform.SetParent(content, false);
            var rootRect = linesRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.SetAsFirstSibling();

            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.StrongerBigBang,
                PrestigeUpgradeType.CosmicEfficiency, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.StrongerBigBang,
                PrestigeUpgradeType.StablePhysics, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.StrongerBigBang,
                PrestigeUpgradeType.SupernovaMemory, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.StrongerBigBang,
                PrestigeUpgradeType.PlanetLongevity, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.CosmicEfficiency,
                PrestigeUpgradeType.ParticleEvolution, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.StablePhysics,
                PrestigeUpgradeType.LongerStarLifespan, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.SupernovaMemory,
                PrestigeUpgradeType.BlackHoleMemory, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.BlackHoleMemory,
                PrestigeUpgradeType.ParallelEcho, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.LongerStarLifespan,
                PrestigeUpgradeType.ParallelEcho, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.PlanetLongevity,
                PrestigeUpgradeType.LifeGenesis, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.PlanetLongevity,
                PrestigeUpgradeType.PlanetaryDetonation, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.LifeGenesis,
                PrestigeUpgradeType.PlanetaryLegacy, treeNodeSize);
            AddPrestigeTreeLine(linesRoot.transform, PrestigeUpgradeType.PlanetaryDetonation,
                PrestigeUpgradeType.CivilizationAcceleration, treeNodeSize);
        }

        private static GameObject BuildPrestigeTooltip(Transform parent, Font font, out Text tooltipText)
        {
            var tooltipGo = new GameObject("PrestigeTooltip", typeof(RectTransform));
            tooltipGo.transform.SetParent(parent, false);
            var rect = tooltipGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-12f, 0f);
            rect.sizeDelta = new Vector2(300f, 220f);
            var image = tooltipGo.AddComponent<Image>();
            image.color = new Color(0.04f, 0.055f, 0.08f, 0.96f);
            image.raycastTarget = false;

            tooltipText = CreateText(tooltipGo.transform, "", font, 14);
            tooltipText.name = "TooltipText";
            tooltipText.alignment = TextAnchor.UpperLeft;
            tooltipText.color = new Color(0.85f, 0.92f, 1f);
            tooltipText.raycastTarget = false;
            var textRect = tooltipText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 10f);
            textRect.offsetMax = new Vector2(-12f, -10f);

            tooltipGo.SetActive(false);
            return tooltipGo;
        }

        private static void AddPrestigeTreeLine(Transform parent, PrestigeUpgradeType from,
            PrestigeUpgradeType to, Vector2 treeNodeSize)
        {
            var start = GetPrestigeTreeNodeCenter(from, treeNodeSize);
            var end = GetPrestigeTreeNodeCenter(to, treeNodeSize);
            var direction = end - start;
            var length = direction.magnitude;
            if (length <= 0.01f)
                return;

            var lineGo = new GameObject($"{from}_To_{to}", typeof(RectTransform));
            lineGo.transform.SetParent(parent, false);
            var rect = lineGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (start + end) * 0.5f;
            rect.sizeDelta = new Vector2(length, 3f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            var image = lineGo.AddComponent<Image>();
            image.color = PrestigeTreeLineColor;
            image.raycastTarget = false;

            var connection = lineGo.AddComponent<PrestigeTreeConnection>();
            connection.Configure(from, to);
        }

        private static Vector2 GetPrestigeTreeNodeCenter(PrestigeUpgradeType type, Vector2 treeNodeSize)
        {
            var position = GetPrestigeTreePosition(type);
            return position + new Vector2(0f, -treeNodeSize.y * 0.5f);
        }

        private static Vector2 GetPrestigeTreePosition(PrestigeUpgradeType type) => type switch
        {
            PrestigeUpgradeType.StrongerBigBang => new Vector2(0f, -380f),

            // Up: Stardust
            PrestigeUpgradeType.CosmicEfficiency => new Vector2(0f, -280f),
            PrestigeUpgradeType.ParticleEvolution => new Vector2(0f, -180f),

            // Right: Stability
            PrestigeUpgradeType.StablePhysics => new Vector2(100f, -380f),
            PrestigeUpgradeType.LongerStarLifespan => new Vector2(200f, -380f),
            PrestigeUpgradeType.ParallelEcho => new Vector2(250f, -480f),

            // Left: DNA
            PrestigeUpgradeType.SupernovaMemory => new Vector2(-100f, -380f),
            PrestigeUpgradeType.BlackHoleMemory => new Vector2(-200f, -380f),

            // Down: Life
            PrestigeUpgradeType.PlanetLongevity => new Vector2(0f, -480f),
            PrestigeUpgradeType.LifeGenesis => new Vector2(-70f, -580f),
            PrestigeUpgradeType.PlanetaryDetonation => new Vector2(70f, -580f),
            PrestigeUpgradeType.PlanetaryLegacy => new Vector2(-70f, -680f),
            PrestigeUpgradeType.CivilizationAcceleration => new Vector2(70f, -680f),

            _ => Vector2.zero
        };

        private static Image CreateUpgradeIcon(Transform parent, string objectName, Vector2 size)
        {
            var iconGo = new GameObject(objectName, typeof(RectTransform));
            iconGo.transform.SetParent(parent, false);
            var rect = iconGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(8f, 0f);
            rect.sizeDelta = size;

            var image = iconGo.AddComponent<Image>();
            image.enabled = false;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void BuildUpgradePanel(GameObject canvasGo, GameController controller)
        {
            var old = canvasGo.transform.Find("UpgradePanel");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var panelPrefab = LoadOrCreateUpgradePanelPrefab();
            var panelGo = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvasGo.transform);
            panelGo.name = "UpgradePanel";

            var panel = panelGo.GetComponent<UpgradePanel>();
            if (panel != null)
                SetRef(panel, "controller", controller);
        }

        private static void BuildSpeciesUi(GameObject canvasGo, GameController controller)
        {
            var old = canvasGo.transform.Find("SpeciesUI");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var entryPrefab = LoadOrCreateSpeciesEntryPrefab();

            var root = new GameObject("SpeciesUI", typeof(RectTransform));
            root.transform.SetParent(canvasGo.transform, false);
            Stretch(root.GetComponent<RectTransform>());
            var portraitPool = root.AddComponent<SpeciesPortraitPool>();

            var buttonGo = new GameObject("SpeciesButton", typeof(RectTransform));
            buttonGo.transform.SetParent(root.transform, false);
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(0f, 0f);
            buttonRect.pivot = new Vector2(0f, 0f);
            buttonRect.anchoredPosition = new Vector2(16, 24);
            buttonRect.sizeDelta = new Vector2(150, 42);
            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.18f, 0.32f, 0.5f, 0.95f);
            var openButton = buttonGo.AddComponent<Button>();

            var buttonLabel = CreateText(buttonGo.transform, "Species", font, 16);
            Stretch(buttonLabel.rectTransform);
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            buttonLabel.fontStyle = FontStyle.Bold;

            var panelGo = new GameObject("SpeciesPanel", typeof(RectTransform));
            panelGo.transform.SetParent(root.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(16, 0);
            panelRect.sizeDelta = new Vector2(330, -140);
            panelGo.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f, 0.96f);

            var title = CreateText(panelGo.transform, "Species", font, 22);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -12);
            titleRect.sizeDelta = new Vector2(-64, 34);
            title.fontStyle = FontStyle.Bold;

            var closeGo = new GameObject("CloseButton", typeof(RectTransform));
            closeGo.transform.SetParent(panelGo.transform, false);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-10, -10);
            closeRect.sizeDelta = new Vector2(34, 30);
            closeGo.AddComponent<Image>().color = new Color(0.2f, 0.22f, 0.3f, 0.95f);
            var closeButton = closeGo.AddComponent<Button>();

            var closeText = CreateText(closeGo.transform, "X", font, 16);
            Stretch(closeText.rectTransform);
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.fontStyle = FontStyle.Bold;

            var scrollGo = new GameObject("SpeciesScroll", typeof(RectTransform));
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRectTransform = scrollGo.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(10, 12);
            scrollRectTransform.offsetMax = new Vector2(-10, -58);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollGo.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;

            var emptyText = CreateText(panelGo.transform, "No species discovered yet.", font, 15);
            var emptyRect = emptyText.rectTransform;
            emptyRect.anchorMin = new Vector2(0, 0.5f);
            emptyRect.anchorMax = new Vector2(1, 0.5f);
            emptyRect.anchoredPosition = new Vector2(0, 0);
            emptyRect.sizeDelta = new Vector2(-32, 44);
            emptyText.color = new Color(0.72f, 0.78f, 0.88f);

            var detailGo = new GameObject("SpeciesDetailPanel", typeof(RectTransform));
            detailGo.transform.SetParent(root.transform, false);
            var detailRect = detailGo.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0f, 0.5f);
            detailRect.anchorMax = new Vector2(0f, 0.5f);
            detailRect.pivot = new Vector2(0f, 0.5f);
            detailRect.anchoredPosition = new Vector2(362, 0);
            detailRect.sizeDelta = new Vector2(380, 470);
            detailGo.AddComponent<Image>().color = new Color(0.07f, 0.08f, 0.13f, 0.98f);

            var detailTitle = CreateText(detailGo.transform, "Species", font, 22);
            var detailTitleRect = detailTitle.rectTransform;
            detailTitleRect.anchorMin = new Vector2(0, 1);
            detailTitleRect.anchorMax = new Vector2(1, 1);
            detailTitleRect.pivot = new Vector2(0.5f, 1f);
            detailTitleRect.anchoredPosition = new Vector2(0, -16);
            detailTitleRect.sizeDelta = new Vector2(-64, 34);
            detailTitle.fontStyle = FontStyle.Bold;

            var detailCloseGo = new GameObject("CloseButton", typeof(RectTransform));
            detailCloseGo.transform.SetParent(detailGo.transform, false);
            var detailCloseRect = detailCloseGo.GetComponent<RectTransform>();
            detailCloseRect.anchorMin = new Vector2(1f, 1f);
            detailCloseRect.anchorMax = new Vector2(1f, 1f);
            detailCloseRect.pivot = new Vector2(1f, 1f);
            detailCloseRect.anchoredPosition = new Vector2(-10, -10);
            detailCloseRect.sizeDelta = new Vector2(34, 30);
            detailCloseGo.AddComponent<Image>().color = new Color(0.2f, 0.22f, 0.3f, 0.95f);
            var detailCloseButton = detailCloseGo.AddComponent<Button>();

            var detailCloseText = CreateText(detailCloseGo.transform, "X", font, 16);
            Stretch(detailCloseText.rectTransform);
            detailCloseText.alignment = TextAnchor.MiddleCenter;
            detailCloseText.fontStyle = FontStyle.Bold;

            var portraitGo = new GameObject("SpeciesPortrait", typeof(RectTransform));
            portraitGo.transform.SetParent(detailGo.transform, false);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 1f);
            portraitRect.anchorMax = new Vector2(0.5f, 1f);
            portraitRect.pivot = new Vector2(0.5f, 1f);
            portraitRect.anchoredPosition = new Vector2(0, -60);
            portraitRect.sizeDelta = new Vector2(104, 104);
            var portraitImage = portraitGo.AddComponent<Image>();
            portraitImage.color = new Color(0.65f, 0.95f, 0.75f);
            var detailPortraitMount = portraitGo.AddComponent<SpeciesPortraitMount>();
            SetRef(detailPortraitMount, "portraitRoot", portraitGo.transform);
            SetRef(detailPortraitMount, "fallbackImage", portraitImage);

            var infoText = CreateText(detailGo.transform, "", font, 15);
            var infoRect = infoText.rectTransform;
            infoRect.anchorMin = new Vector2(0, 0.26f);
            infoRect.anchorMax = new Vector2(1, 0.68f);
            infoRect.offsetMin = new Vector2(22, 0);
            infoRect.offsetMax = new Vector2(-22, 0);
            infoText.alignment = TextAnchor.UpperLeft;

            var descriptionText = CreateText(detailGo.transform, "", font, 13);
            var descriptionRect = descriptionText.rectTransform;
            descriptionRect.anchorMin = new Vector2(0, 0);
            descriptionRect.anchorMax = new Vector2(1, 0.24f);
            descriptionRect.offsetMin = new Vector2(22, 16);
            descriptionRect.offsetMax = new Vector2(-22, -4);
            descriptionText.alignment = TextAnchor.UpperLeft;
            descriptionText.color = new Color(0.72f, 0.78f, 0.88f);

            var panel = root.AddComponent<SpeciesPanel>();
            SetRef(panel, "controller", controller);
            SetRef(panel, "panelRoot", panelGo);
            SetRef(panel, "openButton", openButton);
            SetRef(panel, "closeButton", closeButton);
            SetRef(panel, "contentRoot", content.transform);
            SetRef(panel, "speciesEntryPrefab", entryPrefab.GetComponent<SpeciesEntryView>());
            SetRef(panel, "portraitPool", portraitPool);
            SetRef(panel, "emptyText", emptyText);
            SetRef(panel, "detailPanelRoot", detailGo);
            SetRef(panel, "detailCloseButton", detailCloseButton);
            SetRef(panel, "detailPortraitImage", portraitImage);
            SetRef(panel, "detailPortraitMount", detailPortraitMount);
            SetRef(panel, "detailTitleText", detailTitle);
            SetRef(panel, "detailInfoText", infoText);
            SetRef(panel, "detailDescriptionText", descriptionText);

            panelGo.SetActive(false);
            detailGo.SetActive(false);
        }

        private static GameObject LoadOrCreateSpeciesEntryPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SpeciesEntryPrefabPath);
            if (existing != null)
                return existing;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var row = BuildSpeciesEntryObject(font);
            var prefab = PrefabUtility.SaveAsPrefabAsset(row, SpeciesEntryPrefabPath);
            Object.DestroyImmediate(row);
            return prefab;
        }

        private static GameObject RebuildSpeciesEntryPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SpeciesEntryPrefabPath) != null)
                AssetDatabase.DeleteAsset(SpeciesEntryPrefabPath);

            return LoadOrCreateSpeciesEntryPrefab();
        }

        private static GameObject BuildSpeciesEntryObject(Font font)
        {
            var row = new GameObject("SpeciesEntry", typeof(RectTransform));
            var layout = row.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;
            row.AddComponent<LayoutElement>().minHeight = 142;

            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.18f, 0.92f);

            var portraitGo = new GameObject("SpeciesImage", typeof(RectTransform));
            portraitGo.transform.SetParent(row.transform, false);
            var portraitLayout = portraitGo.AddComponent<LayoutElement>();
            portraitLayout.minWidth = 86;
            portraitLayout.minHeight = 86;
            portraitLayout.preferredWidth = 86;
            portraitLayout.preferredHeight = 86;
            var portraitImage = portraitGo.AddComponent<Image>();
            portraitImage.color = new Color(0.65f, 0.95f, 0.75f);
            var portraitButton = portraitGo.AddComponent<Button>();
            var portraitMount = portraitGo.AddComponent<SpeciesPortraitMount>();
            SetRef(portraitMount, "portraitRoot", portraitGo.transform);
            SetRef(portraitMount, "fallbackImage", portraitImage);

            var speciesName = CreateText(row.transform, "Species", font, 14);
            speciesName.color = new Color(0.65f, 0.95f, 0.75f);
            speciesName.alignment = TextAnchor.MiddleCenter;
            speciesName.fontStyle = FontStyle.Bold;
            speciesName.gameObject.AddComponent<LayoutElement>().minHeight = 26;

            var view = row.AddComponent<SpeciesEntryView>();
            var so = new SerializedObject(view);
            so.FindProperty("portraitButton").objectReferenceValue = portraitButton;
            so.FindProperty("portraitImage").objectReferenceValue = portraitImage;
            so.FindProperty("portraitMount").objectReferenceValue = portraitMount;
            so.FindProperty("speciesNameText").objectReferenceValue = speciesName;
            so.ApplyModifiedPropertiesWithoutUndo();

            return row;
        }

        private static GameObject LoadOrCreateUpgradeRowPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(UpgradeRowPrefabPath);
            if (existing != null)
                return existing;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var definition = LoadUpgradeDefinition("click_power");
            var row = BuildUpgradeRowObject(font, "Upgrade Row", definition);
            var prefab = PrefabUtility.SaveAsPrefabAsset(row, UpgradeRowPrefabPath);
            Object.DestroyImmediate(row);
            return prefab;
        }

        private static GameObject LoadOrCreateUpgradePanelPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(UpgradePanelPrefabPath);
            if (existing != null)
                return existing;

            return BuildUpgradePanelPrefab();
        }

        private static GameObject RebuildUpgradeRowPrefab()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var definition = LoadUpgradeDefinition("click_power");
            var row = BuildUpgradeRowObject(font, "Upgrade Row", definition);
            var prefab = PrefabUtility.SaveAsPrefabAsset(row, UpgradeRowPrefabPath);
            Object.DestroyImmediate(row);
            return prefab;
        }

        private static GameObject RebuildUpgradePanelPrefab()
        {
            return BuildUpgradePanelPrefab();
        }

        private static GameObject BuildUpgradePanelPrefab()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rowPrefab = LoadOrCreateUpgradeRowPrefab();

            var panelGo = new GameObject("UpgradePanel");
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-8, 0);
            panelRect.sizeDelta = new Vector2(280, -100);
            panelGo.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f, 0.92f);

            var header = CreateText(panelGo.transform, "Upgrades", font, 18);
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -8);
            headerRect.sizeDelta = new Vector2(-16, 28);
            header.fontStyle = FontStyle.Bold;

            var scrollGo = new GameObject("UpgradeScroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRectTransform = scrollGo.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(8, 8);
            scrollRectTransform.offsetMax = new Vector2(-8, -40);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            Stretch(viewport.AddComponent<RectTransform>());
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;

            var defs = new[]
            {
                LoadUpgradeDefinition("click_power"),
                LoadUpgradeDefinition("click_power_percent"),
                LoadUpgradeDefinition("passive_production"),
                LoadUpgradeDefinition("star_stability"),
                LoadUpgradeDefinition("supernova_bonus"),
                LoadUpgradeDefinition("click_collect"),
                LoadUpgradeDefinition("max_star_count"),
                LoadUpgradeDefinition("advanced_star_stability"),
                LoadUpgradeDefinition("planet_passive_production"),
                LoadUpgradeDefinition("star_passive_production_percent"),
                LoadUpgradeDefinition("collision_dna_production"),
                LoadUpgradeDefinition("entropy_reduction"),
                LoadUpgradeDefinition("species_dna_production"),
                LoadUpgradeDefinition("star_planet_click_value")
            };

            var rowInstances = new System.Collections.Generic.List<UpgradeRow>();

            foreach (var def in defs)
            {
                if (def == null)
                    continue;

                var rowInstance = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, content.transform);
                rowInstance.name = def.upgradeType.ToString();
                var row = rowInstance.GetComponent<UpgradeRow>();
                var so = new SerializedObject(row);
                so.FindProperty("definition").objectReferenceValue = def;
                so.ApplyModifiedPropertiesWithoutUndo();
                rowInstances.Add(row);
            }

            var panelComp = panelGo.AddComponent<UpgradePanel>();
            var panelSo = new SerializedObject(panelComp);
            var rowsProp = panelSo.FindProperty("upgradeRows");
            rowsProp.arraySize = rowInstances.Count;
            for (var i = 0; i < rowInstances.Count; i++)
                rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rowInstances[i];
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(panelGo, UpgradePanelPrefabPath);
            Object.DestroyImmediate(panelGo);
            return prefab;
        }

        private static UpgradeDefinition LoadUpgradeDefinition(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(
                $"{UpgradeFolder}/{assetName}.asset");
        }

        private static UpgradeDefinition LoadOrCreateClickCollectUpgrade()
        {
            var path = $"{UpgradeFolder}/click_collect.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<UpgradeDefinition>();
            def.upgradeType = UpgradeType.ClickCollectRadius;
            def.upgradeTier = GetDefaultUpgradeTier(UpgradeType.ClickCollectRadius);
            def.displayName = "Click Collect";
            def.description = "Vacuum nearby Stardust anywhere you click (+0.45 radius per level)";
            def.baseCost = 22;
            def.costScale = 1.55;
            def.maxLevel = GetDefaultUpgradeMaxLevel(UpgradeType.ClickCollectRadius);
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static GameObject BuildUpgradeRowObject(Font font, string objectName,
            UpgradeDefinition definition)
        {
            var title = definition != null ? definition.displayName : "Upgrade";
            var description = definition != null ? definition.description : string.Empty;

            var row = new GameObject(objectName, typeof(RectTransform));
            var layout = row.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(6, 6, 6, 6);
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 88;

            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.18f, 0.9f);

            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(row.transform, false);
            headerGo.AddComponent<LayoutElement>().minHeight = 32;
            var headerLayout = headerGo.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 6;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = false;
            headerLayout.childControlHeight = false;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            var iconImage = CreateUpgradeIcon(headerGo.transform, "Icon", new Vector2(28f, 28f));
            iconImage.gameObject.AddComponent<LayoutElement>().preferredWidth = 28f;

            var titleText = CreateText(headerGo.transform, $"{title} (Lv 0)", font, 14);
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.UpperLeft;
            var titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.minHeight = 20;
            titleLayout.preferredWidth = 230f;

            var descText = CreateText(row.transform, description, font, 11);
            descText.color = new Color(0.75f, 0.8f, 0.9f);
            descText.alignment = TextAnchor.UpperLeft;
            descText.gameObject.AddComponent<LayoutElement>().minHeight = 32;

            var btnGo = new GameObject("BuyButton", typeof(RectTransform));
            btnGo.transform.SetParent(row.transform, false);
            btnGo.AddComponent<LayoutElement>().minHeight = 30;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.18f, 0.32f, 0.5f);
            var btn = btnGo.AddComponent<Button>();

            var costText = CreateText(btnGo.transform, "Buy (0)", font, 12);
            costText.alignment = TextAnchor.MiddleCenter;
            Stretch(costText.rectTransform);

            var rowComp = row.AddComponent<UpgradeRow>();
            var so = new SerializedObject(rowComp);
            so.FindProperty("definition").objectReferenceValue = definition;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("descriptionText").objectReferenceValue = descText;
            so.FindProperty("iconImage").objectReferenceValue = iconImage;
            so.FindProperty("buyButton").objectReferenceValue = btn;
            so.FindProperty("costText").objectReferenceValue = costText;
            so.FindProperty("buttonImage").objectReferenceValue = btnImg;
            so.ApplyModifiedPropertiesWithoutUndo();

            return row;
        }

        private static Sprite LoadSunSprite()
        {
            foreach (var path in new[] { SunSpritePath, Sun2SpritePath })
            {
                if (!File.Exists(path))
                    continue;

                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in assets)
                {
                    if (asset is Sprite sprite)
                        return sprite;
                }
            }

            return null;
        }

        private static StarView LoadOrCreateStarPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<StarView>(PrefabPath);
            if (existing != null)
                return existing;

            EnsureFolder("Assets/Prefabs");
            var sprite = LoadSunSprite() ?? CreateCircleSprite();
            return BuildStarPrefab(sprite);
        }

        private static StarView BuildStarPrefab(Sprite sprite)
        {
            var go = new GameObject("Star");

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(go.transform, false);
            var glowSr = glowGo.AddComponent<SpriteRenderer>();
            glowSr.sprite = sprite;
            glowSr.sortingOrder = 1;
            glowSr.color = new Color(1f, 0.92f, 0.35f, 0.45f);
            glowGo.transform.localScale = Vector3.one * 1.1f;

            var coreSr = go.AddComponent<SpriteRenderer>();
            coreSr.sprite = sprite;
            coreSr.sortingOrder = 2;
            coreSr.color = Color.white;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 1f;

            var view = go.AddComponent<StarView>();
            SetRef(view, "coreRenderer", coreSr);
            SetRef(view, "glowRenderer", glowSr);
            // popScalePeak / popDuration use component defaults; tune on prefab after create

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<StarView>();
        }

        private static WorldParticle LoadOrCreateStardustParticlePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<WorldParticle>(StardustParticlePrefabPath);
            if (existing != null)
                return existing;

            return BuildParticlePrefab(
                StardustParticlePrefabPath,
                "StardustParticle",
                Color.white,
                0.12f,
                tintByStage: true,
                scaleByValue: true,
                spawnScatter: 0.35f);
        }

        private static WorldParticle LoadOrCreateDnaParticlePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<WorldParticle>(DnaParticlePrefabPath);
            if (existing != null)
                return existing;

            return BuildParticlePrefab(
                DnaParticlePrefabPath,
                "DnaParticle",
                new Color(0.55f, 1f, 0.75f),
                0.22f,
                tintByStage: false,
                scaleByValue: false,
                spawnScatter: 0.2f);
        }

        private static WorldParticle BuildParticlePrefab(string path, string objectName, Color color,
            float scale, bool tintByStage, bool scaleByValue, float spawnScatter)
        {
            var go = new GameObject(objectName);
            go.transform.localScale = Vector3.one * scale;

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateCircleSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 20;

            var particle = go.AddComponent<WorldParticle>();
            var so = new SerializedObject(particle);
            so.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            so.FindProperty("tintByStage").boolValue = tintByStage;
            so.FindProperty("scaleByValue").boolValue = scaleByValue;
            so.FindProperty("spawnScatter").floatValue = spawnScatter;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<WorldParticle>();
        }

        private static ParticleSystem LoadOrCreateClickEffectPrefab() =>
            LoadOrCreateVfxPrefab(ClickEffectPrefabPath, "ClickEffect", burstCount: 14, startSize: 0.14f, startSpeed: 1.6f, lifetime: 0.45f, radius: 0.22f);

        private static ParticleSystem LoadOrCreateStardustEmitEffectPrefab() =>
            LoadOrCreateVfxPrefab(StardustEmitEffectPrefabPath, "StardustEmitEffect", burstCount: 8, startSize: 0.1f, startSpeed: 1.1f, lifetime: 0.55f, radius: 0.18f);

        private static ParticleSystem LoadOrCreateSupernovaEffectPrefab() =>
            LoadOrCreateVfxPrefab(SupernovaEffectPrefabPath, "SupernovaEffect", burstCount: 36, startSize: 0.22f, startSpeed: 2.4f, lifetime: 0.7f, radius: 0.45f);

        private static ParticleSystem LoadOrCreateVfxPrefab(string path, string objectName, int burstCount,
            float startSize, float startSpeed, float lifetime, float radius)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ParticleSystem>(path);
            if (existing != null)
                return existing;

            var go = new GameObject(objectName);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = Color.white;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ParticleSystem>();
        }

        private static Sprite CreateCircleSprite()
        {
            var texPath = $"{ArtPath}/Circle.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
            if (existing != null)
                return existing;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = size / 2f;
            var radius = size / 2f - 1;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            var png = tex.EncodeToPNG();
            var absolutePath = Path.GetFullPath(texPath);
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(absolutePath, png);
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);

            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            assetPath = assetPath.Replace('\\', '/');
            var parts = assetPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static Text CreateText(Transform parent, string content, Font font, int size)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void AnchorTop(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static void AnchorCenter(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.boolValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.stringValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
                return;

            var scenes = EditorBuildSettings.scenes;
            foreach (var scene in scenes)
            {
                if (scene.path == scenePath)
                {
                    scene.enabled = true;
                    return;
                }
            }

            var next = new EditorBuildSettingsScene[scenes.Length + 1];
            for (var i = 0; i < scenes.Length; i++)
                next[i] = scenes[i];

            next[next.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = next;
        }

        private static void SetEnum(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.enumValueIndex = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CreateSingleStarUpgradeDefinitions()
        {
            CreateUpgradeDefinition(UpgradeType.ClickPower, "click_power",
                "Click Power", "More Stardust per click. Higher levels add bigger flat bonuses.");
            CreateUpgradeDefinition(UpgradeType.ClickPowerPercent, "click_power_percent",
                "Click Power Percent", "Multiplies Stardust gained from star clicks by a percentage per level.");
            CreateUpgradeDefinition(UpgradeType.PassiveProduction, "passive_production",
                "Passive Production", "More Stardust per second. Higher levels add bigger flat bonuses.");
            CreateUpgradeDefinition(UpgradeType.StarStability, "star_stability",
                "Star Stability", "Star ages slower from clicks and passive");
            CreateUpgradeDefinition(UpgradeType.SupernovaBonus, "supernova_bonus",
                "Supernova Bonus", "+15 Supernova reward per level");
            CreateUpgradeDefinition(UpgradeType.MaxPlanetCount, "max_planet_count",
                "Max Planet Count", "+1 max planet orbit slot per level");
            CreateUpgradeDefinition(UpgradeType.AutoPlanetFormation, "auto_planet_formation",
                "Auto Planet Formation", "Chance to spawn a free random planet over time");
            CreateUpgradeDefinition(UpgradeType.PlanetDnaChance, "planet_dna_chance",
                "Planet DNA Chance", "Planets generate more DNA Potential over time");
            CreateUpgradeDefinition(UpgradeType.PlanetClickValue, "planet_click_value",
                "Planet Click Value", "More Stardust when clicking planets");
            CreateUpgradeDefinition(UpgradeType.HabitablePlanetChance, "habitable_planet_chance",
                "Habitable Planet Chance", "New planets are more likely to support life");
            CreateUpgradeDefinition(UpgradeType.MaxStarCount, "max_star_count",
                "Max Star Count", "Unlocks one additional active star slot per level, up to five stars.");
            CreateUpgradeDefinition(UpgradeType.AdvancedStarStability, "advanced_star_stability",
                "Advanced Star Stability", "Further slows star aging after basic stability is developed.");
            CreateUpgradeDefinition(UpgradeType.PlanetPassiveProduction, "planet_passive_production",
                "Planet Passive Production", "Planets orbiting stars produce passive Stardust each tick.");
            CreateUpgradeDefinition(UpgradeType.StarPassiveProductionPercent, "star_passive_production_percent",
                "Star Passive Production Percentage", "Increases each star's passive Stardust production by a percentage.");
            CreateUpgradeDefinition(UpgradeType.CollisionDnaProduction, "collision_dna_production",
                "Collision DNA Production", "Increases DNA Potential gained when celestial objects collide.");
            CreateUpgradeDefinition(UpgradeType.EntropyReduction, "entropy_reduction",
                "Entropy Reduction", "Reduces the rate at which entropy increases.");
            CreateUpgradeDefinition(UpgradeType.SpeciesDnaProduction, "species_dna_production",
                "Species DNA Production", "Species generate passive DNA Potential on their home planets.");
            CreateUpgradeDefinition(UpgradeType.StarPlanetClickValue, "star_planet_click_value",
                "Planet-Powered Clicks", "Stars gain click Stardust from each planet orbiting them.");
            CreateExpandUniverseDefinition();
        }

        private static void CreateExpandUniverseDefinition()
        {
            var path = $"{UpgradeFolder}/expand_universe.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
            if (existing != null)
                return;

            var def = ScriptableObject.CreateInstance<UpgradeDefinition>();
            def.upgradeType = UpgradeType.ExpandUniverse;
            def.upgradeTier = GetDefaultUpgradeTier(UpgradeType.ExpandUniverse);
            def.displayName = "Expand Universe";
            def.description =
                "Unlock Step 2: Multi-Star System Age with star slots, buying new stars, independent star lifecycles, and planets around selected stars.";
            def.baseCost = 500;
            def.costScale = 1f;
            def.maxLevel = 1;
            AssetDatabase.CreateAsset(def, path);
        }

        private static UpgradeDefinition CreateUpgradeDefinition(
            UpgradeType type, string assetName, string displayName, string description)
        {
            var path = $"{UpgradeFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<UpgradeDefinition>();
            def.upgradeType = type;
            def.upgradeTier = GetDefaultUpgradeTier(type);
            def.displayName = displayName;
            def.description = description;
            def.baseCost = 20;
            def.costScale = 1.5;
            def.maxLevel = GetDefaultUpgradeMaxLevel(type);
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static int GetDefaultUpgradeMaxLevel(UpgradeType type) => type switch
        {
            UpgradeType.ClickPower => 25,
            UpgradeType.ClickPowerPercent => 15,
            UpgradeType.PassiveProduction => 20,
            UpgradeType.StarStability => 10,
            UpgradeType.SupernovaBonus => 10,
            UpgradeType.ClickCollectRadius => 5,
            UpgradeType.MaxPlanetCount => 5,
            UpgradeType.AutoPlanetFormation => 10,
            UpgradeType.PlanetDnaChance => 10,
            UpgradeType.PlanetClickValue => 20,
            UpgradeType.HabitablePlanetChance => 10,
            UpgradeType.MaxStarCount => 4,
            UpgradeType.AdvancedStarStability => 10,
            UpgradeType.PlanetPassiveProduction => 15,
            UpgradeType.StarPlanetClickValue => 15,
            UpgradeType.StarPassiveProductionPercent => 15,
            UpgradeType.CollisionDnaProduction => 15,
            UpgradeType.EntropyReduction => 10,
            UpgradeType.SpeciesDnaProduction => 15,
            UpgradeType.ExpandUniverse => 1,
            _ => 0
        };

        private static int GetDefaultUpgradeTier(UpgradeType type) => type switch
        {
            UpgradeType.ClickPowerPercent => 2,
            UpgradeType.SupernovaBonus => 2,
            UpgradeType.ClickCollectRadius => 2,
            UpgradeType.MaxStarCount => 2,
            UpgradeType.AdvancedStarStability => 2,
            UpgradeType.PlanetPassiveProduction => 2,
            UpgradeType.StarPlanetClickValue => 2,
            UpgradeType.StarPassiveProductionPercent => 2,
            UpgradeType.CollisionDnaProduction => 2,
            UpgradeType.EntropyReduction => 2,
            UpgradeType.SpeciesDnaProduction => 2,
            _ => 1
        };

        private static GameObject RebuildSingleStarUpgradePanelPrefab()
        {
            CreateSingleStarUpgradeDefinitions();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rowPrefab = RebuildUpgradeRowPrefab();

            var panelGo = new GameObject("UpgradePanel");
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-8, 0);
            panelRect.sizeDelta = new Vector2(280, -100);
            panelGo.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f, 0.92f);

            var header = CreateText(panelGo.transform, "Upgrades", font, 18);
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -8);
            headerRect.sizeDelta = new Vector2(-16, 28);
            header.fontStyle = FontStyle.Bold;

            var scrollGo = new GameObject("UpgradeScroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRectTransform = scrollGo.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(8, 8);
            scrollRectTransform.offsetMax = new Vector2(-8, -40);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            Stretch(viewport.AddComponent<RectTransform>());
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;

            var defs = new[]
            {
                LoadUpgradeDefinition("click_power"),
                LoadUpgradeDefinition("click_power_percent"),
                LoadUpgradeDefinition("passive_production"),
                LoadUpgradeDefinition("star_stability"),
                LoadUpgradeDefinition("max_planet_count"),
                LoadUpgradeDefinition("auto_planet_formation"),
                LoadUpgradeDefinition("planet_dna_chance"),
                LoadUpgradeDefinition("planet_click_value"),
                LoadUpgradeDefinition("habitable_planet_chance"),
                LoadUpgradeDefinition("max_star_count"),
                LoadUpgradeDefinition("advanced_star_stability"),
                LoadUpgradeDefinition("planet_passive_production"),
                LoadUpgradeDefinition("star_passive_production_percent"),
                LoadUpgradeDefinition("collision_dna_production"),
                LoadUpgradeDefinition("entropy_reduction"),
                LoadUpgradeDefinition("species_dna_production"),
                LoadUpgradeDefinition("star_planet_click_value"),
                LoadUpgradeDefinition("expand_universe")
            };

            var rowInstances = new System.Collections.Generic.List<UpgradeRow>();
            foreach (var def in defs)
            {
                if (def == null)
                    continue;

                var rowInstance = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, content.transform);
                rowInstance.name = def.upgradeType.ToString();
                var row = rowInstance.GetComponent<UpgradeRow>();
                var so = new SerializedObject(row);
                so.FindProperty("definition").objectReferenceValue = def;
                so.ApplyModifiedPropertiesWithoutUndo();
                rowInstances.Add(row);
            }

            var panelComp = panelGo.AddComponent<UpgradePanel>();
            var panelSo = new SerializedObject(panelComp);
            var rowsProp = panelSo.FindProperty("upgradeRows");
            rowsProp.arraySize = rowInstances.Count;
            for (var i = 0; i < rowInstances.Count; i++)
                rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rowInstances[i];
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(panelGo, UpgradePanelPrefabPath);
            Object.DestroyImmediate(panelGo);
            return prefab;
        }

        private static PlanetView LoadOrCreatePlanetPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PlanetView>(PlanetPrefabPath);
            if (existing != null)
            {
                EnsurePlanetNameTooltipOnPrefab();
                return existing;
            }

            var go = new GameObject("Planet");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = new Color(0.5f, 0.65f, 0.9f);
            sr.sortingOrder = 5;
            go.transform.localScale = Vector3.one * 0.22f;
            go.AddComponent<CircleCollider2D>().radius = 0.5f;
            var view = go.AddComponent<PlanetView>();
            AddPlanetNameTooltip(go);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlanetPrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<PlanetView>();
        }

        private static void EnsurePlanetNameTooltipOnPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PlanetPrefabPath);
            try
            {
                if (root.GetComponent<PlanetNameTooltip>() == null)
                {
                    AddPlanetNameTooltip(root);
                    PrefabUtility.SaveAsPrefabAsset(root, PlanetPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddPlanetNameTooltip(GameObject planetRoot)
        {
            var labelGo = new GameObject("PlanetNameLabel");
            labelGo.transform.SetParent(planetRoot.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            labelGo.transform.localScale = Vector3.one;

            var label = labelGo.AddComponent<TextMesh>();
            label.text = "Planet";
            label.fontSize = 36;
            label.characterSize = 0.035f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            label.GetComponent<MeshRenderer>().sortingOrder = 20;
            labelGo.SetActive(false);

            var tooltip = planetRoot.AddComponent<PlanetNameTooltip>();
            SetRef(tooltip, "label", label);
        }

        private static PlanetTypeCatalog LoadOrCreatePlanetTypeCatalog()
        {
            CreateAllPlanetTypeDefinitions();

            var catalog = AssetDatabase.LoadAssetAtPath<PlanetTypeCatalog>(PlanetTypeCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PlanetTypeCatalog>();
                AssetDatabase.CreateAsset(catalog, PlanetTypeCatalogPath);
            }

            var so = new SerializedObject(catalog);
            var listProp = so.FindProperty("planetTypes");
            listProp.ClearArray();

            var types = new[]
            {
                PlanetType.Rocky,
                PlanetType.Ocean,
                PlanetType.Lava,
                PlanetType.Ice,
                PlanetType.GasGiant,
                PlanetType.Toxic,
                PlanetType.Crystal,
                PlanetType.Desert,
                PlanetType.Forest
            };

            for (var i = 0; i < types.Length; i++)
            {
                var def = LoadPlanetTypeDefinition(types[i]);
                if (def == null)
                    continue;

                listProp.InsertArrayElementAtIndex(i);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = def;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void CreateAllPlanetTypeDefinitions()
        {
            CreatePlanetTypeDefinition(PlanetType.Rocky, "rocky", "Rocky Planet",
                "Balanced Stardust, medium habitability.");
            CreatePlanetTypeDefinition(PlanetType.Ocean, "ocean", "Ocean Planet",
                "Lower Stardust, high habitability and life chance.");
            CreatePlanetTypeDefinition(PlanetType.Lava, "lava", "Lava Planet",
                "High Stardust, low habitability, fragile.");
            CreatePlanetTypeDefinition(PlanetType.Ice, "ice", "Ice Planet",
                "Low Stardust, durable, medium-low habitability.");
            CreatePlanetTypeDefinition(PlanetType.GasGiant, "gas_giant", "Gas Giant",
                "High Stardust, cannot civilize.");
            CreatePlanetTypeDefinition(PlanetType.Toxic, "toxic", "Toxic Planet",
                "Mutation-friendly DNA if life appears.");
            CreatePlanetTypeDefinition(PlanetType.Crystal, "crystal", "Crystal Planet",
                "Rare, high value, fragile, strong DNA.");
            CreatePlanetTypeDefinition(PlanetType.Desert, "desert", "Desert Planet",
                "Balanced desert world.");
            CreatePlanetTypeDefinition(PlanetType.Forest, "forest", "Forest Planet",
                "High habitability and civilization chance.");
        }

        private static PlanetTypeDefinition CreatePlanetTypeDefinition(
            PlanetType type, string assetName, string displayName, string description)
        {
            var path = $"{PlanetTypesFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PlanetTypeDefinition>(path);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<PlanetTypeDefinition>();
            def.planetType = type;
            def.displayName = displayName;
            def.description = description;
            var temp = PlanetTypeUtility.CreateFallbackDefinition(type);
            def.baseClickValue = temp.baseClickValue;
            def.habitability = temp.habitability;
            def.dnaChance = temp.dnaChance;
            def.maxDurability = temp.maxDurability;
            def.canCivilize = temp.canCivilize;
            def.spawnWeight = temp.spawnWeight;
            def.planetColor = temp.planetColor;
            Object.DestroyImmediate(temp);

            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static PlanetTypeDefinition LoadPlanetTypeDefinition(PlanetType type)
        {
            var assetName = type switch
            {
                PlanetType.GasGiant => "gas_giant",
                _ => type.ToString().ToLowerInvariant()
            };

            return AssetDatabase.LoadAssetAtPath<PlanetTypeDefinition>(
                $"{PlanetTypesFolder}/{assetName}.asset");
        }

        private static SingleStarBalance LoadOrCreateSingleStarBalance()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SingleStarBalance>(SingleStarBalancePath);
            if (existing != null)
                return existing;

            var balance = ScriptableObject.CreateInstance<SingleStarBalance>();
            AssetDatabase.CreateAsset(balance, SingleStarBalancePath);
            return balance;
        }

        private static void BuildSingleStarStepUi(GameObject canvasGo, GameController controller,
            HUD hud)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var existingEnd = canvasGo.transform.Find("StarSystemEndPanel");
            if (existingEnd != null)
                Object.DestroyImmediate(existingEnd.gameObject);

            var existingUpgrade = canvasGo.transform.Find("UpgradePanel");
            if (existingUpgrade != null)
                Object.DestroyImmediate(existingUpgrade.gameObject);

            var upgradePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UpgradePanelPrefabPath);
            if (upgradePrefab != null)
            {
                var upgradeGo = (GameObject)PrefabUtility.InstantiatePrefab(upgradePrefab, canvasGo.transform);
                upgradeGo.name = "UpgradePanel";
                var panel = upgradeGo.GetComponent<UpgradePanel>();
                if (panel != null)
                    SetRef(panel, "controller", controller);
            }

            var endPanelGo = new GameObject("StarSystemEndPanel");
            endPanelGo.transform.SetParent(canvasGo.transform, false);
            Stretch(endPanelGo.AddComponent<RectTransform>());
            endPanelGo.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.9f);

            var card = new GameObject("Card");
            card.transform.SetParent(endPanelGo.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(520, 520);
            card.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.16f, 0.98f);

            var title = CreateText(card.transform, "Star System Ended", font, 28);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(-32, 40);
            title.fontStyle = FontStyle.Bold;

            var summary = CreateText(card.transform, "", font, 17);
            var summaryRect = summary.rectTransform;
            summaryRect.anchorMin = new Vector2(0, 0.2f);
            summaryRect.anchorMax = new Vector2(1, 0.88f);
            summaryRect.offsetMin = new Vector2(24, 0);
            summaryRect.offsetMax = new Vector2(-24, 0);
            summary.alignment = TextAnchor.UpperLeft;

            var newBtnGo = new GameObject("NewStarSystemButton");
            newBtnGo.transform.SetParent(card.transform, false);
            var newBtnRect = newBtnGo.AddComponent<RectTransform>();
            newBtnRect.anchorMin = new Vector2(0.5f, 0f);
            newBtnRect.anchorMax = new Vector2(0.5f, 0f);
            newBtnRect.anchoredPosition = new Vector2(0, 24);
            newBtnRect.sizeDelta = new Vector2(260, 44);
            newBtnGo.AddComponent<Image>().color = new Color(0.2f, 0.45f, 0.65f);
            var newBtn = newBtnGo.AddComponent<Button>();
            var newLabel = CreateText(newBtnGo.transform, "Start New Star System", font, 18);
            Stretch(newLabel.rectTransform);
            newLabel.alignment = TextAnchor.MiddleCenter;

            var endPanel = endPanelGo.AddComponent<StarSystemEndPanel>();
            SetRef(endPanel, "controller", controller);
            SetRef(endPanel, "panelRoot", endPanelGo);
            SetRef(endPanel, "titleText", title);
            SetRef(endPanel, "summaryText", summary);
            SetRef(endPanel, "newSystemButton", newBtn);
            endPanelGo.SetActive(false);

            var entropyPanel = canvasGo.transform.Find("EntropyPanel");
            if (entropyPanel != null && hud != null)
                SetRef(hud, "entropyPanelRoot", entropyPanel.gameObject);

            if (hud != null)
                SetRef(hud, "starSystemEndPanel", endPanel);

            SetEnum(controller, "gameplayMode", (int)GameplayMode.SingleStarSystemAge);
        }
    }
}
#endif
