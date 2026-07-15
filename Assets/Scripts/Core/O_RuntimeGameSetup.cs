using Universes.Planets;
using Universes.Presentation;
using Universes.Stars;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Universes.Core
{
    [DefaultExecutionOrder(-100)]
    public class O_RuntimeGameSetup : MonoBehaviour
    {
        [SerializeField] private bool setupOnAwake = true;

        private void Awake()
        {
            if (!setupOnAwake || FindConfiguredRunController() != null)
                return;

            BuildGame();
        }

        private static O_UniverseRunController FindConfiguredRunController()
        {
            var run = FindAnyObjectByType<O_UniverseRunController>();
            if (run == null || run.Balance == null)
                return null;
            return run;
        }

        public void BuildGame()
        {
            var sprite = O_DefaultGameData.CreateCircleSprite();
            var balance = O_DefaultGameData.CreateBalance();
            var upgradeCatalog = O_DefaultGameData.CreateUpgradeCatalog();
            var variantCatalog = O_DefaultGameData.CreateVariantCatalog();

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
            if (cam.GetComponent<O_WorldCamera>() == null)
                cam.gameObject.AddComponent<O_WorldCamera>();

            var worldRoot = GameObject.Find("WorldRoot")?.transform;
            if (worldRoot == null)
            {
                var wr = new GameObject("WorldRoot");
                worldRoot = wr.transform;
            }

            if (FindAnyObjectByType<O_SaveSystem>() == null)
            {
                var saveGo = new GameObject("SaveSystem");
                saveGo.AddComponent<O_SaveSystem>();
            }

            var bootstrapGo = new GameObject("GameSystems");
            var clock = bootstrapGo.AddComponent<O_GameClock>();

            var runGo = new GameObject("UniverseRun");
            var run = runGo.AddComponent<O_UniverseRunController>();
            var starMgr = runGo.AddComponent<O_StarManager>();
            var planetMgr = runGo.AddComponent<O_PlanetManager>();

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

            var ui = O_RuntimeUIBuilder.Build(run, upgradeCatalog, variantCatalog);
            SetPrivateField(ui.Hud, "floatingText", ui.FloatingText);

            DontDestroyOnLoad(bootstrapGo);
            DontDestroyOnLoad(runGo);
        }

        private static void WireRunController(O_UniverseRunController run, O_GameBalance balance,
            Prestige.O_UpgradeCatalog upgrades, Prestige.O_VariantCatalog variants,
            O_GameClock clock, O_StarManager starMgr, O_PlanetManager planetMgr, Transform worldRoot)
        {
            SetPrivateField(run, "balance", balance);
            SetPrivateField(run, "upgradeCatalog", upgrades);
            SetPrivateField(run, "variantCatalog", variants);
            SetPrivateField(run, "gameClock", clock);
            SetPrivateField(run, "starManager", starMgr);
            SetPrivateField(run, "planetManager", planetMgr);
            SetPrivateField(run, "worldRoot", worldRoot);
        }

        private static O_StarView CreateStarPrefab(Sprite sprite)
        {
            var go = new GameObject("StarPrefab");
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;
            go.AddComponent<O_StarView>();
            DontDestroyOnLoad(go);
            return go.GetComponent<O_StarView>();
        }

        private static O_PlanetView CreatePlanetPrefab(Sprite sprite)
        {
            var go = new GameObject("PlanetPrefab");
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;
            go.AddComponent<O_PlanetView>();
            DontDestroyOnLoad(go);
            return go.GetComponent<O_PlanetView>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
