using Universes.Planets;
using Universes.Presentation;
using Universes.Stars;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Universes.Core
{
    [DefaultExecutionOrder(-100)]
    public class RuntimeGameSetup : MonoBehaviour
    {
        [SerializeField] private bool setupOnAwake = true;

        private void Awake()
        {
            if (!setupOnAwake || FindConfiguredRunController() != null)
                return;

            BuildGame();
        }

        private static UniverseRunController FindConfiguredRunController()
        {
            var run = FindAnyObjectByType<UniverseRunController>();
            if (run == null || run.Balance == null)
                return null;
            return run;
        }

        public void BuildGame()
        {
            var sprite = DefaultGameData.CreateCircleSprite();
            var balance = DefaultGameData.CreateBalance();
            var upgradeCatalog = DefaultGameData.CreateUpgradeCatalog();
            var variantCatalog = DefaultGameData.CreateVariantCatalog();

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.1f);
            if (cam.GetComponent<WorldCamera>() == null)
                cam.gameObject.AddComponent<WorldCamera>();

            var worldRoot = GameObject.Find("WorldRoot")?.transform;
            if (worldRoot == null)
            {
                var wr = new GameObject("WorldRoot");
                worldRoot = wr.transform;
            }

            if (FindAnyObjectByType<SaveSystem>() == null)
            {
                var saveGo = new GameObject("SaveSystem");
                saveGo.AddComponent<SaveSystem>();
            }

            var bootstrapGo = new GameObject("GameSystems");
            var clock = bootstrapGo.AddComponent<GameClock>();

            var runGo = new GameObject("UniverseRun");
            var run = runGo.AddComponent<UniverseRunController>();
            var starMgr = runGo.AddComponent<StarManager>();
            var planetMgr = runGo.AddComponent<PlanetManager>();

            WireRunController(run, balance, upgradeCatalog, variantCatalog, clock, starMgr, planetMgr, worldRoot);

            var starPrefab = CreateStarPrefab(sprite);
            var planetPrefab = CreatePlanetPrefab(sprite);
            starMgr.Initialize(balance, null, null, worldRoot);
            SetPrivateField(starMgr, "starViewPrefab", starPrefab);
            SetPrivateField(starMgr, "worldRoot", worldRoot);
            SetPrivateField(planetMgr, "planetViewPrefab", planetPrefab);
            SetPrivateField(planetMgr, "worldRoot", worldRoot);

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var ui = RuntimeUIBuilder.Build(run, upgradeCatalog, variantCatalog);
            SetPrivateField(ui.Hud, "floatingText", ui.FloatingText);

            DontDestroyOnLoad(bootstrapGo);
            DontDestroyOnLoad(runGo);
        }

        private static void WireRunController(UniverseRunController run, GameBalance balance,
            Prestige.UpgradeCatalog upgrades, Prestige.VariantCatalog variants,
            GameClock clock, StarManager starMgr, PlanetManager planetMgr, Transform worldRoot)
        {
            SetPrivateField(run, "balance", balance);
            SetPrivateField(run, "upgradeCatalog", upgrades);
            SetPrivateField(run, "variantCatalog", variants);
            SetPrivateField(run, "gameClock", clock);
            SetPrivateField(run, "starManager", starMgr);
            SetPrivateField(run, "planetManager", planetMgr);
            SetPrivateField(run, "worldRoot", worldRoot);
        }

        private static StarView CreateStarPrefab(Sprite sprite)
        {
            var go = new GameObject("StarPrefab");
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;
            go.AddComponent<StarView>();
            DontDestroyOnLoad(go);
            return go.GetComponent<StarView>();
        }

        private static PlanetView CreatePlanetPrefab(Sprite sprite)
        {
            var go = new GameObject("PlanetPrefab");
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;
            go.AddComponent<PlanetView>();
            DontDestroyOnLoad(go);
            return go.GetComponent<PlanetView>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
