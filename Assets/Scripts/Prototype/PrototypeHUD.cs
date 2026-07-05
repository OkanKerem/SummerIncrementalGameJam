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
        [SerializeField] private RectTransform stardustCollector;
        [SerializeField] private RectTransform dnaCollector;
        [SerializeField] private Button createStarButton;
        [SerializeField] private Text createStarButtonText;
        [SerializeField] private Button collapseUniverseButton;
        [SerializeField] private PrototypeCollapsePanel collapsePanel;

        private float _feedbackTimer;
        private Canvas _canvas;

        private void Start()
        {
            if (controller == null)
                controller = FindAnyObjectByType<PrototypeGameController>();

            if (collapsePanel == null)
                collapsePanel = FindAnyObjectByType<PrototypeCollapsePanel>(FindObjectsInactive.Include);

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

            createStarButton?.onClick.AddListener(() => controller.TryCreateNewStar());
            collapseUniverseButton?.onClick.AddListener(() => controller.TryCollapseUniverse());

            controller.OnStateChanged += Refresh;
            controller.OnStardustGained += _ => Refresh();
            controller.OnDnaGained += Refresh;
            controller.OnEntropyChanged += _ => RefreshEntropy();
            controller.OnSupernova += () => ShowMessage($"SUPERNOVA! +{controller.GetSupernovaBonus()} bonus");
            controller.OnStarCollision += () => ShowMessage("STAR COLLISION!");
            controller.OnBlackHoleSpawned += () => ShowMessage("BLACK HOLE formed!");
            controller.OnUniverseCollapsed += OnUniverseCollapsed;
            controller.Prestige.OnChanged += Refresh;

            Refresh();
        }

        private void OnUniverseCollapsed(bool manualCollapse)
        {
            Refresh();
            if (collapsePanel == null)
                collapsePanel = FindAnyObjectByType<PrototypeCollapsePanel>(FindObjectsInactive.Include);

            collapsePanel?.Show(manualCollapse);
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

        private void Update()
        {
            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f && feedbackText != null)
                    feedbackText.text = "";
            }

            RefreshStatsLive();
            RefreshBlackHoleStatus();
        }

        private void Refresh()
        {
            if (controller == null)
                return;

            if (stardustText != null)
                stardustText.text = $"Stardust: {controller.Stardust:0}";

            if (dnaText != null)
                dnaText.text = $"DNA Fragments: {controller.DnaFragments}";

            if (universeDnaText != null)
                universeDnaText.text = $"Universe DNA: {controller.Prestige.UniverseDna:0}";

            RefreshEntropy();
            RefreshStatsLive();
            RefreshBlackHoleStatus();
            RefreshButtons();
        }

        private void RefreshEntropy()
        {
            if (controller == null)
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
            if (blackHoleStatusText == null || controller == null)
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

            var canPlay = !controller.IsCollapsed;

            if (createStarButton != null)
                createStarButton.interactable = canPlay && controller.Stardust >= PrototypeGameController.CreateStarCost;

            if (createStarButtonText != null)
                createStarButtonText.text = $"Create New Star ({PrototypeGameController.CreateStarCost})";

            if (collapseUniverseButton != null)
                collapseUniverseButton.interactable = canPlay;
        }

        private void RefreshStatsLive()
        {
            if (statsText == null || controller == null)
                return;

            if (controller.IsCollapsed)
            {
                statsText.text =
                    "Universe collapsed. Universe DNA is permanent — spend it on prestige upgrades, then start a new universe.";
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
                statsText.text =
                    $"No active stars\n" +
                    $"Supernova bonus: +{controller.GetSupernovaBonus()}";
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
