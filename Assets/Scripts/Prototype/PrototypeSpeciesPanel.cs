using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Universes.Prototype
{
    public class PrototypeSpeciesPanel : MonoBehaviour
    {
        [SerializeField] private PrototypeGameController controller;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private PrototypeSpeciesEntryView speciesEntryPrefab;
        [SerializeField] private GameObject speciesPortraitPrefab;
        [SerializeField] private int portraitPoolPreloadCount = 8;
        [SerializeField] private PrototypeSpeciesPortraitPool portraitPool;
        [SerializeField] private Text emptyText;
        [SerializeField] private GameObject detailPanelRoot;
        [SerializeField] private Button detailCloseButton;
        [SerializeField] private Image detailPortraitImage;
        [SerializeField] private PrototypeSpeciesPortraitMount detailPortraitMount;
        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailInfoText;
        [SerializeField] private Text detailDescriptionText;

        private readonly List<PrototypeSpeciesEntryView> _entries = new();
        private PrototypePlanetManager _subscribedPlanetManager;

        public PrototypeSpeciesPortraitPool PortraitPool => portraitPool;

        private void Start()
        {
            if (controller == null)
                controller = FindAnyObjectByType<PrototypeGameController>();

            if (panelRoot == null)
                panelRoot = gameObject;

            if (portraitPool == null)
                portraitPool = GetComponent<PrototypeSpeciesPortraitPool>();
            if (portraitPool == null)
                portraitPool = gameObject.AddComponent<PrototypeSpeciesPortraitPool>();
            if (speciesPortraitPrefab == null)
                speciesPortraitPrefab = portraitPool.PortraitPrefab;
            portraitPool.Configure(speciesPortraitPrefab, portraitPoolPreloadCount);

            openButton?.onClick.AddListener(Show);
            closeButton?.onClick.AddListener(Hide);
            detailCloseButton?.onClick.AddListener(HideDetails);

            SubscribeToPlanetManager();
            Hide();
            HideDetails();
            Refresh();
        }

        private void OnDestroy()
        {
            openButton?.onClick.RemoveListener(Show);
            closeButton?.onClick.RemoveListener(Hide);
            detailCloseButton?.onClick.RemoveListener(HideDetails);
            UnsubscribeFromPlanetManager();
        }

        private void Update()
        {
            if (_subscribedPlanetManager == null || _subscribedPlanetManager != controller?.PlanetManager)
                SubscribeToPlanetManager();
        }

        public void Show()
        {
            Refresh();
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            HideDetails();
        }

        public void ShowDetails(PrototypePlanet planet)
        {
            var manager = controller != null ? controller.PlanetManager : null;
            var balance = controller != null ? controller.SingleStarBalance : null;
            if (planet == null || manager == null || balance == null)
                return;

            var planetName = manager.GetPlanetDisplayName(planet);
            var stage = PrototypeCivilizationUtility.GetLabel(planet.CivilizationStage, balance.civilization);
            var dnaMultiplier = balance.species.GetDnaPotentialMultiplier(planet.Intelligence, planet.Aggression);
            var clickMultiplier = balance.civilization.GetStageClickBonusMultiplier(planet.CivilizationStage) *
                                  balance.species.GetClickStardustMultiplier(
                                      planet.Intelligence,
                                      planet.Aggression);
            var speedMultiplier = balance.species.GetCivilizationSpeedMultiplier(
                planet.Intelligence,
                planet.Aggression);

            if (detailPortraitImage != null)
                detailPortraitImage.color = PrototypeSpeciesPortraitPool.GetSpeciesColor(planet);

            if (detailPortraitMount != null)
                detailPortraitMount.Bind(planet, portraitPool);

            if (detailTitleText != null)
                detailTitleText.text = planet.SpeciesName;

            if (detailInfoText != null)
            {
                detailInfoText.text =
                    $"Planet: {planetName}\n" +
                    $"Civilization: {planet.CivilizationName}\n" +
                    $"Stage: {stage}\n" +
                    $"Intelligence: {planet.Intelligence}\n" +
                    $"Aggression: {planet.Aggression}\n\n" +
                    $"DNA Potential: {FormatMultiplier(dnaMultiplier)}\n" +
                    $"Planet Click Stardust: {FormatMultiplier(clickMultiplier)}\n" +
                    $"Civilization Progress: {FormatMultiplier(speedMultiplier)}";
            }

            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = !string.IsNullOrWhiteSpace(planet.SpeciesDescription)
                    ? planet.SpeciesDescription
                    : "A quiet species with no recorded history yet.";
            }

            if (detailPanelRoot != null)
                detailPanelRoot.SetActive(true);
        }

        public void HideDetails()
        {
            if (detailPanelRoot != null)
                detailPanelRoot.SetActive(false);
        }

        private void SubscribeToPlanetManager()
        {
            UnsubscribeFromPlanetManager();

            _subscribedPlanetManager = controller != null ? controller.PlanetManager : null;
            if (_subscribedPlanetManager == null)
                return;

            _subscribedPlanetManager.OnPlanetsChanged += Refresh;
            _subscribedPlanetManager.OnCivilizationAdvanced += OnCivilizationAdvanced;
            _subscribedPlanetManager.OnCivilizationEvent += OnCivilizationEvent;
        }

        private void UnsubscribeFromPlanetManager()
        {
            if (_subscribedPlanetManager == null)
                return;

            _subscribedPlanetManager.OnPlanetsChanged -= Refresh;
            _subscribedPlanetManager.OnCivilizationAdvanced -= OnCivilizationAdvanced;
            _subscribedPlanetManager.OnCivilizationEvent -= OnCivilizationEvent;
            _subscribedPlanetManager = null;
        }

        private void OnCivilizationAdvanced(PrototypePlanet planet, PrototypeCivilizationStage stage) => Refresh();

        private void OnCivilizationEvent(string message) => Refresh();

        private void Refresh()
        {
            ClearEntries();

            var manager = controller != null ? controller.PlanetManager : null;
            var balance = controller != null ? controller.SingleStarBalance : null;
            if (manager == null || balance == null || contentRoot == null || speciesEntryPrefab == null)
            {
                SetEmptyVisible(true);
                return;
            }

            var shown = 0;
            foreach (var planet in manager.Planets)
            {
                if (planet == null || !planet.IsAlive || !planet.HasSpecies)
                    continue;

                var entry = Instantiate(speciesEntryPrefab, contentRoot);
                entry.name = $"Species_{planet.SpeciesName}";
                entry.Bind(planet, this);
                _entries.Add(entry);
                shown++;
            }

            SetEmptyVisible(shown == 0);
        }

        private void ClearEntries()
        {
            foreach (var entry in _entries)
            {
                if (entry != null)
                {
                    entry.ReleasePortrait();
                    Destroy(entry.gameObject);
                }
            }

            _entries.Clear();
        }

        private void SetEmptyVisible(bool visible)
        {
            if (emptyText != null)
                emptyText.gameObject.SetActive(visible);
        }

        private static string FormatMultiplier(float value)
        {
            var percent = Mathf.RoundToInt((value - 1f) * 100f);
            return percent >= 0 ? $"+{percent}%" : $"{percent}%";
        }
    }
}
