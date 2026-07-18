using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Universes.Game
{
    public class SpeciesPanel : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private SpeciesEntryView speciesEntryPrefab;
        [SerializeField] private GameObject speciesPortraitPrefab;
        [SerializeField] private int portraitPoolPreloadCount = 8;
        [SerializeField] private SpeciesPortraitPool portraitPool;
        [SerializeField] private Text emptyText;
        [SerializeField] private GameObject detailPanelRoot;
        [SerializeField] private Button detailCloseButton;
        [SerializeField] private Image detailPortraitImage;
        [SerializeField] private SpeciesPortraitMount detailPortraitMount;
        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailInfoText;
        [SerializeField] private Text detailDescriptionText;

        private readonly List<SpeciesEntryView> _entries = new();
        private PlanetManager _subscribedPlanetManager;

        public SpeciesPortraitPool PortraitPool => portraitPool;

        private void Start()
        {
            if (controller == null)
                controller = FindAnyObjectByType<GameController>();

            if (panelRoot == null)
                panelRoot = gameObject;

            if (portraitPool == null)
                portraitPool = GetComponent<SpeciesPortraitPool>();
            if (portraitPool == null)
                portraitPool = gameObject.AddComponent<SpeciesPortraitPool>();
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

        public void ShowDetails(Planet planet)
        {
            var manager = controller != null ? controller.PlanetManager : null;
            var balance = controller != null ? controller.SingleStarBalance : null;
            if (planet == null || !planet.HasSpecies || manager == null || balance == null)
                return;

            var colonies = GetColoniesForSpecies(planet.SpeciesName, manager);
            if (colonies.Count == 0)
                return;

            var representative = PickRepresentative(colonies);
            var stage = CivilizationUtility.GetLabel(representative.CivilizationStage, balance.civilization);
            var dnaMultiplier = balance.species.GetDnaPotentialMultiplier(
                representative.Intelligence,
                representative.Aggression);
            var clickMultiplier = balance.civilization.GetStageClickBonusMultiplier(representative.CivilizationStage) *
                                  balance.species.GetClickStardustMultiplier(
                                      representative.Intelligence,
                                      representative.Aggression);
            var speedMultiplier = balance.species.GetCivilizationSpeedMultiplier(
                representative.Intelligence,
                representative.Aggression);

            if (detailPortraitImage != null)
                detailPortraitImage.color = SpeciesPortraitPool.GetSpeciesColor(representative);

            if (detailPortraitMount != null)
                detailPortraitMount.Bind(representative, portraitPool);

            if (detailTitleText != null)
                detailTitleText.text = representative.SpeciesName;

            if (detailInfoText != null)
            {
                var hardSpaceRequirement = balance.phase3.hardSpaceCompletionRequirement;
                var hardSpaceProgress = representative.CivilizationStage == CivilizationStage.HardSpace
                    ? $"\nHard Space Completion: {representative.HardSpaceCompletionProgress:0.0}/{hardSpaceRequirement:0.0}"
                    : string.Empty;
                var spaceProgram = !string.IsNullOrWhiteSpace(representative.SpaceProgramName)
                    ? $"\nSpace Program: {representative.SpaceProgramName}"
                    : string.Empty;

                detailInfoText.text =
                    $"Homeworld: {manager.GetPlanetDisplayName(representative)}\n" +
                    $"Civilization: {representative.CivilizationName}\n" +
                    $"Stage: {stage}\n" +
                    $"Intelligence: {representative.Intelligence}\n" +
                    $"Aggression: {representative.Aggression}" +
                    spaceProgram +
                    hardSpaceProgress +
                    "\n\n" +
                    $"DNA Potential: {FormatMultiplier(dnaMultiplier)}\n" +
                    $"Planet Click Stardust: {FormatMultiplier(clickMultiplier)}\n" +
                    $"Civilization Progress: {FormatMultiplier(speedMultiplier)}" +
                    BuildColonyListText(colonies, manager, balance);
            }

            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = !string.IsNullOrWhiteSpace(representative.SpeciesDescription)
                    ? representative.SpeciesDescription
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

        private void OnCivilizationAdvanced(Planet planet, CivilizationStage stage) => Refresh();

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

            var groups = BuildSpeciesGroups(manager);
            var shown = 0;
            foreach (var group in groups.Values)
            {
                if (group.Count == 0)
                    continue;

                var representative = PickRepresentative(group);
                var entry = Instantiate(speciesEntryPrefab, contentRoot);
                entry.name = $"Species_{representative.SpeciesName}";
                entry.Bind(representative, this, group.Count);
                _entries.Add(entry);
                shown++;
            }

            SetEmptyVisible(shown == 0);
        }

        private static Dictionary<string, List<Planet>> BuildSpeciesGroups(PlanetManager manager)
        {
            var groups = new Dictionary<string, List<Planet>>();
            foreach (var planet in manager.Planets)
            {
                if (planet == null || !planet.IsAlive || !planet.HasSpecies)
                    continue;

                if (!groups.TryGetValue(planet.SpeciesName, out var colonies))
                {
                    colonies = new List<Planet>();
                    groups[planet.SpeciesName] = colonies;
                }

                colonies.Add(planet);
            }

            return groups;
        }

        private static List<Planet> GetColoniesForSpecies(string speciesName, PlanetManager manager)
        {
            return manager.Planets
                .Where(p => p != null && p.IsAlive && p.HasSpecies && p.SpeciesName == speciesName)
                .OrderByDescending(p => p.CivilizationStage)
                .ThenBy(p => manager.GetPlanetDisplayName(p))
                .ToList();
        }

        private static Planet PickRepresentative(IReadOnlyList<Planet> colonies)
        {
            Planet best = colonies[0];
            for (var i = 1; i < colonies.Count; i++)
            {
                var candidate = colonies[i];
                if (IsBetterRepresentative(candidate, best))
                    best = candidate;
            }

            return best;
        }

        private static bool IsBetterRepresentative(Planet candidate, Planet current)
        {
            if (candidate.CivilizationStage != current.CivilizationStage)
                return candidate.CivilizationStage > current.CivilizationStage;

            if (candidate.Intelligence != current.Intelligence)
                return candidate.Intelligence > current.Intelligence;

            return candidate.Id < current.Id;
        }

        private static string BuildColonyListText(IReadOnlyList<Planet> colonies, PlanetManager manager,
            SingleStarBalance balance)
        {
            if (colonies.Count <= 1)
                return string.Empty;

            var text = new StringBuilder();
            text.AppendLine("\n\nColonies:");
            foreach (var colony in colonies)
            {
                var stage = CivilizationUtility.GetLabel(colony.CivilizationStage, balance.civilization);
                text.AppendLine($"- {manager.GetPlanetDisplayName(colony)} ({stage})");
            }

            return text.ToString().TrimEnd();
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
