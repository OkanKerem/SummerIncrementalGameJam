using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Universes.Game
{
    public class CollapsePanel : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private PrestigePanel prestigePanel;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button prestigeButton;
        [SerializeField] private Button startNewUniverseButton;
        [SerializeField] private string prestigeSceneName = "PrestigeScene";
        private const string ReturnSceneKey = "PrototypePrestigeReturnScene";

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<GameController>();

            if (prestigePanel == null)
                prestigePanel = FindAnyObjectByType<PrestigePanel>(FindObjectsInactive.Include);

            if (panelRoot == null)
                panelRoot = gameObject;

            prestigeButton?.onClick.AddListener(LoadPrestigeScene);
            startNewUniverseButton?.onClick.AddListener(() =>
            {
                controller?.ReloadActiveSceneForNewUniverse();
            });

            if (controller != null)
                controller.OnUniverseCollapsed += Show;
        }

        private void OnDestroy()
        {
            if (controller != null)
                controller.OnUniverseCollapsed -= Show;
        }

        public void Show(bool manualCollapse)
        {
            if (controller != null && controller.IsGameWon)
                return;

            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (titleText != null)
            {
                titleText.text = manualCollapse
                    ? "Universe Collapsed — Legacy Preserved"
                    : "Universe Collapsed — Legacy Preserved";
            }

            if (titleText != null && controller != null &&
                !string.IsNullOrWhiteSpace(controller.EndingTitle))
            {
                titleText.text = controller.EndingTitle;
            }
            else if (titleText != null && manualCollapse)
            {
                titleText.text = "Controlled Collapse";
            }

            if (summaryText != null && controller != null)
            {
                var breakdown = controller.LastCollapseBreakdown ?? new CollapseBreakdown();
                summaryText.text = BuildSummary(controller, breakdown);
            }

        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private static string BuildSummary(GameController controller, CollapseBreakdown breakdown)
        {
            var stats = controller.RunStats;
            var minutes = Mathf.FloorToInt(stats.SurvivalTimeSeconds / 60f);
            var seconds = Mathf.FloorToInt(stats.SurvivalTimeSeconds % 60f);
            var endingText = !string.IsNullOrWhiteSpace(controller.EndingText)
                ? controller.EndingText + "\n\n"
                : string.Empty;

            return
                endingText +
                "Run Summary\n" +
                $"Total Stardust Produced: {stats.TotalStardustProduced:0}\n" +
                $"Stars Created: {stats.StarsCreated}\n" +
                $"Supernovas: {stats.SupernovaCount}\n" +
                $"Star Collisions: {stats.StarCollisionCount}\n" +
                $"Black Holes Created: {stats.BlackHolesCreated}\n" +
                $"DNA Fragments Collected: {stats.DnaFragmentsCollected}\n" +
                $"DNA Potential at collapse: {controller.DnaPotential:0}\n" +
                $"Universe Lifetime: {minutes:00}:{seconds:00}\n" +
                $"Final Entropy: {stats.FinalEntropy:0}%\n\n" +
                "Universe DNA Earned\n" +
                $"Base collapse reward: +{breakdown.BaseReward}\n" +
                $"DNA Fragments: +{breakdown.FromDnaFragments}\n" +
                $"DNA Potential: +{breakdown.FromDnaPotential}\n" +
                $"Supernovas ({PrestigeBalance.SupernovasPerDnaPoint} each): +{breakdown.FromSupernovas}\n" +
                $"Collisions ({PrestigeBalance.CollisionsPerDnaPoint} each): +{breakdown.FromCollisions}\n" +
                $"Production ({PrestigeBalance.StardustPerDnaPoint:0} Stardust each): +{breakdown.FromProduction}\n" +
                $"Lifetime bonus: +{breakdown.FromLifetime}\n" +
                $"Universe DNA gained this run: +{breakdown.TotalGained}\n" +
                $"Total Universe DNA owned: {controller.Prestige.UniverseDna:0}\n\n" +
                "Spend Universe DNA on permanent upgrades, then start a stronger universe.";
        }

        private void LoadPrestigeScene()
        {
            if (!string.IsNullOrWhiteSpace(prestigeSceneName))
            {
                PlayerPrefs.SetString(ReturnSceneKey, SceneManager.GetActiveScene().name);
                PlayerPrefs.Save();
                SceneManager.LoadScene(prestigeSceneName);
            }
        }
    }
}
