using UnityEngine;
using UnityEngine.UI;

namespace Universes.Prototype
{
    public class PrototypeHUD : MonoBehaviour
    {
        [SerializeField] private PrototypeGameController controller;
        [SerializeField] private Text stardustText;
        [SerializeField] private Text dnaText;
        [SerializeField] private Text universeDnaText;
        [SerializeField] private Text statsText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Text entropyText;
        [SerializeField] private Text universeStatusText;
        [SerializeField] private Text blackHoleStatusText;
        [SerializeField] private Slider entropySlider;
        [SerializeField] private Image entropyFillImage;
        [SerializeField] private GameObject entropyPanelRoot;
        [SerializeField] private RectTransform stardustCollector;
        [SerializeField] private RectTransform dnaCollector;
        [SerializeField] private Button createStarButton;
        [SerializeField] private Text createStarButtonText;
        [SerializeField] private Button collapseUniverseButton;
        [SerializeField] private PrototypeCollapsePanel collapsePanel;
        [SerializeField] private PrototypeStarSystemEndPanel starSystemEndPanel;

        private float _feedbackTimer;
        private Canvas _canvas;

        private void Start()
        {
            if (controller == null)
                controller = FindAnyObjectByType<PrototypeGameController>();

            if (collapsePanel == null)
                collapsePanel = FindAnyObjectByType<PrototypeCollapsePanel>(FindObjectsInactive.Include);

            if (starSystemEndPanel == null)
                starSystemEndPanel = FindAnyObjectByType<PrototypeStarSystemEndPanel>(FindObjectsInactive.Include);

            _canvas = GetComponent<Canvas>();

            if (stardustCollector == null && stardustText != null)
                stardustCollector = stardustText.rectTransform;

            if (dnaCollector == null && dnaText != null)
                dnaCollector = dnaText.rectTransform;

            var particleManager = controller != null
                ? controller.GetComponent<PrototypeCosmicParticleManager>()
                : FindAnyObjectByType<PrototypeCosmicParticleManager>();

            if (particleManager != null)
                ConfigureParticleManager(particleManager);

            var planetManager = controller.PlanetManager;
            if (planetManager != null)
            {
                planetManager.OnCivilizationAdvanced += OnCivilizationAdvanced;
                planetManager.OnCivilizationEvent += ShowMessage;
            }

            createStarButton?.onClick.AddListener(OnPrimaryActionClicked);
            collapseUniverseButton?.onClick.AddListener(() => controller.TryCollapseUniverse());

            controller.OnStateChanged += Refresh;
            controller.OnStardustGained += _ => Refresh();
            controller.OnDnaGained += Refresh;
            controller.OnEntropyChanged += _ => RefreshEntropy();
            controller.OnSupernova += () => ShowMessage($"SUPERNOVA!");
            controller.OnStarCollision += () => ShowMessage("STAR COLLISION!");
            controller.OnBlackHoleSpawned += () => ShowMessage("BLACK HOLE formed!");
            controller.OnUniverseCollapsed += OnUniverseCollapsed;
            controller.OnStarSystemEnded += OnStarSystemEnded;
            controller.OnExpansionFeedback += ShowMessage;
            controller.Prestige.OnChanged += Refresh;

            ApplyModeVisibility();
            Refresh();
        }

        private void OnCivilizationAdvanced(PrototypePlanet planet, PrototypeCivilizationStage stage)
        {
            if (planet == null)
                return;

            var planetName = controller.PlanetManager != null
                ? controller.PlanetManager.GetPlanetDisplayName(planet)
                : PrototypePlanetTypeUtility.GetLabel(planet.Definition);
            var stageName = PrototypeCivilizationUtility.GetLabel(stage, controller.SingleStarBalance.civilization);
            var speciesName = planet.HasSpecies ? $" ({planet.SpeciesName})" : string.Empty;
            ShowMessage($"{planetName}{speciesName}: {stageName}!");
        }

        private void OnPrimaryActionClicked()
        {
            if (controller == null)
                return;

            if (controller.IsSingleStarMode)
                controller.TryCreatePlanet();
            else
                controller.TryCreateNewStar();
        }

        private void OnUniverseCollapsed(bool manualCollapse)
        {
            if (controller.IsSingleStarMode)
                return;

            Refresh();
            if (collapsePanel == null)
                collapsePanel = FindAnyObjectByType<PrototypeCollapsePanel>(FindObjectsInactive.Include);

            collapsePanel?.Show(manualCollapse);
        }

        private void OnStarSystemEnded()
        {
            Refresh();
            if (starSystemEndPanel == null)
                starSystemEndPanel = FindAnyObjectByType<PrototypeStarSystemEndPanel>(FindObjectsInactive.Include);

            starSystemEndPanel?.Show();
        }

        public void ConfigureParticleManager(PrototypeCosmicParticleManager particleManager)
        {
            if (particleManager == null)
                return;

            if (_canvas == null)
                _canvas = GetComponent<Canvas>();

            if (stardustCollector == null && stardustText != null)
                stardustCollector = stardustText.rectTransform;

            if (dnaCollector == null && dnaText != null)
                dnaCollector = dnaText.rectTransform;

            particleManager.Configure(stardustCollector, dnaCollector, _canvas, Camera.main);
        }

        private void ApplyModeVisibility()
        {
            if (controller == null)
                return;

            var single = controller.IsSingleStarMode;
            if (entropyPanelRoot != null)
                entropyPanelRoot.SetActive(!single);
            else if (entropyText != null)
                entropyText.transform.parent?.gameObject.SetActive(!single);

            if (collapseUniverseButton != null)
                collapseUniverseButton.gameObject.SetActive(!single);

            if (blackHoleStatusText != null && single)
                blackHoleStatusText.text = string.Empty;
        }

        private void Update()
        {
            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f && feedbackText != null)
                    feedbackText.text = "";
            }

            RefreshStatsLive();
            if (!controller.IsSingleStarMode)
                RefreshBlackHoleStatus();
        }

        private void Refresh()
        {
            if (controller == null)
                return;

            if (stardustText != null)
                stardustText.text = $"Stardust: {controller.Stardust:0}";

            if (dnaText != null)
            {
                dnaText.text = controller.IsSingleStarMode
                    ? $"DNA Potential: {controller.DnaPotential:0}"
                    : $"DNA Fragments: {controller.DnaFragments}";
            }

            if (universeDnaText != null)
                universeDnaText.text = $"Universe DNA: {controller.Prestige.UniverseDna:0}";

            if (!controller.IsSingleStarMode)
                RefreshEntropy();

            RefreshStatsLive();
            RefreshButtons();
        }

        private void RefreshEntropy()
        {
            if (controller == null || controller.IsSingleStarMode)
                return;

            var entropy = controller.Entropy;
            if (entropyText != null)
                entropyText.text = $"Entropy: {entropy:0}%";

            if (universeStatusText != null)
                universeStatusText.text = PrototypeEntropyBalance.GetStatusLabel(entropy);

            if (entropySlider != null)
            {
                entropySlider.interactable = false;
                entropySlider.value = entropy / 100f;
            }

            if (entropyFillImage != null)
                entropyFillImage.color = GetEntropyColor(entropy);
        }

        private void RefreshBlackHoleStatus()
        {
            if (blackHoleStatusText == null || controller == null || controller.IsSingleStarMode)
                return;

            if (controller.ActiveBlackHoleCount > 0)
            {
                blackHoleStatusText.text =
                    $"Black Holes: {controller.ActiveBlackHoleCount}  |  " +
                    $"DNA Potential: {controller.RunStats.BlackHoleDnaPotential:0}\n" +
                    "Black Hole destabilizing the universe.";
                blackHoleStatusText.color = new Color(1f, 0.55f, 0.75f);
            }
            else
            {
                blackHoleStatusText.text = "";
            }
        }

        private void RefreshButtons()
        {
            if (controller == null)
                return;

            var canPlay = !controller.IsRunEnded;

            if (createStarButton != null)
            {
                if (controller.IsSingleStarMode)
                {
                    var manager = controller.PlanetManager;
                    var max = manager != null && manager.IsInitialized
                        ? manager.GetMaxPlanets()
                        : PrototypePlanetBalance.BaseMaxPlanets;
                    var count = manager != null && manager.IsInitialized
                        ? manager.PlanetCount
                        : 0;
                    var hasSlot = count < max;
                    var createCost = manager != null && manager.IsInitialized
                        ? manager.GetCreatePlanetCost()
                        : controller.SingleStarBalance.createPlanetCost;
                    createStarButton.interactable = canPlay && hasSlot &&
                                                    controller.Stardust >= createCost;
                }
                else
                {
                    createStarButton.interactable = canPlay &&
                                                    controller.Stardust >= PrototypeGameController.CreateStarCost;
                }
            }

            if (createStarButtonText != null)
            {
                if (controller.IsSingleStarMode)
                {
                    var planetManager = controller.PlanetManager;
                    var createCost = planetManager != null && planetManager.IsInitialized
                        ? planetManager.GetCreatePlanetCost()
                        : controller.SingleStarBalance.createPlanetCost;
                    createStarButtonText.text = $"Create Planet ({createCost:0})";
                }
                else
                {
                    createStarButtonText.text = $"Create New Star ({PrototypeGameController.CreateStarCost})";
                }
            }

            if (collapseUniverseButton != null)
                collapseUniverseButton.interactable = canPlay && !controller.IsSingleStarMode;
        }

        private void RefreshStatsLive()
        {
            if (statsText == null || controller == null)
                return;

            if (controller.IsRunEnded)
            {
                statsText.text = controller.IsSingleStarMode
                    ? "Star system ended. Review your summary and start a new system."
                    : "Universe collapsed. Universe DNA is permanent — spend it on prestige upgrades, then start a new universe.";
                return;
            }

            if (controller.IsSingleStarMode && controller.CentralStar != null)
            {
                var manager = controller.PlanetManager;
                var max = manager != null && manager.IsInitialized
                    ? manager.GetMaxPlanets()
                    : PrototypePlanetBalance.BaseMaxPlanets;
                var count = manager != null && manager.IsInitialized
                    ? manager.PlanetCount
                    : 0;
                statsText.text =
                    $"Star Age: {controller.StarAge}/{controller.GetMaxStarHealth()} ({controller.Stage})\n" +
                    $"Click: +{controller.GetClickReward()}  |  Passive: +{controller.GetTotalPassivePerSecond()}/s\n" +
                    $"Planets: {count}/{max}  |  DNA Potential: {controller.DnaPotential:0}";
                return;
            }

            if (controller.HasActiveStar)
            {
                statsText.text =
                    $"Stars: {controller.ActiveStarCount}  |  Primary Age: {controller.StarAge}/100 ({controller.Stage})\n" +
                    $"Click: +{controller.GetClickReward()}  |  Passive: +{controller.GetTotalPassivePerSecond()}/s\n" +
                    $"Collect radius: {controller.GetClickCollectRadius():0.0}  |  Supernova bonus: +{controller.GetSupernovaBonus()}";
            }
            else
            {
                statsText.text = $"No active stars\nSupernova bonus: +{controller.GetSupernovaBonus()}";
            }
        }

        private void ShowMessage(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                _feedbackTimer = 1.5f;
            }

            Refresh();
        }

        private static Color GetEntropyColor(float entropy)
        {
            if (entropy >= 91f) return new Color(0.95f, 0.2f, 0.2f);
            if (entropy >= 61f) return new Color(0.95f, 0.45f, 0.15f);
            if (entropy >= 31f) return new Color(0.95f, 0.8f, 0.2f);
            return new Color(0.25f, 0.75f, 0.95f);
        }
    }
}
