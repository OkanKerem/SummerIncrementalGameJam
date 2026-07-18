using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Universes.Game
{
    public class StarSystemEndPanel : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button newSystemButton;
        [SerializeField] private Button prestigeButton;
        [SerializeField] private string prestigeSceneName = "PrestigeScene";
        private const string ReturnSceneKey = "PrototypePrestigeReturnScene";

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<GameController>();

            if (panelRoot == null)
                panelRoot = gameObject;

            newSystemButton?.onClick.AddListener(() =>
            {
                controller?.StartNewStarSystem();
                Hide();
            });

            prestigeButton?.onClick.AddListener(LoadPrestigeScene);

            if (controller != null)
                controller.OnStarSystemEnded += Show;
        }

        private void OnDestroy()
        {
            if (controller != null)
                controller.OnStarSystemEnded -= Show;
        }

        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (titleText != null)
                titleText.text = "Star System Ended";

            if (summaryText != null && controller != null)
                summaryText.text = BuildSummary(controller);
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void LoadPrestigeScene()
        {
            if (string.IsNullOrWhiteSpace(prestigeSceneName))
                return;

            PlayerPrefs.SetString(ReturnSceneKey, SceneManager.GetActiveScene().name);
            PlayerPrefs.Save();
            SceneManager.LoadScene(prestigeSceneName);
        }

        private static string BuildSummary(GameController controller)
        {
            var stats = controller.RunStats;
            var breakdown = controller.LastStarSystemBreakdown;
            var minutes = Mathf.FloorToInt(stats.SurvivalTimeSeconds / 60f);
            var seconds = Mathf.FloorToInt(stats.SurvivalTimeSeconds % 60f);

            return
                "Star System Summary\n" +
                $"Total Stardust Produced: {stats.TotalStardustProduced:0}\n" +
                $"Planets Created: {stats.PlanetsCreated}\n" +
                $"Planets Destroyed: {stats.PlanetsDestroyed}\n" +
                $"Highest Planet Count: {stats.HighestPlanetCount}\n" +
                $"Life-Bearing Planets: {stats.LifePlanetsReached}\n" +
                $"Highest Civilization: {stats.HighestCivilizationLabel}\n" +
                $"DNA Potential Generated: {controller.DnaPotential:0}\n" +
                $"Star Lifetime: {minutes:00}:{seconds:00}\n\n" +
                "Universe DNA Earned\n" +
                $"DNA Potential: +{breakdown.FromDnaPotential}\n" +
                $"Civilization bonus: +{breakdown.FromCivilizationBonus}\n" +
                $"Planet bonus: +{breakdown.FromPlanetBonus}\n" +
                $"Universe DNA gained: +{breakdown.TotalGained}\n" +
                $"Total Universe DNA: {controller.Prestige.UniverseDna:0}\n\n" +
                "Start a new star system when ready.";
        }
    }
}
