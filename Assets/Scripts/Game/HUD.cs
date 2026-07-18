using UnityEngine;
using UnityEngine.UI;

namespace Universes.Game
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private GameController controller;
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
        [SerializeField] private Button createPlanetButton;
        [SerializeField] private Text createPlanetButtonText;
        [SerializeField] private Button collapseUniverseButton;
        [SerializeField] private CollapsePanel collapsePanel;
        [SerializeField] private VictoryPanel victoryPanel;
        [SerializeField] private StarSystemEndPanel starSystemEndPanel;
        [SerializeField] private GameObject starTooltipRoot;
        [SerializeField] private Text starTooltipText;

        private float _feedbackTimer;
        private Canvas _canvas;
        private StarView _starTooltipTarget;

        private void Start()
        {
            if (controller == null)
                controller = FindAnyObjectByType<GameController>();

            if (collapsePanel == null)
                collapsePanel = FindAnyObjectByType<CollapsePanel>(FindObjectsInactive.Include);

            if (victoryPanel == null)
                victoryPanel = FindAnyObjectByType<VictoryPanel>(FindObjectsInactive.Include);

            if (starSystemEndPanel == null)
                starSystemEndPanel = FindAnyObjectByType<StarSystemEndPanel>(FindObjectsInactive.Include);

            _canvas = GetComponent<Canvas>();

            if (stardustCollector == null && stardustText != null)
                stardustCollector = stardustText.rectTransform;

            if (dnaCollector == null && dnaText != null)
                dnaCollector = dnaText.rectTransform;

            var particleManager = controller != null
                ? controller.GetComponent<CosmicParticleManager>()
                : FindAnyObjectByType<CosmicParticleManager>();

            if (particleManager != null)
                ConfigureParticleManager(particleManager);

            var planetManager = controller.PlanetManager;
            if (planetManager != null)
            {
                planetManager.OnCivilizationAdvanced += OnCivilizationAdvanced;
                planetManager.OnCivilizationEvent += ShowMessage;
            }

            createStarButton?.onClick.AddListener(OnPrimaryActionClicked);
            createPlanetButton?.onClick.AddListener(OnCreatePlanetClicked);
            collapseUniverseButton?.onClick.AddListener(() => controller.TryEndUniverse());

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

        private void OnCivilizationAdvanced(Planet planet, CivilizationStage stage)
        {
            if (planet == null)
                return;

            var planetName = controller.PlanetManager != null
                ? controller.PlanetManager.GetPlanetDisplayName(planet)
                : PlanetTypeUtility.GetLabel(planet.Definition);
            var stageName = CivilizationUtility.GetLabel(stage, controller.SingleStarBalance.civilization);
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

        private void OnCreatePlanetClicked()
        {
            if (controller == null)
                return;

            controller.TryCreatePlanet();
        }

        private void OnUniverseCollapsed(bool manualCollapse)
        {
            if (controller.IsSingleStarMode)
                return;

            Refresh();

            if (controller.IsGameWon)
            {
                if (victoryPanel == null)
                    victoryPanel = FindAnyObjectByType<VictoryPanel>(FindObjectsInactive.Include);

                victoryPanel?.Show();
                return;
            }

            if (collapsePanel == null)
                collapsePanel = FindAnyObjectByType<CollapsePanel>(FindObjectsInactive.Include);

            collapsePanel?.Show(manualCollapse);
        }

        private void OnStarSystemEnded()
        {
            Refresh();
            if (starSystemEndPanel == null)
                starSystemEndPanel = FindAnyObjectByType<StarSystemEndPanel>(FindObjectsInactive.Include);

            starSystemEndPanel?.Show();
        }

        public void ConfigureParticleManager(CosmicParticleManager particleManager)
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

            var usesEntropy = GameplayFeatures.UsesEntropy(GetGameplayMode());
            if (entropyPanelRoot != null)
                entropyPanelRoot.SetActive(usesEntropy);
            else if (entropyText != null)
                entropyText.transform.parent?.gameObject.SetActive(usesEntropy);

            if (collapseUniverseButton != null)
            {
                var showCollapse = !controller.IsSingleStarMode;
                collapseUniverseButton.gameObject.SetActive(showCollapse);
                if (showCollapse)
                    collapseUniverseButton.interactable = !controller.IsRunEnded;
            }

            if (blackHoleStatusText != null && !GameplayFeatures.UsesBlackHoles(GetGameplayMode()))
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
            UpdateStarTooltip();
            if (GameplayFeatures.UsesBlackHoles(GetGameplayMode()))
                RefreshBlackHoleStatus();
        }

        private void Refresh()
        {
            if (controller == null)
                return;

            ApplyModeVisibility();

            if (stardustText != null)
                stardustText.text = $"Stardust: {controller.Stardust:0}";

            if (dnaText != null)
            {
                dnaText.text = controller.IsSingleStarMode ||
                               (GameplayFeatures.UsesMultiStar(GetGameplayMode()) &&
                                !GameplayFeatures.UsesUniverseCollapse(GetGameplayMode()))
                    ? $"DNA Potential: {controller.DnaPotential:0}"
                    : $"DNA Fragments: {controller.DnaFragments}";
            }

            if (universeDnaText != null)
                universeDnaText.text = $"Universe DNA: {controller.Prestige.UniverseDna:0}";

            if (GameplayFeatures.UsesEntropy(GetGameplayMode()))
                RefreshEntropy();

            RefreshStatsLive();
            RefreshButtons();
        }

        private void RefreshEntropy()
        {
            if (controller == null || !GameplayFeatures.UsesEntropy(GetGameplayMode()))
                return;

            var entropy = controller.Entropy;
            var maxEntropy = controller.MaxEntropy;
            if (entropyText != null)
            {
                var percent = maxEntropy > 0f ? entropy / maxEntropy * 100f : 0f;
                entropyText.text = $"Entropy: {percent:0.#}%";
            }

            if (universeStatusText != null)
                universeStatusText.text = EntropyBalance.GetStatusLabel(entropy, maxEntropy);

            if (entropySlider != null)
            {
                entropySlider.interactable = false;
                entropySlider.value = maxEntropy > 0f ? entropy / maxEntropy : 0f;
            }

            if (entropyFillImage != null)
                entropyFillImage.color = GetEntropyColor(maxEntropy > 0f ? entropy / maxEntropy * 100f : 0f);
        }

        private void RefreshBlackHoleStatus()
        {
            if (blackHoleStatusText == null || controller == null ||
                !GameplayFeatures.UsesBlackHoles(GetGameplayMode()))
                return;

            if (controller.ActiveBlackHoleCount > 0)
            {
                blackHoleStatusText.text =
                    $"Black Holes: {controller.ActiveBlackHoleCount}  |  " +
                    $"DNA Potential: +{controller.ActiveBlackHoleDnaPerSecond:0.0}/s  |  " +
                    $"Instability: +{controller.ActiveBlackHoleInstabilityPerSecond:0.0}/s\n" +
                    "Warning: Black Holes are destabilizing the universe.";
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
                        : PlanetBalance.BaseMaxPlanets;
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
                    createStarButton.interactable = canPlay && controller.CanCreateStarOrConstellation(out _);
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
                else if (GameplayFeatures.UsesUniverseCollapse(GetGameplayMode()))
                {
                    var createCost = controller.GetCreateConstellationCost();
                    var count = controller.GetConstellationStarCount();
                    createStarButtonText.text = controller.CanCreateConstellation(out var reason)
                        ? $"Create Constellation x{count} ({createCost:0})"
                        : $"Create Constellation ({reason})";
                }
                else
                {
                    var createCost = controller.GetCreateStarCost();
                    createStarButtonText.text = controller.CanCreateNewStar(out var reason)
                        ? $"Create Star ({createCost:0})"
                        : $"Create Star ({reason})";
                }
            }

            if (createPlanetButton != null)
            {
                createPlanetButton.gameObject.SetActive(!controller.IsSingleStarMode);
                var manager = controller.PlanetManager;
                var createCost = manager != null && manager.IsInitialized
                    ? manager.GetCreatePlanetCost()
                    : controller.SingleStarBalance.createPlanetCost;
                var canCreatePlanet = canPlay && controller.SelectedStar != null &&
                                      manager != null && manager.IsInitialized &&
                                      manager.HasOpenSlot() &&
                                      controller.Stardust >= createCost;
                createPlanetButton.interactable = canCreatePlanet;
            }

            if (createPlanetButtonText != null)
            {
                var manager = controller.PlanetManager;
                var createCost = manager != null && manager.IsInitialized
                    ? manager.GetCreatePlanetCost()
                    : controller.SingleStarBalance.createPlanetCost;
                var starName = controller.SelectedStar != null ? controller.SelectedStar.StarName : "Select Star";
                createPlanetButtonText.text = $"Create Planet: {starName} ({createCost:0})";
            }

            if (collapseUniverseButton != null)
            {
                var showCollapse = !controller.IsSingleStarMode;
                collapseUniverseButton.gameObject.SetActive(showCollapse);
                if (showCollapse)
                    collapseUniverseButton.interactable = canPlay;
            }
        }

        private GameplayMode GetGameplayMode()
        {
            if (controller == null)
                return GameplayMode.SingleStarSystemAge;

            return controller.GameplayMode;
        }

        private void RefreshStatsLive()
        {
            if (statsText == null || controller == null)
                return;

            statsText.text = FormatSurvivalTime(controller.RunStats.SurvivalTimeSeconds);
        }

        public void ShowStarTooltip(StarView star)
        {
            if (star == null || controller == null || controller.IsRunEnded || controller.IsCollapsed)
                return;

            EnsureStarTooltip();
            if (starTooltipRoot == null || starTooltipText == null)
                return;

            _starTooltipTarget = star;
            starTooltipText.text = BuildStarTooltipText(star);
            PositionStarTooltip(star.transform.position);
            starTooltipRoot.SetActive(true);
        }

        public void HideStarTooltip()
        {
            _starTooltipTarget = null;
            if (starTooltipRoot != null)
                starTooltipRoot.SetActive(false);
        }

        private void UpdateStarTooltip()
        {
            if (controller == null || controller.IsRunEnded || controller.IsCollapsed)
            {
                HideStarTooltip();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                HideStarTooltip();

            if (Input.GetMouseButtonDown(1) && !IsPointerOverUi())
            {
                var star = StarView.GetStarUnderMouse();
                if (star != null && star.IsInteractable)
                {
                    if (_starTooltipTarget == star)
                        HideStarTooltip();
                    else
                        ShowStarTooltip(star);
                }
                else if (_starTooltipTarget != null)
                {
                    HideStarTooltip();
                }
            }

            if (_starTooltipTarget == null)
                return;

            if (!_starTooltipTarget.IsInteractable)
            {
                HideStarTooltip();
                return;
            }

            RefreshActiveStarTooltip(_starTooltipTarget);
        }

        private void RefreshActiveStarTooltip(StarView star)
        {
            if (starTooltipRoot == null || starTooltipText == null || !starTooltipRoot.activeSelf)
                return;

            starTooltipText.text = BuildStarTooltipText(star);
            PositionStarTooltip(star.transform.position);
        }

        private static bool IsPointerOverUi() =>
            UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        private string BuildStarTooltipText(StarView star)
        {
            var manager = controller.PlanetManager;
            var maxPlanets = manager != null && manager.IsInitialized
                ? manager.GetMaxPlanets()
                : PlanetBalance.BaseMaxPlanets;
            var planetCount = controller.GetPlanetCountForStar(star);
            var selected = !controller.IsSingleStarMode && controller.SelectedStar == star ? "\nSelected star" : string.Empty;
            var starCountLine = controller.IsSingleStarMode
                ? string.Empty
                : $"\nStars in system: {controller.ActiveStarCount}/{controller.MaxStarSlots}";

            return $"{star.StarName}\n" +
                   $"Age: {star.StarAge}/{star.MaxStarAge} years\n" +
                   $"Stage: {star.Stage}\n" +
                   $"Planets: {planetCount}/{maxPlanets}\n" +
                   $"Click: +{controller.GetClickReward(star)}\n" +
                   $"Passive: +{controller.GetPassivePerSecond(star)}/s" +
                   selected +
                   starCountLine;
        }

        private void PositionStarTooltip(Vector3 worldPosition)
        {
            if (starTooltipRoot == null)
                return;

            if (_canvas == null)
                _canvas = GetComponent<Canvas>();

            var canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
            if (canvasRect == null)
                return;

            var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, camera,
                    out var localPoint))
                return;

            var tooltipRect = starTooltipRoot.GetComponent<RectTransform>();
            tooltipRect.anchoredPosition = localPoint + new Vector2(28f, 36f);
        }

        private void EnsureStarTooltip()
        {
            if (starTooltipRoot != null && starTooltipText != null)
                return;

            if (_canvas == null)
                _canvas = GetComponent<Canvas>();

            var parent = transform;
            starTooltipRoot = new GameObject("StarTooltip", typeof(RectTransform), typeof(Image));
            starTooltipRoot.transform.SetParent(parent, false);
            var tooltipRect = starTooltipRoot.GetComponent<RectTransform>();
            tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.pivot = new Vector2(0f, 0.5f);
            tooltipRect.sizeDelta = new Vector2(260f, 170f);
            var tooltipImage = starTooltipRoot.GetComponent<Image>();
            tooltipImage.color = new Color(0.04f, 0.055f, 0.08f, 0.96f);
            tooltipImage.raycastTarget = false;

            var textGo = new GameObject("TooltipText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(starTooltipRoot.transform, false);
            starTooltipText = textGo.GetComponent<Text>();
            starTooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            starTooltipText.fontSize = 14;
            starTooltipText.alignment = TextAnchor.UpperLeft;
            starTooltipText.color = new Color(0.85f, 0.92f, 1f);
            starTooltipText.raycastTarget = false;
            var textRect = starTooltipText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 10f);
            textRect.offsetMax = new Vector2(-12f, -10f);

            starTooltipRoot.SetActive(false);
        }

        private static string FormatSurvivalTime(float seconds)
        {
            var minutes = Mathf.FloorToInt(seconds / 60f);
            var secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
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
