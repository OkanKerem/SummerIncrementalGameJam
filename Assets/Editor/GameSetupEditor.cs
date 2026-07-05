#if UNITY_EDITOR
using System.IO;
using Universes.Core;
using Universes.Prestige;
using Universes.Presentation;
using Universes.Planets;
using Universes.Stars;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Universes.Editor
{
    public static class GameSetupEditor
    {
        private const string Root = "Assets";
        private const string SoPath = "Assets/ScriptableObjects";
        private const string PrefabPath = "Assets/Prefabs";
        private const string ArtPath = "Assets/Art/Sprites";
        private const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Universes/Setup Game (Full)")]
        public static void SetupFullGame()
        {
            EnsureFolders();
            var circleSprite = CreateCircleSprite();
            var balance = CreateOrLoadBalance();
            var catalog = CreateUpgradeCatalog(balance);
            var variantCatalog = CreateVariantCatalog();
            var starPrefab = CreateStarPrefab(circleSprite);
            var planetPrefab = CreatePlanetPrefab(circleSprite);
            CreateGameScene(balance, catalog, variantCatalog, starPrefab, planetPrefab, circleSprite);
            SetBuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Universes game setup complete! Open Assets/Scenes/Game.unity and press Play.");
        }

        private static void EnsureFolders()
        {
            foreach (var path in new[]
            {
                "Assets/Scripts", SoPath, $"{SoPath}/Upgrades", $"{SoPath}/Variants",
                PrefabPath, $"{PrefabPath}/UI", "Assets/Art", ArtPath, "Assets/Scenes", "Assets/Editor"
            })
            {
                EnsureFolder(path);
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            assetPath = assetPath.Replace('\\', '/');
            var parts = assetPath.Split('/');
            if (parts.Length < 2)
                return;

            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
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

        private static GameBalance CreateOrLoadBalance()
        {
            var path = $"{SoPath}/GameBalance.asset";
            var balance = AssetDatabase.LoadAssetAtPath<GameBalance>(path);
            if (balance != null)
                return balance;

            balance = ScriptableObject.CreateInstance<GameBalance>();
            AssetDatabase.CreateAsset(balance, path);
            return balance;
        }

        private static UpgradeCatalog CreateUpgradeCatalog(GameBalance balance)
        {
            var path = $"{SoPath}/UpgradeCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(path);
            if (catalog != null && catalog.upgrades != null && catalog.upgrades.Length > 0)
                return catalog;

            var upgrades = new[]
            {
                CreateUpgrade("big_bang", "Stronger Big Bang", "+1 starting star per level", UpgradeEffectType.StartingStars, 1, 15),
                CreateUpgrade("cosmic_reserves", "Cosmic Reserves", "+10 starting stardust per level", UpgradeEffectType.StartingStardust, 10, 12),
                CreateUpgrade("entropy_dampening", "Entropy Dampening", "-10% entropy rate per level", UpgradeEffectType.EntropyReduction, 0.1, 20),
                CreateUpgrade("stellar_longevity", "Stellar Longevity", "+15% star lifespan per level", UpgradeEffectType.StarLifespan, 0.15, 18),
                CreateUpgrade("stellar_harvest", "Stellar Harvest", "+20% stardust production per level", UpgradeEffectType.StardustProduction, 0.2, 15),
                CreateUpgrade("click_resonance", "Click Resonance", "+25% click yield per level", UpgradeEffectType.ClickYield, 0.25, 12),
                CreateUpgrade("planet_forge", "Planet Forge", "-10% planet cost per level", UpgradeEffectType.PlanetCostReduction, 0.1, 14),
                CreateUpgrade("spark_of_life", "Spark of Life", "+30% life chance per level", UpgradeEffectType.LifeChance, 0.3, 22),
                CreateUpgrade("genetic_memory", "Genetic Memory", "+15% DNA from collapse per level", UpgradeEffectType.DnaMultiplier, 0.15, 25),
                CreateUpgrade("parallel_echo", "Parallel Echo", "+0.5 passive stardust/sec between runs", UpgradeEffectType.ParallelEcho, 0.5, 30)
            };

            catalog = ScriptableObject.CreateInstance<UpgradeCatalog>();
            catalog.upgrades = upgrades;
            AssetDatabase.CreateAsset(catalog, path);
            return catalog;
        }

        private static UpgradeDefinition CreateUpgrade(string id, string name, string desc, UpgradeEffectType type, double effect, double cost)
        {
            var path = $"{SoPath}/Upgrades/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<UpgradeDefinition>();
            def.id = id;
            def.displayName = name;
            def.description = desc;
            def.effectType = type;
            def.effectPerLevel = effect;
            def.baseCost = cost;
            def.maxLevel = 5;
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static VariantCatalog CreateVariantCatalog()
        {
            var path = $"{SoPath}/VariantCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<VariantCatalog>(path);
            if (catalog != null && catalog.variants != null && catalog.variants.Length > 0)
                return catalog;

            catalog = ScriptableObject.CreateInstance<VariantCatalog>();
            catalog.variants = new[]
            {
                CreateVariant("volatile", "Volatile Universe", "+30% Stardust production", "+25% Entropy growth", 1.3, 1.25, 1, 1, 1),
                CreateVariant("stable", "Stable Universe", "-25% Entropy growth", "-20% Stardust production", 0.8, 0.75, 1, 1, 1),
                CreateVariant("life_rich", "Life-Rich Universe", "+50% life development chance", "-20% star lifespan", 1, 1, 1.5, 0.8, 1)
            };
            AssetDatabase.CreateAsset(catalog, path);
            return catalog;
        }

        private static ParallelVariant CreateVariant(string id, string name, string bonus, string drawback,
            double stardust, double entropy, double life, double lifespan, double dna)
        {
            var path = $"{SoPath}/Variants/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ParallelVariant>(path);
            if (existing != null)
                return existing;

            var v = ScriptableObject.CreateInstance<ParallelVariant>();
            v.id = id;
            v.displayName = name;
            v.bonusDescription = bonus;
            v.drawbackDescription = drawback;
            v.stardustMultiplier = stardust;
            v.entropyMultiplier = entropy;
            v.lifeChanceMultiplier = life;
            v.starLifespanMultiplier = lifespan;
            v.dnaMultiplier = dna;
            AssetDatabase.CreateAsset(v, path);
            return v;
        }

        private static StarView CreateStarPrefab(Sprite sprite)
        {
            var path = $"{PrefabPath}/Star.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<StarView>(path);
            if (existing != null)
                return existing;

            var go = new GameObject("Star");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;
            go.AddComponent<StarView>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<StarView>();
        }

        private static PlanetView CreatePlanetPrefab(Sprite sprite)
        {
            var path = $"{PrefabPath}/Planet.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<PlanetView>(path);
            if (existing != null)
                return existing;

            var go = new GameObject("Planet");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;
            go.AddComponent<PlanetView>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<PlanetView>();
        }

        private static void CreateGameScene(GameBalance balance, UpgradeCatalog catalog, VariantCatalog variants, StarView starPrefab, PlanetView planetPrefab, Sprite sprite)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.1f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<WorldCamera>();

            // World
            var worldRoot = new GameObject("WorldRoot");

            // Game systems
            var bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<GameBootstrap>();
            bootstrap.AddComponent<GameClock>();
            bootstrap.AddComponent<SaveSystem>();

            var runGo = new GameObject("UniverseRun");
            var run = runGo.AddComponent<UniverseRunController>();
            var starMgr = runGo.AddComponent<StarManager>();
            var planetMgr = runGo.AddComponent<PlanetManager>();

            SetSerialized(run, "balance", balance);
            SetSerialized(run, "upgradeCatalog", catalog);
            SetSerialized(run, "variantCatalog", variants);
            SetSerialized(run, "gameClock", bootstrap.GetComponent<GameClock>());
            SetSerialized(run, "starManager", starMgr);
            SetSerialized(run, "planetManager", planetMgr);
            SetSerialized(run, "worldRoot", worldRoot.transform);

            SetSerialized(starMgr, "worldRoot", worldRoot.transform);
            SetSerialized(starMgr, "starViewPrefab", starPrefab);
            SetSerialized(planetMgr, "worldRoot", worldRoot.transform);
            SetSerialized(planetMgr, "planetViewPrefab", planetPrefab);

            SetSerialized(bootstrap.GetComponent<GameBootstrap>(), "runController", run);

            // UI
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            var canvasGo = CreateCanvas();
            var floatingText = canvasGo.AddComponent<FloatingTextSpawner>();
            SetSerialized(floatingText, "canvas", canvasGo.GetComponent<Canvas>());

            var hud = canvasGo.AddComponent<HUDController>();
            var upgradePanel = canvasGo.AddComponent<UpgradePanelController>();
            var collapsePanel = canvasGo.AddComponent<CollapsePanelController>();
            var variantPicker = canvasGo.AddComponent<VariantPickerController>();

            SetSerialized(hud, "runController", run);
            SetSerialized(hud, "floatingText", floatingText);
            SetSerialized(upgradePanel, "runController", run);
            SetSerialized(upgradePanel, "catalog", catalog);
            SetSerialized(collapsePanel, "runController", run);
            SetSerialized(variantPicker, "runController", run);
            SetSerialized(variantPicker, "catalog", variants);

            BuildHudUi(canvasGo.transform, hud, upgradePanel, collapsePanel, variantPicker);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static GameObject CreateCanvas()
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvasGo;
        }

        private static void BuildHudUi(Transform canvas, HUDController hud, UpgradePanelController upgrades, CollapsePanelController collapse, VariantPickerController variants)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Top bar
            var topBar = CreatePanel(canvas, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(-20, 0));
            var topLayout = topBar.AddComponent<HorizontalLayoutGroup>();
            topLayout.padding = new RectOffset(20, 20, 8, 8);
            topLayout.spacing = 30;
            topLayout.childAlignment = TextAnchor.MiddleLeft;

            var stardustText = CreateText(topBar.transform, "StardustText", "Stardust: 0", font, 20);
            var dnaText = CreateText(topBar.transform, "DnaText", "DNA: 0", font, 20);
            var timerText = CreateText(topBar.transform, "TimerText", "Time: 00:00", font, 20);

            var entropyGo = CreatePanel(topBar.transform, "Entropy", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var entropyRect = entropyGo.GetComponent<RectTransform>();
            entropyRect.sizeDelta = new Vector2(300, 24);
            var entropySlider = entropyGo.AddComponent<Slider>();
            var entropyBg = CreateImage(entropyGo.transform, "Background", new Color(0.15f, 0.15f, 0.2f));
            Stretch(entropyBg.rectTransform);
            var fillArea = CreatePanel(entropyGo.transform, "Fill Area", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(fillArea.GetComponent<RectTransform>());
            var fill = CreateImage(fillArea.transform, "Fill", new Color(0.4f, 0.7f, 0.9f));
            Stretch(fill.rectTransform);
            entropySlider.fillRect = fill.rectTransform;
            entropySlider.targetGraphic = fill;
            entropySlider.interactable = false;

            SetSerialized(hud, "stardustText", stardustText);
            SetSerialized(hud, "dnaText", dnaText);
            SetSerialized(hud, "timerText", timerText);
            SetSerialized(hud, "entropySlider", entropySlider);
            SetSerialized(hud, "entropyFill", fill);

            // Left actions panel
            var leftPanel = CreatePanel(canvas, "LeftPanel", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(10, 10), new Vector2(10, -90));
            var leftRect = leftPanel.GetComponent<RectTransform>();
            leftRect.sizeDelta = new Vector2(180, 0);
            var leftLayout = leftPanel.AddComponent<VerticalLayoutGroup>();
            leftLayout.spacing = 8;
            leftLayout.padding = new RectOffset(8, 8, 8, 8);
            leftPanel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 0.85f);

            var bigBangBtn = CreateButton(leftPanel.transform, "Big Bang", font);
            var createStarBtn = CreateButton(leftPanel.transform, "Create Star", font);
            var createStarCost = CreateText(leftPanel.transform, "StarCost", "25", font, 12);
            var createPlanetBtn = CreateButton(leftPanel.transform, "Create Planet", font);
            var createPlanetCost = CreateText(leftPanel.transform, "PlanetCost", "100", font, 12);
            var slowEntropyBtn = CreateButton(leftPanel.transform, "Slow Entropy", font);
            var collapseBtn = CreateButton(leftPanel.transform, "Collapse Universe", font);

            SetSerialized(hud, "bigBangButton", bigBangBtn);
            SetSerialized(hud, "createStarButton", createStarBtn);
            SetSerialized(hud, "createPlanetButton", createPlanetBtn);
            SetSerialized(hud, "slowEntropyButton", slowEntropyBtn);
            SetSerialized(hud, "collapseButton", collapseBtn);
            SetSerialized(hud, "createStarCostText", createStarCost);
            SetSerialized(hud, "createPlanetCostText", createPlanetCost);

            // Right star info
            var starInfo = CreatePanel(canvas, "StarInfo", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(-10, 0));
            var starInfoRect = starInfo.GetComponent<RectTransform>();
            starInfoRect.sizeDelta = new Vector2(200, 140);
            starInfo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 0.85f);
            var starInfoText = CreateText(starInfo.transform, "Info", "Select a star", font, 14);
            Stretch(starInfoText.rectTransform, 10);
            starInfo.SetActive(false);
            SetSerialized(hud, "starInfoPanel", starInfo);
            SetSerialized(hud, "starInfoText", starInfoText);

            // Bottom stats + upgrades
            var bottomPanel = CreatePanel(canvas, "BottomPanel", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(200, 10), new Vector2(-10, 10));
            var bottomRect = bottomPanel.GetComponent<RectTransform>();
            bottomRect.sizeDelta = new Vector2(-420, 200);
            bottomPanel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 0.85f);

            var statsText = CreateText(bottomPanel.transform, "Stats", "", font, 13);
            var statsRect = statsText.rectTransform;
            statsRect.anchorMin = new Vector2(0, 0);
            statsRect.anchorMax = new Vector2(0.35f, 1);
            statsRect.offsetMin = new Vector2(10, 10);
            statsRect.offsetMax = new Vector2(-5, -10);
            statsText.alignment = TextAnchor.UpperLeft;
            SetSerialized(hud, "statsText", statsText);

            var upgradeScroll = CreatePanel(bottomPanel.transform, "UpgradeScroll", new Vector2(0.35f, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var scroll = upgradeScroll.AddComponent<ScrollRect>();
            var viewport = CreatePanel(upgradeScroll.transform, "Viewport", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            var content = CreatePanel(viewport.transform, "Content", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.pivot = new Vector2(0.5f, 1);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.spacing = 4;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;

            SetSerialized(upgrades, "contentRoot", content.transform);

            // Collapse modal
            var collapsePanel = CreatePanel(canvas, "CollapsePanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            collapsePanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            var collapseBox = CreatePanel(collapsePanel.transform, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            collapseBox.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 400);
            collapseBox.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.2f);
            var collapseText = CreateText(collapseBox.transform, "Breakdown", "", font, 14);
            Stretch(collapseText.rectTransform, 15);
            collapseText.alignment = TextAnchor.UpperLeft;
            var continueBtn = CreateButton(collapseBox.transform, "Continue", font);
            var continueRect = continueBtn.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(0.5f, 0);
            continueRect.anchorMax = new Vector2(0.5f, 0);
            continueRect.anchoredPosition = new Vector2(0, 30);
            continueRect.sizeDelta = new Vector2(160, 36);
            collapsePanel.SetActive(false);

            SetSerialized(collapse, "panel", collapsePanel);
            SetSerialized(collapse, "breakdownText", collapseText);
            SetSerialized(collapse, "continueButton", continueBtn);

            // Variant picker modal
            var variantPanel = CreatePanel(canvas, "VariantPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            variantPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            var variantTitle = CreateText(variantPanel.transform, "Title", "Choose Your Next Parallel Universe", font, 22);
            var titleRect = variantTitle.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -50);
            titleRect.sizeDelta = new Vector2(600, 40);
            variantTitle.alignment = TextAnchor.MiddleCenter;

            var cardsRoot = CreatePanel(variantPanel.transform, "Cards", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cardsRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 200);
            var hlg = cardsRoot.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            variantPanel.SetActive(false);

            SetSerialized(variants, "panel", variantPanel);
            SetSerialized(variants, "cardsRoot", cardsRoot.transform);
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string content, Font font, int size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            go.AddComponent<LayoutElement>().minHeight = size + 8;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, Font font)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.35f, 0.55f);
            var btn = go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 32);
            go.AddComponent<LayoutElement>().minHeight = 36;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.font = font;
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            Stretch(text.rectTransform);
            return btn;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static void Stretch(RectTransform rect, float padding = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static void SetSerialized(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetBuildScene()
        {
            var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes;
        }
    }
}
#endif
