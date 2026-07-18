using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Universes.Game
{
    public class VictoryPanel : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Transform alienImageRoot;
        [SerializeField] private GameObject alienImagePrefab;
        [SerializeField] private Button prestigeButton;
        [SerializeField] private Button startNewUniverseButton;
        [SerializeField] private string prestigeSceneName = "PrestigeScene";
        private const string ReturnSceneKey = "PrototypePrestigeReturnScene";

        private GameObject _alienInstance;

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<GameController>();

            if (panelRoot == null)
                panelRoot = gameObject;

            if (alienImageRoot == null)
                alienImageRoot = transform.Find("Card/AlienImage");

            prestigeButton?.onClick.AddListener(LoadPrestigeScene);
            startNewUniverseButton?.onClick.AddListener(() =>
            {
                controller?.ReloadActiveSceneForNewUniverse();
            });

            if (controller != null)
                controller.OnUniverseCollapsed += OnUniverseCollapsed;
        }

        private void OnDestroy()
        {
            if (controller != null)
                controller.OnUniverseCollapsed -= OnUniverseCollapsed;

            ClearAlienImage();
        }

        private void OnUniverseCollapsed(bool _)
        {
            if (controller != null && controller.IsGameWon)
                Show();
        }

        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (controller == null)
                return;

            if (titleText != null)
            {
                titleText.text = !string.IsNullOrWhiteSpace(controller.EndingTitle)
                    ? controller.EndingTitle
                    : "Victory";
            }

            if (summaryText != null)
            {
                var breakdown = controller.LastCollapseBreakdown ?? new CollapseBreakdown();
                summaryText.text = BuildSummary(controller, breakdown);
            }

            RefreshAlienImage();
        }

        public void Hide()
        {
            ClearAlienImage();

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void RefreshAlienImage()
        {
            ClearAlienImage();

            if (alienImageRoot == null)
                return;

            var planet = ResolveFeaturedPlanet();
            if (planet == null || !planet.HasSpecies)
            {
                alienImageRoot.gameObject.SetActive(false);
                return;
            }

            alienImageRoot.gameObject.SetActive(true);

            if (alienImagePrefab != null)
            {
                _alienInstance = Instantiate(alienImagePrefab, alienImageRoot);
                FitAlienInstance(_alienInstance);
                SpeciesPortraitPool.ConfigurePortrait(_alienInstance, planet);
                return;
            }

            var existing = FindExistingAlienRoot(alienImageRoot);
            if (existing != null)
            {
                existing.SetActive(true);
                SpeciesPortraitPool.ConfigurePortrait(existing, planet);
            }
        }

        private void ClearAlienImage()
        {
            if (_alienInstance != null)
            {
                Destroy(_alienInstance);
                _alienInstance = null;
            }

            if (alienImageRoot == null)
                return;

            for (var i = alienImageRoot.childCount - 1; i >= 0; i--)
            {
                var child = alienImageRoot.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        private Planet ResolveFeaturedPlanet()
        {
            if (controller == null)
                return null;

            if (controller.WinningPlanet != null && controller.WinningPlanet.HasSpecies)
                return controller.WinningPlanet;

            var manager = controller.PlanetManager;
            if (manager == null)
                return null;

            Planet best = null;
            foreach (var planet in manager.Planets)
            {
                if (planet == null || !planet.IsAlive || !planet.HasSpecies)
                    continue;

                if (best == null ||
                    planet.CivilizationStage > best.CivilizationStage ||
                    (planet.CivilizationStage == best.CivilizationStage &&
                     planet.CivilizationProgress > best.CivilizationProgress))
                {
                    best = planet;
                }
            }

            return best;
        }

        private static void FitAlienInstance(GameObject instance)
        {
            if (instance == null)
                return;

            if (instance.TryGetComponent<RectTransform>(out var rect))
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                return;
            }

            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        private static GameObject FindExistingAlienRoot(Transform root)
        {
            if (root == null)
                return null;

            var named = root.Find("Alien");
            if (named != null)
                return named.gameObject;

            foreach (Transform child in root)
            {
                if (child.Find("Faces") != null && child.Find("Eyes") != null)
                    return child.gameObject;
            }

            return null;
        }

        private static string BuildSummary(GameController controller, CollapseBreakdown breakdown)
        {
            var stats = controller.RunStats;
            var minutes = Mathf.FloorToInt(stats.SurvivalTimeSeconds / 60f);
            var seconds = Mathf.FloorToInt(stats.SurvivalTimeSeconds % 60f);

            var reason = !string.IsNullOrWhiteSpace(controller.EndingText)
                ? controller.EndingText
                : "The universe was won.";

            var speciesSummary = BuildMostCivilizedSpeciesSummary(controller);

            return
                "Victory Reason\n" +
                reason + "\n\n" +
                "Most Civilized Species\n" +
                speciesSummary + "\n\n" +
                "Run Summary\n" +
                $"Total Stardust Produced: {stats.TotalStardustProduced:0}\n" +
                $"Stars Created: {stats.StarsCreated}\n" +
                $"Planets Created: {stats.PlanetsCreated}\n" +
                $"Life-Bearing Planets: {stats.LifePlanetsReached}\n" +
                $"Highest Civilization: {stats.HighestCivilizationLabel}\n" +
                $"Supernovas: {stats.SupernovaCount}\n" +
                $"Star Collisions: {stats.StarCollisionCount}\n" +
                $"Black Holes Created: {stats.BlackHolesCreated}\n" +
                $"DNA Fragments Collected: {stats.DnaFragmentsCollected}\n" +
                $"DNA Potential: {controller.DnaPotential:0}\n" +
                $"Universe Lifetime: {minutes:00}:{seconds:00}\n" +
                $"Final Entropy: {stats.FinalEntropy:0}%\n\n" +
                "Universe DNA Earned\n" +
                $"Base reward: +{breakdown.BaseReward}\n" +
                $"DNA Fragments: +{breakdown.FromDnaFragments}\n" +
                $"DNA Potential: +{breakdown.FromDnaPotential}\n" +
                $"Supernovas: +{breakdown.FromSupernovas}\n" +
                $"Collisions: +{breakdown.FromCollisions}\n" +
                $"Production: +{breakdown.FromProduction}\n" +
                $"Lifetime bonus: +{breakdown.FromLifetime}\n" +
                $"Universe DNA gained: +{breakdown.TotalGained}\n" +
                $"Total Universe DNA: {controller.Prestige.UniverseDna:0}\n\n" +
                "Spend Universe DNA on prestige upgrades, then start a stronger universe.";
        }

        private static string BuildMostCivilizedSpeciesSummary(GameController controller)
        {
            var winning = controller.WinningPlanet;
            if (winning != null && winning.HasSpecies)
            {
                var home = controller.PlanetManager != null
                    ? controller.PlanetManager.GetPlanetDisplayName(winning)
                    : "Unknown world";
                return
                    $"Species: {winning.SpeciesName}\n" +
                    $"Civilization: {winning.CivilizationName}\n" +
                    $"Home Planet: {home}";
            }

            if (!string.IsNullOrWhiteSpace(controller.RunStats.HighestCivilizationSpeciesName))
            {
                return
                    $"Species: {controller.RunStats.HighestCivilizationSpeciesName}\n" +
                    $"Civilization: {controller.RunStats.HighestCivilizationLabel}";
            }

            if (controller.RunStats.HighestCivilizationLabel != "No Life")
                return $"Civilization: {controller.RunStats.HighestCivilizationLabel}";

            return "No living species reached civilization.";
        }

        private void LoadPrestigeScene()
        {
            if (string.IsNullOrWhiteSpace(prestigeSceneName))
                return;

            PlayerPrefs.SetString(ReturnSceneKey, SceneManager.GetActiveScene().name);
            PlayerPrefs.Save();
            SceneManager.LoadScene(prestigeSceneName);
        }
    }
}
