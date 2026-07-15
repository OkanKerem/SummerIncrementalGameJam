using Universes.Core;
using Universes.Stars;
using UnityEngine;
using UnityEngine.UI;

namespace Universes.Presentation
{
    public class O_HUDController : MonoBehaviour
    {
        [SerializeField] private O_UniverseRunController runController;
        [SerializeField] private O_FloatingTextSpawner floatingText;

        [Header("Top HUD")]
        [SerializeField] private Text stardustText;
        [SerializeField] private Text dnaText;
        [SerializeField] private Text timerText;
        [SerializeField] private Slider entropySlider;
        [SerializeField] private Image entropyFill;

        [Header("Actions")]
        [SerializeField] private Button bigBangButton;
        [SerializeField] private Button createStarButton;
        [SerializeField] private Button createPlanetButton;
        [SerializeField] private Button slowEntropyButton;
        [SerializeField] private Button collapseButton;
        [SerializeField] private Text createStarCostText;
        [SerializeField] private Text createPlanetCostText;

        [Header("Star Info")]
        [SerializeField] private GameObject starInfoPanel;
        [SerializeField] private Text starInfoText;

        [Header("Stats")]
        [SerializeField] private Text statsText;

        private O_StarManager _starManager;

        private void Start()
        {
            if (runController == null)
                runController = FindAnyObjectByType<O_UniverseRunController>();

            _starManager = runController.GetComponent<O_StarManager>();

            bigBangButton?.onClick.AddListener(() => runController.BigBang());
            createStarButton?.onClick.AddListener(() => runController.TryCreateStar());
            createPlanetButton?.onClick.AddListener(() => runController.TryCreatePlanet());
            slowEntropyButton?.onClick.AddListener(() => runController.TrySlowEntropy());
            collapseButton?.onClick.AddListener(() => runController.Collapse(manualCollapse: true));

            runController.Wallet.OnBalanceChanged += _ => RefreshHud();
            runController.Wallet.OnTotalProducedChanged += _ => RefreshHud();
            runController.Entropy.OnEntropyChanged += _ => RefreshHud();
            runController.Prestige.OnChanged += RefreshHud;
            runController.OnRunStarted += RefreshHud;
            runController.OnPhaseChanged += RefreshHud;
            runController.OnRunCollapsed += _ => RefreshHud();

            if (_starManager != null)
            {
                _starManager.OnStarSelected += _ => RefreshStarInfo();
                _starManager.OnStarClickedReward += HandleStarClicked;
                _starManager.OnStarSupernova += HandleSupernova;
            }

            RefreshHud();
        }

        private void Update()
        {
            RefreshHud();
        }

        private void HandleStarClicked(O_Star star, double reward)
        {
            if (floatingText == null || _starManager == null)
                return;

            var view = _starManager.GetView(star);
            if (view != null)
                floatingText.Spawn(view.transform.position, $"+{O_NumberFormatHelper.Format(reward)}", new Color(1f, 0.9f, 0.4f));
        }

        private void HandleSupernova(O_Star star)
        {
            if (floatingText == null || _starManager == null)
                return;

            var view = _starManager.GetView(star);
            if (view != null)
                floatingText.Spawn(view.transform.position, "SUPERNOVA!", new Color(1f, 1f, 1f));
        }

        private void RefreshHud()
        {
            if (runController == null)
                return;

            if (stardustText != null)
                stardustText.text = $"Stardust: {O_NumberFormatHelper.Format(runController.Wallet.Balance)}";

            if (dnaText != null)
                dnaText.text = $"DNA: {O_NumberFormatHelper.Format(runController.Prestige.UniverseDna)}";

            if (timerText != null)
            {
                var mins = Mathf.FloorToInt(runController.RunTime / 60f);
                var secs = Mathf.FloorToInt(runController.RunTime % 60f);
                timerText.text = $"Time: {mins:00}:{secs:00}";
            }

            if (entropySlider != null)
            {
                entropySlider.value = runController.Entropy.Entropy / 100f;
                if (entropyFill != null)
                {
                    var e = runController.Entropy.Entropy;
                    entropyFill.color = e > 75f
                        ? new Color(0.9f, 0.3f, 0.2f)
                        : e > 50f
                            ? new Color(0.9f, 0.7f, 0.2f)
                            : new Color(0.4f, 0.7f, 0.9f);
                }
            }

            var active = runController.Phase == O_RunPhase.Active;
            if (bigBangButton != null)
                bigBangButton.interactable = runController.CanBigBang();
            if (createStarButton != null)
                createStarButton.interactable = active;
            if (createPlanetButton != null)
                createPlanetButton.interactable = active && _starManager != null && _starManager.SelectedStar != null;
            if (slowEntropyButton != null)
                slowEntropyButton.interactable = active;
            if (collapseButton != null)
                collapseButton.interactable = active;

            if (createStarCostText != null)
                createStarCostText.text = O_NumberFormatHelper.Format(runController.GetCreateStarCost());
            if (createPlanetCostText != null)
                createPlanetCostText.text = O_NumberFormatHelper.Format(runController.GetCreatePlanetCost());

            RefreshStarInfo();
            RefreshStats();
        }

        private void RefreshStarInfo()
        {
            if (starInfoPanel == null || starInfoText == null || _starManager == null)
                return;

            var star = _starManager.SelectedStar;
            if (star == null)
            {
                starInfoPanel.SetActive(false);
                return;
            }

            starInfoPanel.SetActive(true);
            starInfoText.text =
                $"Star #{star.Id}\n" +
                $"Stage: {star.Stage}\n" +
                $"Age: {star.Age:0}%\n" +
                $"Planets: {star.PlanetIds.Count}";
        }

        private void RefreshStats()
        {
            if (statsText == null)
                return;

            var s = runController.CurrentStats;
            var p = runController.Prestige;
            statsText.text =
                $"--- Run ---\n" +
                $"Produced: {O_NumberFormatHelper.Format(s.TotalStardustProduced)}\n" +
                $"Stars: {s.StarsCreated} | Planets: {s.PlanetsCreated}\n" +
                $"Life: {s.LifePlanets} | Supernovas: {s.SupernovaCount}\n" +
                $"--- Lifetime ---\n" +
                $"Collapses: {p.TotalCollapses}\n" +
                $"Total DNA: {O_NumberFormatHelper.Format(p.TotalDnaEarned)}";
        }
    }
}
