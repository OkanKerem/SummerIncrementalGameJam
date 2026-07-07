#if UNITY_EDITOR
using System.IO;
using Universes.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Universes.Editor
{
    public static class PrototypeSceneEditor
    {
        private const string ScenePath = "Assets/Scenes/PrototypeScene.unity";
        private const string PrefabPath = "Assets/Prefabs/Prototype/Star.prefab";
        private const string UpgradeRowPrefabPath = "Assets/Prefabs/Prototype/UpgradeRow.prefab";
        private const string UpgradePanelPrefabPath = "Assets/Prefabs/Prototype/UpgradePanel.prefab";
        private const string StardustParticlePrefabPath = "Assets/Prefabs/Prototype/StardustParticle.prefab";
        private const string DnaParticlePrefabPath = "Assets/Prefabs/Prototype/DnaParticle.prefab";
        private const string ClickEffectPrefabPath = "Assets/Prefabs/Prototype/ClickEffect.prefab";
        private const string StardustEmitEffectPrefabPath = "Assets/Prefabs/Prototype/StardustEmitEffect.prefab";
        private const string SupernovaEffectPrefabPath = "Assets/Prefabs/Prototype/SupernovaEffect.prefab";
        private const string PrototypeUpgradeFolder = "Assets/ScriptableObjects/Prototype";
        private const string PrototypePrestigeFolder = "Assets/ScriptableObjects/Prototype/Prestige";
        private const string PrestigeRowPrefabPath = "Assets/Prefabs/Prototype/PrestigeRow.prefab";
        private const string PrestigePanelPrefabPath = "Assets/Prefabs/Prototype/PrestigePanel.prefab";
        private const string PlanetPrefabPath = "Assets/Prefabs/Prototype/Planet.prefab";
        private const string SpeciesEntryPrefabPath = "Assets/Prefabs/Prototype/SpeciesEntry.prefab";
        private const string PrototypePlanetTypesFolder = "Assets/ScriptableObjects/Prototype/Planets";
        private const string PlanetTypeCatalogPath = "Assets/ScriptableObjects/Prototype/Planets/PlanetTypeCatalog.asset";
        private const string SingleStarBalancePath = "Assets/ScriptableObjects/Prototype/SingleStarBalance.asset";
        private const string SunSpritePath = "Assets/Art/Sprites/Sun/sun1.png";
        private const string Sun2SpritePath = "Assets/Art/Sprites/Sun/sun2.png";
        private const string ArtPath = "Assets/Art/Sprites";

        [MenuItem("Universes/Add Prototype Step 4 UI To Open Scene")]
        public static void AddStep4UiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<PrototypeGameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need PrototypeGame and Canvas in the open scene.");
                return;
            }

            var hud = canvas.GetComponent<PrototypeHUD>();
            if (hud == null)
            {
                Debug.LogWarning("PrototypeHUD not found on Canvas.");
                return;
            }

            var stardust = canvas.transform.Find("StardustText")?.GetComponent<Text>();
            var dna = canvas.transform.Find("DnaText")?.GetComponent<Text>();
            BuildStep4Ui(canvas.gameObject, controller, hud, stardust, dna);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("DNA display, particle collectors, and black hole status added.");
        }

        [MenuItem("Universes/Add Prototype Single Star Step UI To Open Scene")]
        public static void AddSingleStarStepUiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<PrototypeGameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need PrototypeGame and Canvas in the open scene.");
                return;
            }

            var hud = canvas.GetComponent<PrototypeHUD>();
            if (hud == null)
            {
                Debug.LogWarning("PrototypeHUD not found on Canvas.");
                return;
            }

            CreateSingleStarUpgradeDefinitions();
            var planetPrefab = LoadOrCreatePlanetPrefab();
            var planetCatalog = LoadOrCreatePlanetTypeCatalog();
            var singleStarBalance = LoadOrCreateSingleStarBalance();
            RebuildSingleStarUpgradePanelPrefab();
            RebuildSpeciesEntryPrefab();

            var planetManager = controller.GetComponent<PrototypePlanetManager>();
            if (planetManager == null)
                planetManager = controller.gameObject.AddComponent<PrototypePlanetManager>();

            SetRef(planetManager, "planetPrefab", planetPrefab);
            SetRef(planetManager, "planetTypeCatalog", planetCatalog);
            SetRef(controller, "planetManager", planetManager);
            SetRef(controller, "planetTypeCatalog", planetCatalog);
            SetRef(controller, "singleStarBalance", singleStarBalance);
            SetEnum(controller, "gameplayMode", (int)PrototypeGameplayMode.SingleStarSystemAge);

            BuildSingleStarStepUi(canvas.gameObject, controller, hud);
            BuildSpeciesUi(canvas.gameObject, controller);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Single Star System Age UI: planet button, upgrades, species panel, and star system end panel.");
        }

        [MenuItem("Universes/Add Prototype Species UI To Open Scene")]
        public static void AddSpeciesUiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<PrototypeGameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need PrototypeGame and Canvas in the open scene.");
                return;
            }

            RebuildSpeciesEntryPrefab();
            BuildSpeciesUi(canvas.gameObject, controller);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Species button and species panel added without changing other scene UI.");
        }

        [MenuItem("Universes/Create Prototype Planet Prefab")]
        public static void CreatePlanetPrefabMenu()
        {
            EnsureFolder("Assets/Prefabs/Prototype");
            LoadOrCreatePlanetPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log($"Planet prefab ready at {PlanetPrefabPath}");
        }

        [MenuItem("Universes/Create Prototype Species UI Prefabs")]
        public static void CreateSpeciesUiPrefabsMenu()
        {
            EnsureFolder("Assets/Prefabs/Prototype");
            RebuildSpeciesEntryPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log($"Species entry prefab ready at {SpeciesEntryPrefabPath}");
        }

        [MenuItem("Universes/Create Prototype Single Star Balance Asset")]
        public static void CreateSingleStarBalanceMenu()
        {
            EnsureFolder(PrototypeUpgradeFolder);
            var balance = LoadOrCreateSingleStarBalance();
            AssetDatabase.SaveAssets();
            Debug.Log($"Single Star balance ready at {SingleStarBalancePath}. Tune star age, click power, orbits, and planet rules in the Inspector.");
            Selection.activeObject = balance;
        }

        [MenuItem("Universes/Create Prototype Planet Type Assets")]
        public static void CreatePlanetTypeAssetsMenu()
        {
            EnsureFolder(PrototypePlanetTypesFolder);
            var catalog = LoadOrCreatePlanetTypeCatalog();
            AssetDatabase.SaveAssets();
            Debug.Log($"Planet type assets ready under {PrototypePlanetTypesFolder}. Edit sprites, colors, and effect prefabs per type in the Inspector.");
            Selection.activeObject = catalog;
        }

        [MenuItem("Universes/Create Prototype Prestige Assets")]
        public static void CreatePrestigeAssetsMenu()
        {
            EnsureFolder(PrototypePrestigeFolder);
            EnsureFolder("Assets/Prefabs/Prototype");
            CreateAllPrestigeUpgradeDefinitions();
            LoadOrCreatePrestigeRowPrefab();
            RebuildPrestigePanelPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log($"Prestige assets ready under {PrototypePrestigeFolder} and {PrestigePanelPrefabPath}.");
        }

        [MenuItem("Universes/Add Prototype Prestige UI To Open Scene")]
        public static void AddPrestigeUiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<PrototypeGameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need PrototypeGame and Canvas in the open scene.");
                return;
            }

            var hud = canvas.GetComponent<PrototypeHUD>();
            if (hud == null)
            {
                Debug.LogWarning("PrototypeHUD not found on Canvas.");
                return;
            }

            CreateAllPrestigeUpgradeDefinitions();
            BuildStep5Ui(canvas.gameObject, controller, hud);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Universe DNA display, prestige panel, and collapse DNA breakdown added.");
        }

        [MenuItem("Universes/Add Prototype Step 3 UI To Open Scene")]
        public static void AddStep3UiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<PrototypeGameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need PrototypeGame and Canvas in the open scene.");
                return;
            }

            var hud = canvas.GetComponent<PrototypeHUD>();
            if (hud == null)
            {
                Debug.LogWarning("PrototypeHUD not found on Canvas.");
                return;
            }

            BuildStep3Ui(canvas.gameObject, controller, hud);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Entropy bar, collapse controls, and run summary panel added.");
        }

        [MenuItem("Universes/Add Prototype Step 2 UI To Open Scene")]
        public static void AddStep2UiToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<PrototypeGameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need PrototypeGame and Canvas in the open scene.");
                return;
            }

            var hud = canvas.GetComponent<PrototypeHUD>();
            if (hud == null)
            {
                Debug.LogWarning("PrototypeHUD not found on Canvas.");
                return;
            }

            BuildUpgradePanel(canvas.gameObject, controller);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Upgrade panel added. Stats line shows click/passive rates.");
        }

        [MenuItem("Universes/Add Prototype VFX To Open Scene")]
        public static void AddVfxToOpenScene()
        {
            var controller = Object.FindAnyObjectByType<PrototypeGameController>();
            var canvas = Object.FindAnyObjectByType<Canvas>();

            if (controller == null || canvas == null)
            {
                Debug.LogWarning("Need PrototypeGame and Canvas in the open scene.");
                return;
            }

            var floater = canvas.GetComponent<PrototypeFloatingTextSpawner>();
            if (floater == null)
                floater = canvas.gameObject.AddComponent<PrototypeFloatingTextSpawner>();

            SetRef(floater, "canvas", canvas);
            SetRef(controller, "floatingTextSpawner", floater);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Floating stardust text spawner added.");
        }

        [MenuItem("Universes/Create Prototype Particle Prefabs")]
        public static void CreateParticlePrefabsMenu()
        {
            EnsureFolder("Assets/Prefabs/Prototype");
            LoadOrCreateStardustParticlePrefab();
            LoadOrCreateDnaParticlePrefab();
            LoadOrCreateClickEffectPrefab();
            LoadOrCreateStardustEmitEffectPrefab();
            LoadOrCreateSupernovaEffectPrefab();
            LoadOrCreateClickCollectUpgrade();
            AssetDatabase.SaveAssets();
            Debug.Log("Particle prefabs and VFX ready under Assets/Prefabs/Prototype/. Edit collectible sprites and ParticleSystem bursts in the prefabs.");
        }

        [MenuItem("Universes/Create Prototype Upgrade Prefabs")]
        public static void CreateUpgradePrefabsMenu()
        {
            EnsureFolder("Assets/Prefabs/Prototype");
            LoadOrCreateClickCollectUpgrade();
            LoadOrCreateUpgradeRowPrefab();
            RebuildUpgradePanelPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log($"Upgrade prefabs ready at {UpgradeRowPrefabPath} and {UpgradePanelPrefabPath}. Re-run Step 2 UI menu on your scene if the panel was already placed.");
        }

        [MenuItem("Universes/Create Prototype Star Prefab")]
        public static void CreateStarPrefabMenu()
        {
            EnsureFolder("Assets/Prefabs/Prototype");

            if (AssetDatabase.LoadAssetAtPath<PrototypeStarView>(PrefabPath) != null)
            {
                Debug.Log($"Star prefab already exists at {PrefabPath}. Edit it in the Inspector — setup will not overwrite it.");
                return;
            }

            var sprite = LoadSunSprite() ?? CreateCircleSprite();
            BuildStarPrefab(sprite);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created star prefab at {PrefabPath}. Tune glow scale, sprites, and collider in the prefab.");
        }

        [MenuItem("Universes/Setup Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildPrototypeScene();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"Prototype scene saved to {ScenePath}. Edit objects in the Hierarchy, then press Play.");
        }

        [MenuItem("Universes/Setup Prototype In Open Scene")]
        public static void SetupInOpenScene()
        {
            BuildPrototypeScene();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Prototype objects added to the open scene. Edit in the Hierarchy, then press Play.");
        }

        private static void BuildPrototypeScene()
        {
            EnsureFolder("Assets/Prefabs/Prototype");
            EnsureFolder(ArtPath);

            var starPrefab = LoadOrCreateStarPrefab();

            SetupCamera();
            var worldRoot = new GameObject("WorldRoot").transform;

            var gameGo = new GameObject("PrototypeGame");
            var controller = gameGo.AddComponent<PrototypeGameController>();
            var particles = gameGo.AddComponent<PrototypeCosmicParticleManager>();
            var effects = gameGo.AddComponent<PrototypeParticleEffectManager>();
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

        private static PrototypeFloatingTextSpawner BuildHud(PrototypeGameController controller)
        {
            var existingCanvas = Object.FindAnyObjectByType<Canvas>();
            if (existingCanvas != null && existingCanvas.GetComponent<PrototypeHUD>() != null)
                Object.DestroyImmediate(existingCanvas.gameObject);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<PrototypeHUD>();
            var floater = canvasGo.AddComponent<PrototypeFloatingTextSpawner>();
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

        private static PrototypeCollapsePanel BuildStep3Ui(GameObject canvasGo, PrototypeGameController controller,
            PrototypeHUD hud)
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

            var collapsePanel = collapsePanelGo.AddComponent<PrototypeCollapsePanel>();
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

        private static void BuildStep4Ui(GameObject canvasGo, PrototypeGameController controller,
            PrototypeHUD hud, Text stardustText, Text dnaText)
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

            var particles = controller.GetComponent<PrototypeCosmicParticleManager>();
            if (particles == null)
                particles = controller.gameObject.AddComponent<PrototypeCosmicParticleManager>();

            var effects = controller.GetComponent<PrototypeParticleEffectManager>();
            if (effects == null)
                effects = controller.gameObject.AddComponent<PrototypeParticleEffectManager>();

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

        private static void BuildStep5Ui(GameObject canvasGo, PrototypeGameController controller, PrototypeHUD hud)
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

            var collapsePanel = Object.FindAnyObjectByType<PrototypeCollapsePanel>(FindObjectsInactive.Include);
            var prestigePanel = prestigeGo.GetComponent<PrototypePrestigePanel>();
            if (prestigePanel != null)
                SetRef(prestigePanel, "controller", controller);
            if (collapsePanel != null)
                SetRef(collapsePanel, "prestigePanel", prestigePanel);

            if (hud != null)
                SetRef(hud, "universeDnaText", universeDna);
        }

        private static void CreateAllPrestigeUpgradeDefinitions()
        {
            CreatePrestigeDefinition(PrototypePrestigeUpgradeType.StrongerBigBang, "stronger_big_bang",
                "Stronger Big Bang", "New universes start with bonus Stardust.");
            CreatePrestigeDefinition(PrototypePrestigeUpgradeType.StablePhysics, "stable_physics",
                "Stable Physics", "Reduces Entropy gain from all sources.");
            CreatePrestigeDefinition(PrototypePrestigeUpgradeType.LongerStarLifespan, "longer_star_lifespan",
                "Longer Star Lifespan", "Stars age more slowly from clicks and passive production.");
            CreatePrestigeDefinition(PrototypePrestigeUpgradeType.SupernovaMemory, "supernova_memory",
                "Supernova Memory", "Supernovas are more likely to create DNA Fragments.");
            CreatePrestigeDefinition(PrototypePrestigeUpgradeType.BlackHoleMemory, "black_hole_memory",
                "Black Hole Memory", "Black Holes generate more DNA potential while alive.");
            CreatePrestigeDefinition(PrototypePrestigeUpgradeType.CosmicEfficiency, "cosmic_efficiency",
                "Cosmic Efficiency", "Increases all Stardust production.");
            CreatePrestigeDefinition(PrototypePrestigeUpgradeType.ParticleEvolution, "particle_evolution",
                "Particle Evolution", "Higher chance for stars to produce larger Stardust particles.");
            CreatePrestigeDefinition(PrototypePrestigeUpgradeType.ParallelEcho, "parallel_echo",
                "Parallel Echo", "Past collapsed universes echo passive Stardust into the new universe.");
        }

        private static PrototypePrestigeUpgradeDefinition CreatePrestigeDefinition(
            PrototypePrestigeUpgradeType type, string assetName, string displayName, string description)
        {
            var path = $"{PrototypePrestigeFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PrototypePrestigeUpgradeDefinition>(path);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<PrototypePrestigeUpgradeDefinition>();
            def.upgradeType = type;
            def.displayName = displayName;
            def.description = description;
            def.maxLevel = 0;
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static PrototypePrestigeUpgradeDefinition LoadPrestigeDefinition(string assetName) =>
            AssetDatabase.LoadAssetAtPath<PrototypePrestigeUpgradeDefinition>(
                $"{PrototypePrestigeFolder}/{assetName}.asset");

        private static GameObject LoadOrCreatePrestigeRowPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrestigeRowPrefabPath);
            if (existing != null)
                return existing;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateAllPrestigeUpgradeDefinitions();
            var definition = LoadPrestigeDefinition("stronger_big_bang");
            var row = BuildPrestigeRowObject(font, "Prestige Row", definition);
            var prefab = PrefabUtility.SaveAsPrefabAsset(row, PrestigeRowPrefabPath);
            Object.DestroyImmediate(row);
            return prefab;
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
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrestigePanelPrefabPath) != null)
                AssetDatabase.DeleteAsset(PrestigePanelPrefabPath);

            return BuildPrestigePanelPrefab();
        }

        private static GameObject BuildPrestigePanelPrefab()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rowPrefab = LoadOrCreatePrestigeRowPrefab();
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
                LoadPrestigeDefinition("stronger_big_bang"),
                LoadPrestigeDefinition("stable_physics"),
                LoadPrestigeDefinition("longer_star_lifespan"),
                LoadPrestigeDefinition("supernova_memory"),
                LoadPrestigeDefinition("black_hole_memory"),
                LoadPrestigeDefinition("cosmic_efficiency"),
                LoadPrestigeDefinition("particle_evolution"),
                LoadPrestigeDefinition("parallel_echo")
            };

            var rowInstances = new System.Collections.Generic.List<PrototypePrestigeUpgradeRow>();
            foreach (var def in defs)
            {
                if (def == null)
                    continue;

                var rowInstance = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, content.transform);
                rowInstance.name = def.upgradeType.ToString();
                var row = rowInstance.GetComponent<PrototypePrestigeUpgradeRow>();
                var so = new SerializedObject(row);
                so.FindProperty("definition").objectReferenceValue = def;
                so.ApplyModifiedPropertiesWithoutUndo();
                rowInstances.Add(row);
            }

            var panelComp = panelGo.AddComponent<PrototypePrestigePanel>();
            var panelSo = new SerializedObject(panelComp);
            var rowsProp = panelSo.FindProperty("upgradeRows");
            rowsProp.arraySize = rowInstances.Count;
            for (var i = 0; i < rowInstances.Count; i++)
                rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rowInstances[i];
            panelSo.FindProperty("panelRoot").objectReferenceValue = panelGo;
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(panelGo, PrestigePanelPrefabPath);
            Object.DestroyImmediate(panelGo);
            return prefab;
        }

        private static GameObject BuildPrestigeRowObject(Font font, string objectName,
            PrototypePrestigeUpgradeDefinition definition)
        {
            var title = definition != null ? definition.displayName : "Prestige Upgrade";
            var description = definition != null ? definition.description : string.Empty;

            var row = new GameObject(objectName, typeof(RectTransform));
            var layout = row.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(6, 6, 6, 6);
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 104;

            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.12f, 0.14f, 0.95f);

            var titleText = CreateText(row.transform, $"{title} (Lv 0)", font, 14);
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.UpperLeft;
            titleText.gameObject.AddComponent<LayoutElement>().minHeight = 20;

            var descText = CreateText(row.transform, description, font, 11);
            descText.color = new Color(0.7f, 0.78f, 0.85f);
            descText.alignment = TextAnchor.UpperLeft;
            descText.gameObject.AddComponent<LayoutElement>().minHeight = 28;

            var effectText = CreateText(row.transform, "", font, 11);
            effectText.color = new Color(0.55f, 0.95f, 0.75f);
            effectText.alignment = TextAnchor.UpperLeft;
            effectText.gameObject.AddComponent<LayoutElement>().minHeight = 18;

            var btnGo = new GameObject("BuyButton", typeof(RectTransform));
            btnGo.transform.SetParent(row.transform, false);
            btnGo.AddComponent<LayoutElement>().minHeight = 30;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.22f, 0.38f, 0.28f);
            var btn = btnGo.AddComponent<Button>();

            var costText = CreateText(btnGo.transform, "Buy (1 DNA)", font, 12);
            costText.alignment = TextAnchor.MiddleCenter;
            Stretch(costText.rectTransform);

            var rowComp = row.AddComponent<PrototypePrestigeUpgradeRow>();
            var so = new SerializedObject(rowComp);
            so.FindProperty("definition").objectReferenceValue = definition;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("descriptionText").objectReferenceValue = descText;
            so.FindProperty("effectText").objectReferenceValue = effectText;
            so.FindProperty("buyButton").objectReferenceValue = btn;
            so.FindProperty("costText").objectReferenceValue = costText;
            so.FindProperty("buttonImage").objectReferenceValue = btnImg;
            so.ApplyModifiedPropertiesWithoutUndo();

            return row;
        }

        private static void BuildUpgradePanel(GameObject canvasGo, PrototypeGameController controller)
        {
            var old = canvasGo.transform.Find("UpgradePanel");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var panelPrefab = LoadOrCreateUpgradePanelPrefab();
            var panelGo = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvasGo.transform);
            panelGo.name = "UpgradePanel";

            var panel = panelGo.GetComponent<PrototypeUpgradePanel>();
            if (panel != null)
                SetRef(panel, "controller", controller);
        }

        private static void BuildSpeciesUi(GameObject canvasGo, PrototypeGameController controller)
        {
            var old = canvasGo.transform.Find("SpeciesUI");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var entryPrefab = LoadOrCreateSpeciesEntryPrefab();

            var root = new GameObject("SpeciesUI", typeof(RectTransform));
            root.transform.SetParent(canvasGo.transform, false);
            Stretch(root.GetComponent<RectTransform>());
            var portraitPool = root.AddComponent<PrototypeSpeciesPortraitPool>();

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
            var detailPortraitMount = portraitGo.AddComponent<PrototypeSpeciesPortraitMount>();
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

            var panel = root.AddComponent<PrototypeSpeciesPanel>();
            SetRef(panel, "controller", controller);
            SetRef(panel, "panelRoot", panelGo);
            SetRef(panel, "openButton", openButton);
            SetRef(panel, "closeButton", closeButton);
            SetRef(panel, "contentRoot", content.transform);
            SetRef(panel, "speciesEntryPrefab", entryPrefab.GetComponent<PrototypeSpeciesEntryView>());
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
            var portraitMount = portraitGo.AddComponent<PrototypeSpeciesPortraitMount>();
            SetRef(portraitMount, "portraitRoot", portraitGo.transform);
            SetRef(portraitMount, "fallbackImage", portraitImage);

            var speciesName = CreateText(row.transform, "Species", font, 14);
            speciesName.color = new Color(0.65f, 0.95f, 0.75f);
            speciesName.alignment = TextAnchor.MiddleCenter;
            speciesName.fontStyle = FontStyle.Bold;
            speciesName.gameObject.AddComponent<LayoutElement>().minHeight = 26;

            var view = row.AddComponent<PrototypeSpeciesEntryView>();
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

        private static GameObject RebuildUpgradePanelPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(UpgradePanelPrefabPath) != null)
                AssetDatabase.DeleteAsset(UpgradePanelPrefabPath);

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
                LoadUpgradeDefinition("passive_production"),
                LoadUpgradeDefinition("star_stability"),
                LoadUpgradeDefinition("supernova_bonus"),
                LoadUpgradeDefinition("click_collect")
            };

            var rowInstances = new System.Collections.Generic.List<PrototypeUpgradeRow>();

            foreach (var def in defs)
            {
                if (def == null)
                    continue;

                var rowInstance = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, content.transform);
                rowInstance.name = def.upgradeType.ToString();
                var row = rowInstance.GetComponent<PrototypeUpgradeRow>();
                var so = new SerializedObject(row);
                so.FindProperty("definition").objectReferenceValue = def;
                so.ApplyModifiedPropertiesWithoutUndo();
                rowInstances.Add(row);
            }

            var panelComp = panelGo.AddComponent<PrototypeUpgradePanel>();
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

        private static PrototypeUpgradeDefinition LoadUpgradeDefinition(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<PrototypeUpgradeDefinition>(
                $"{PrototypeUpgradeFolder}/{assetName}.asset");
        }

        private static PrototypeUpgradeDefinition LoadOrCreateClickCollectUpgrade()
        {
            var path = $"{PrototypeUpgradeFolder}/click_collect.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PrototypeUpgradeDefinition>(path);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<PrototypeUpgradeDefinition>();
            def.upgradeType = PrototypeUpgradeType.ClickCollectRadius;
            def.displayName = "Click Collect";
            def.description = "Vacuum nearby Stardust anywhere you click (+0.45 radius per level)";
            def.baseCost = 22;
            def.costScale = 1.55;
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static GameObject BuildUpgradeRowObject(Font font, string objectName,
            PrototypeUpgradeDefinition definition)
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

            var titleText = CreateText(row.transform, $"{title} (Lv 0)", font, 14);
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.UpperLeft;
            titleText.gameObject.AddComponent<LayoutElement>().minHeight = 20;

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

            var rowComp = row.AddComponent<PrototypeUpgradeRow>();
            var so = new SerializedObject(rowComp);
            so.FindProperty("definition").objectReferenceValue = definition;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("descriptionText").objectReferenceValue = descText;
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

        private static PrototypeStarView LoadOrCreateStarPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PrototypeStarView>(PrefabPath);
            if (existing != null)
                return existing;

            EnsureFolder("Assets/Prefabs/Prototype");
            var sprite = LoadSunSprite() ?? CreateCircleSprite();
            return BuildStarPrefab(sprite);
        }

        private static PrototypeStarView BuildStarPrefab(Sprite sprite)
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

            var view = go.AddComponent<PrototypeStarView>();
            SetRef(view, "coreRenderer", coreSr);
            SetRef(view, "glowRenderer", glowSr);
            // popScalePeak / popDuration use component defaults; tune on prefab after create

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<PrototypeStarView>();
        }

        private static PrototypeWorldParticle LoadOrCreateStardustParticlePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PrototypeWorldParticle>(StardustParticlePrefabPath);
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

        private static PrototypeWorldParticle LoadOrCreateDnaParticlePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PrototypeWorldParticle>(DnaParticlePrefabPath);
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

        private static PrototypeWorldParticle BuildParticlePrefab(string path, string objectName, Color color,
            float scale, bool tintByStage, bool scaleByValue, float spawnScatter)
        {
            var go = new GameObject(objectName);
            go.transform.localScale = Vector3.one * scale;

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateCircleSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 20;

            var particle = go.AddComponent<PrototypeWorldParticle>();
            var so = new SerializedObject(particle);
            so.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            so.FindProperty("tintByStage").boolValue = tintByStage;
            so.FindProperty("scaleByValue").boolValue = scaleByValue;
            so.FindProperty("spawnScatter").floatValue = spawnScatter;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<PrototypeWorldParticle>();
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
            CreateUpgradeDefinition(PrototypeUpgradeType.MaxPlanetCount, "max_planet_count",
                "Max Planet Count", "+1 max planet orbit slot per level");
            CreateUpgradeDefinition(PrototypeUpgradeType.AutoPlanetFormation, "auto_planet_formation",
                "Auto Planet Formation", "Chance to spawn a free random planet over time");
            CreateUpgradeDefinition(PrototypeUpgradeType.PlanetDnaChance, "planet_dna_chance",
                "Planet DNA Chance", "Planets generate more DNA Potential over time");
            CreateUpgradeDefinition(PrototypeUpgradeType.PlanetClickValue, "planet_click_value",
                "Planet Click Value", "More Stardust when clicking planets");
            CreateUpgradeDefinition(PrototypeUpgradeType.HabitablePlanetChance, "habitable_planet_chance",
                "Habitable Planet Chance", "New planets are more likely to support life");
            CreateUpgradeDefinition(PrototypeUpgradeType.MaxStarCount, "max_star_count",
                "Max Star Count", "Unlocks one additional active star slot per level, up to five stars.");
            CreateUpgradeDefinition(PrototypeUpgradeType.AdvancedStarStability, "advanced_star_stability",
                "Advanced Star Stability", "Further slows star aging after basic stability is developed.");
            CreateExpandUniverseDefinition();
        }

        private static void CreateExpandUniverseDefinition()
        {
            var path = $"{PrototypeUpgradeFolder}/expand_universe.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PrototypeUpgradeDefinition>(path);
            if (existing != null)
                return;

            var def = ScriptableObject.CreateInstance<PrototypeUpgradeDefinition>();
            def.upgradeType = PrototypeUpgradeType.ExpandUniverse;
            def.displayName = "Expand Universe";
            def.description =
                "Unlock Step 2: Multi-Star System Age with star slots, buying new stars, independent star lifecycles, and planets around selected stars.";
            def.baseCost = 500;
            def.costScale = 1f;
            def.maxLevel = 1;
            AssetDatabase.CreateAsset(def, path);
        }

        private static PrototypeUpgradeDefinition CreateUpgradeDefinition(
            PrototypeUpgradeType type, string assetName, string displayName, string description)
        {
            var path = $"{PrototypeUpgradeFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PrototypeUpgradeDefinition>(path);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<PrototypeUpgradeDefinition>();
            def.upgradeType = type;
            def.displayName = displayName;
            def.description = description;
            def.baseCost = 20;
            def.costScale = 1.5;
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static GameObject RebuildSingleStarUpgradePanelPrefab()
        {
            CreateSingleStarUpgradeDefinitions();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(UpgradePanelPrefabPath) != null)
                AssetDatabase.DeleteAsset(UpgradePanelPrefabPath);

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
                LoadUpgradeDefinition("passive_production"),
                LoadUpgradeDefinition("star_stability"),
                LoadUpgradeDefinition("max_planet_count"),
                LoadUpgradeDefinition("auto_planet_formation"),
                LoadUpgradeDefinition("planet_dna_chance"),
                LoadUpgradeDefinition("planet_click_value"),
                LoadUpgradeDefinition("habitable_planet_chance"),
                LoadUpgradeDefinition("max_star_count"),
                LoadUpgradeDefinition("advanced_star_stability"),
                LoadUpgradeDefinition("expand_universe")
            };

            var rowInstances = new System.Collections.Generic.List<PrototypeUpgradeRow>();
            foreach (var def in defs)
            {
                if (def == null)
                    continue;

                var rowInstance = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, content.transform);
                rowInstance.name = def.upgradeType.ToString();
                var row = rowInstance.GetComponent<PrototypeUpgradeRow>();
                var so = new SerializedObject(row);
                so.FindProperty("definition").objectReferenceValue = def;
                so.ApplyModifiedPropertiesWithoutUndo();
                rowInstances.Add(row);
            }

            var panelComp = panelGo.AddComponent<PrototypeUpgradePanel>();
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

        private static PrototypePlanetView LoadOrCreatePlanetPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PrototypePlanetView>(PlanetPrefabPath);
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
            var view = go.AddComponent<PrototypePlanetView>();
            AddPlanetNameTooltip(go);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlanetPrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<PrototypePlanetView>();
        }

        private static void EnsurePlanetNameTooltipOnPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PlanetPrefabPath);
            try
            {
                if (root.GetComponent<PrototypePlanetNameTooltip>() == null)
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

            var tooltip = planetRoot.AddComponent<PrototypePlanetNameTooltip>();
            SetRef(tooltip, "label", label);
        }

        private static PrototypePlanetTypeCatalog LoadOrCreatePlanetTypeCatalog()
        {
            CreateAllPlanetTypeDefinitions();

            var catalog = AssetDatabase.LoadAssetAtPath<PrototypePlanetTypeCatalog>(PlanetTypeCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PrototypePlanetTypeCatalog>();
                AssetDatabase.CreateAsset(catalog, PlanetTypeCatalogPath);
            }

            var so = new SerializedObject(catalog);
            var listProp = so.FindProperty("planetTypes");
            listProp.ClearArray();

            var types = new[]
            {
                PrototypePlanetType.Rocky,
                PrototypePlanetType.Ocean,
                PrototypePlanetType.Lava,
                PrototypePlanetType.Ice,
                PrototypePlanetType.GasGiant,
                PrototypePlanetType.Toxic,
                PrototypePlanetType.Crystal,
                PrototypePlanetType.Desert,
                PrototypePlanetType.Forest
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
            CreatePlanetTypeDefinition(PrototypePlanetType.Rocky, "rocky", "Rocky Planet",
                "Balanced Stardust, medium habitability.");
            CreatePlanetTypeDefinition(PrototypePlanetType.Ocean, "ocean", "Ocean Planet",
                "Lower Stardust, high habitability and life chance.");
            CreatePlanetTypeDefinition(PrototypePlanetType.Lava, "lava", "Lava Planet",
                "High Stardust, low habitability, fragile.");
            CreatePlanetTypeDefinition(PrototypePlanetType.Ice, "ice", "Ice Planet",
                "Low Stardust, durable, medium-low habitability.");
            CreatePlanetTypeDefinition(PrototypePlanetType.GasGiant, "gas_giant", "Gas Giant",
                "High Stardust, cannot civilize.");
            CreatePlanetTypeDefinition(PrototypePlanetType.Toxic, "toxic", "Toxic Planet",
                "Mutation-friendly DNA if life appears.");
            CreatePlanetTypeDefinition(PrototypePlanetType.Crystal, "crystal", "Crystal Planet",
                "Rare, high value, fragile, strong DNA.");
            CreatePlanetTypeDefinition(PrototypePlanetType.Desert, "desert", "Desert Planet",
                "Balanced desert world.");
            CreatePlanetTypeDefinition(PrototypePlanetType.Forest, "forest", "Forest Planet",
                "High habitability and civilization chance.");
        }

        private static PrototypePlanetTypeDefinition CreatePlanetTypeDefinition(
            PrototypePlanetType type, string assetName, string displayName, string description)
        {
            var path = $"{PrototypePlanetTypesFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PrototypePlanetTypeDefinition>(path);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<PrototypePlanetTypeDefinition>();
            def.planetType = type;
            def.displayName = displayName;
            def.description = description;
            var temp = PrototypePlanetTypeUtility.CreateFallbackDefinition(type);
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

        private static PrototypePlanetTypeDefinition LoadPlanetTypeDefinition(PrototypePlanetType type)
        {
            var assetName = type switch
            {
                PrototypePlanetType.GasGiant => "gas_giant",
                _ => type.ToString().ToLowerInvariant()
            };

            return AssetDatabase.LoadAssetAtPath<PrototypePlanetTypeDefinition>(
                $"{PrototypePlanetTypesFolder}/{assetName}.asset");
        }

        private static PrototypeSingleStarBalance LoadOrCreateSingleStarBalance()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PrototypeSingleStarBalance>(SingleStarBalancePath);
            if (existing != null)
                return existing;

            var balance = ScriptableObject.CreateInstance<PrototypeSingleStarBalance>();
            AssetDatabase.CreateAsset(balance, SingleStarBalancePath);
            return balance;
        }

        private static void BuildSingleStarStepUi(GameObject canvasGo, PrototypeGameController controller,
            PrototypeHUD hud)
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
                var panel = upgradeGo.GetComponent<PrototypeUpgradePanel>();
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

            var endPanel = endPanelGo.AddComponent<PrototypeStarSystemEndPanel>();
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

            SetEnum(controller, "gameplayMode", (int)PrototypeGameplayMode.SingleStarSystemAge);
        }
    }
}
#endif
