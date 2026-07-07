using UnityEngine;
using UnityEngine.UI;

namespace Universes.Prototype
{
    public class PrototypeStarSystemEndPanel : MonoBehaviour
    {
        [SerializeField] private PrototypeGameController controller;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button newSystemButton;

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<PrototypeGameController>();

            if (panelRoot == null)
                panelRoot = gameObject;

            newSystemButton?.onClick.AddListener(() =>
            {
                controller?.StartNewStarSystem();
                Hide();
            });

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

        private static string BuildSummary(PrototypeGameController controller)
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
