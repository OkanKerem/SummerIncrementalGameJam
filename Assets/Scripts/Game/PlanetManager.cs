using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Universes.Game
{
    public class PlanetManager : MonoBehaviour
    {
        [SerializeField] private PlanetView planetPrefab;
        [SerializeField] private PlanetTypeCatalog planetTypeCatalog;
        [SerializeField] private Transform planetsRoot;
        [SerializeField] private Vector2 orbitEllipseScale = new(1.25f, 0.58f);

        private GameController _controller;
        private StarView _hostStar;
        private readonly List<Planet> _planets = new();
        private readonly Dictionary<int, PlanetView> _views = new();
        private readonly List<GameObject> _spaceships = new();
        private readonly List<SpaceStationView> _spaceStations = new();
        private DestroyRocketPool _destroyRocketPool;
        private int _nextPlanetId = 1;
        private float _autoFormationTimer;
        private float _civilizationTimer;
        private float _dnaTimer;
        private float _spaceTravelTimer;
        private float _spaceStationTimer;
        private int _activeSpaceships;
        private bool _destroyProtocolStarted;

        public IReadOnlyList<Planet> Planets => _planets;
        public int PlanetCount => _planets.Count(p => p.IsAlive);
        public int HighestCivilizationRank { get; private set; }

        public bool IsInitialized => _controller != null;

        public event Action OnPlanetsChanged;
        public event Action<Planet, CivilizationStage> OnCivilizationAdvanced;
        public event Action<string> OnCivilizationEvent;

        private SingleStarBalance Balance => _controller?.SingleStarBalance;

        public void Initialize(GameController controller, Transform root,
            PlanetTypeCatalog catalog = null)
        {
            _controller = controller;
            if (catalog != null)
                planetTypeCatalog = catalog;
            if (planetsRoot == null)
            {
                var go = new GameObject("Planets");
                go.transform.SetParent(root);
                planetsRoot = go.transform;
            }
        }

        public void BindHostStar(StarView star) => _hostStar = star;

        public void ResetAll()
        {
            foreach (var view in _views.Values)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }

            foreach (var spaceship in _spaceships)
            {
                if (spaceship != null)
                    Destroy(spaceship);
            }

            foreach (var station in _spaceStations)
            {
                if (station != null)
                    Destroy(station.gameObject);
            }

            _destroyRocketPool?.ReturnAll();

            _planets.Clear();
            _views.Clear();
            _spaceships.Clear();
            _spaceStations.Clear();
            _nextPlanetId = 1;
            _autoFormationTimer = 0f;
            _civilizationTimer = 0f;
            _dnaTimer = 0f;
            _spaceTravelTimer = 0f;
            _spaceStationTimer = 0f;
            _activeSpaceships = 0;
            _destroyProtocolStarted = false;
            HighestCivilizationRank = 0;
            StopAllCoroutines();
        }

        public int GetMaxPlanets() =>
            _controller != null
                ? Balance.baseMaxPlanets + _controller.Upgrades.MaxPlanetCountLevel
                : PlanetBalance.BaseMaxPlanets;

        public float GetOrbitSpeed() =>
            Balance != null ? Balance.orbitSpeed : PlanetBalance.OrbitSpeed;

        public bool ShowOrbitLines => Balance == null || Balance.showOrbitLines;
        public Color OrbitLineColor => Balance != null ? Balance.orbitLineColor : new Color(0.55f, 0.75f, 1f, 0.22f);
        public float OrbitLineWidth => Balance != null ? Balance.orbitLineWidth : 0.025f;
        public int OrbitLineSegments => Balance != null ? Balance.orbitLineSegments : 72;
        public int OrbitLineSortingOrder => Balance != null ? Balance.orbitLineSortingOrder : 0;

        public Vector3 GetOrbitOffset(Planet planet)
        {
            if (planet == null)
                return Vector3.zero;

            return GetOrbitOffset(planet.OrbitRadius, planet.OrbitAngle);
        }

        public Vector3 GetOrbitOffset(float orbitRadius, float orbitAngle)
        {
            var rad = orbitAngle * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Cos(rad) * orbitRadius * orbitEllipseScale.x,
                Mathf.Sin(rad) * orbitRadius * orbitEllipseScale.y,
                0f);
        }

        public double GetCreatePlanetCost()
        {
            if (Balance == null)
                return PlanetBalance.CreatePlanetCost;

            var createdPlanets = _controller != null ? _controller.RunStats.PlanetsCreated : 0;
            var multiplier = 1.0 + Balance.createPlanetCostIncreasePercent / 100.0;
            return Balance.createPlanetCost * Math.Pow(multiplier, createdPlanets);
        }

        public bool HasOpenSlot() => _hostStar != null && GetPlanetCountForStar(_hostStar.StarId) < GetMaxPlanets();

        public int GetPlanetCountForStar(int starId) =>
            _planets.Count(p => p.IsAlive && p.HostStarId == starId);

        public float GetStarOrbitFootprintRadius(int starId)
        {
            if (Balance == null)
                return 0f;

            var planetCount = GetPlanetCountForStar(starId);
            return GetOrbitFootprintForPlanetCount(planetCount);
        }

        public float GetEstimatedEmptyStarOrbitFootprintRadius() =>
            GetOrbitFootprintForPlanetCount(GetMaxPlanets());

        private float GetOrbitFootprintForPlanetCount(int planetCount)
        {
            if (Balance == null)
                return 0f;

            var scaledCount = Mathf.Max(0, planetCount);
            var orbitReach = Balance.baseOrbitRadius + scaledCount * Balance.orbitRadiusStep;
            var ellipseScale = Mathf.Max(orbitEllipseScale.x, orbitEllipseScale.y);
            return orbitReach * ellipseScale;
        }

        public void HideAllOrbitLines()
        {
            foreach (var view in _views.Values)
                view?.HideOrbitLine();
        }

        public void PullAllToward(Vector3 center, float speed, float deltaTime)
        {
            foreach (var view in _views.Values)
            {
                if (view == null)
                    continue;

                view.HideOrbitLine();
                view.transform.position = Vector3.MoveTowards(
                    view.transform.position,
                    center,
                    speed * deltaTime);
                view.transform.localScale = Vector3.Max(
                    view.transform.localScale * (1f - deltaTime * 1.1f),
                    Vector3.one * 0.01f);
            }
        }

        public bool TryCreatePlanet(bool free = false) =>
            TryCreatePlanetForStar(_hostStar, free);

        public bool TryCreatePlanetForStar(StarView hostStar, bool free = false, bool playCreationSound = true)
        {
            if (_controller == null || hostStar == null || !hostStar.IsInteractable)
                return false;

            if (GetPlanetCountForStar(hostStar.StarId) >= GetMaxPlanets())
                return false;

            if (!free)
            {
                var cost = GetCreatePlanetCost();
                if (_controller.Stardust < cost)
                    return false;

                _controller.SpendStardust(cost);
            }

            CreateRandomPlanetForStar(hostStar, playCreationSound);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_controller == null)
                return;

            foreach (var view in _views.Values)
                view?.TickOrbit(deltaTime);

            TickPlanetCollisions();
            TickAutoFormation(deltaTime);
            TickCivilization(deltaTime);
            TickDnaGeneration(deltaTime);
            TickSpaceTravel(deltaTime);
            TickSpaceStations(deltaTime);
        }

        public void OnPlanetClicked(PlanetView view)
        {
            if (_controller == null || view?.Planet == null || !view.Planet.IsAlive)
                return;

            var planet = view.Planet;
            var reward = _controller.GetPlanetClickReward(planet);
            var damage = _controller.GetPlanetClickDamage();

            var creditedReward = _controller.CreditStardustDirect(reward);
            _controller.RunStats.RecordPlanetClick();
            _controller.FloatingTextSpawner?.Spawn(view.transform.position, Mathf.RoundToInt((float)creditedReward), view.GetDisplayColor());
            _controller.SfxManager?.PlayPlanetClick();
            planet.Damage(damage);
            view.RefreshVisual();
            PlayPlanetClickEffect(view);

            if (!planet.IsAlive)
            {
                PlayPlanetDestroyEffect(view);
                DestroyPlanet(planet.Id, causeStarDamage: true, grantDna: true);
            }

            OnPlanetsChanged?.Invoke();
            _controller.NotifyStateChanged();
        }

        public void DestroyAllPlanets()
        {
            foreach (var id in _planets.Select(p => p.Id).ToList())
                DestroyPlanet(id, recordDestruction: false);
        }

        private void CreateRandomPlanet() => CreateRandomPlanetForStar(_hostStar);

        private void CreateRandomPlanetForStar(StarView hostStar, bool playCreationSound = true)
        {
            if (hostStar == null)
                return;

            StartCoroutine(CreateRandomPlanetRoutine(hostStar, playCreationSound));
        }

        private IEnumerator CreateRandomPlanetRoutine(StarView hostStar, bool playCreationSound = true)
        {
            if (hostStar == null || !hostStar.IsInteractable)
                yield break;

            var slot = FindOpenOrbitSlot(hostStar.StarId);
            if (slot < 0)
                yield break;

            var definition = RollPlanetTypeDefinition();
            if (definition == null)
            {
                Debug.LogWarning("No planet type definitions available. Assign Planet Type Catalog on PlanetManager.",
                    this);
                yield break;
            }

            var habitable = RollHabitable(definition, hostStar);
            var orbitRadius = CalculateOrbitRadius(hostStar.StarId, slot, definition);
            var angle = UnityEngine.Random.Range(0f, 360f);

            var planet = new Planet(_nextPlanetId++, hostStar.StarId, definition, slot, orbitRadius, angle, habitable,
                Balance.planets.fallbackBaseDurability,
                PrestigeModifiers.GetPlanetLifetimeMultiplier(_controller.Prestige));
            _planets.Add(planet);

            OnPlanetsChanged?.Invoke();
            _controller.NotifyStateChanged();

            PlayPlanetSpawnEffect(planet, playCreationSound);

            var delay = Balance.planetSpawnDelay;
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (!_planets.Contains(planet) || !planet.IsAlive)
                yield break;

            SpawnView(planet);
            _controller.RunStats.RecordPlanetCreated(PlanetCount);
            OnPlanetsChanged?.Invoke();
            _controller.NotifyStateChanged();
        }

        private void SpawnView(Planet planet)
        {
            if (planetPrefab == null)
                return;

            var hostStar = GetHostStar(planet);
            if (hostStar == null)
                return;

            var view = Instantiate(planetPrefab, planetsRoot);
            view.name = $"Planet_{PlanetTypeUtility.GetLabel(planet.Definition)}";
            view.Bind(planet, this, hostStar.transform);
            _views[planet.Id] = view;
        }

        private PlanetTypeDefinition RollPlanetTypeDefinition()
        {
            if (planetTypeCatalog != null)
            {
                var rolled = planetTypeCatalog.RollRandom();
                if (rolled != null)
                    return rolled;
            }

            return PlanetTypeUtility.CreateFallbackDefinition(
                PlanetTypeUtility.RollRandomFallback(Balance.planets),
                Balance.planets);
        }

        private void PlayPlanetSpawnEffect(Planet planet, bool playSound = true)
        {
            if (planet?.Definition == null)
                return;

            var hostStar = GetHostStar(planet);
            if (hostStar == null)
                return;

            var position = hostStar.transform.position + GetOrbitOffset(planet);
            if (playSound)
                _controller.SfxManager?.PlayPlanetCreated();
            planet.Definition.PlayEffect(planet.Definition.spawnEffectPrefab, position, planetsRoot);
        }

        private void PlayPlanetClickEffect(PlanetView view)
        {
            if (view?.Planet?.Definition == null)
                return;

            view.Planet.Definition.PlayEffect(
                view.Planet.Definition.clickEffectPrefab,
                view.transform.position,
                planetsRoot);
        }

        private void PlayPlanetDestroyEffect(PlanetView view)
        {
            if (view?.Planet?.Definition == null)
                return;

            _controller.SfxManager?.PlayPlanetDeath();
            view.Planet.Definition.PlayEffect(
                view.Planet.Definition.destroyEffectPrefab,
                view.transform.position,
                planetsRoot);
        }

        private int FindOpenOrbitSlot(int hostStarId)
        {
            var used = new HashSet<int>(_planets.Where(p => p.IsAlive && p.HostStarId == hostStarId).Select(p => p.OrbitSlot));
            for (var slot = 0; slot < GetMaxPlanets(); slot++)
            {
                if (!used.Contains(slot))
                    return slot;
            }

            return -1;
        }

        private float CalculateOrbitRadius(int hostStarId, int slot, PlanetTypeDefinition definition)
        {
            var newVisualRadius = GetPlanetVisualRadius(definition);
            var radius = Balance.baseOrbitRadius + newVisualRadius;

            foreach (var planet in _planets
                         .Where(p => p.IsAlive && p.HostStarId == hostStarId && p.OrbitSlot < slot)
                         .OrderBy(p => p.OrbitRadius))
            {
                var innerVisualRadius = GetPlanetVisualRadius(planet.Definition);
                var requiredRadius = planet.OrbitRadius +
                                     innerVisualRadius +
                                     newVisualRadius +
                                     Balance.orbitRadiusStep;
                radius = Mathf.Max(radius, requiredRadius);
            }

            return radius;
        }

        private static float GetPlanetVisualRadius(PlanetTypeDefinition definition)
        {
            if (definition == null)
                return 0.5f;

            return Mathf.Max(0.05f, definition.visualScale * 0.5f);
        }

        public float GetSpaceStationOrbitRadius(Planet planet)
        {
            if (Balance == null || planet == null)
                return 0.75f;

            var civ = Balance.civilization;
            var planetRadius = GetPlanetVisualRadius(planet.Definition);
            if (_views.TryGetValue(planet.Id, out var view) && view != null)
            {
                var renderer = view.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    var extents = renderer.bounds.extents;
                    planetRadius = Mathf.Max(planetRadius, extents.x, extents.y);
                }
            }

            return civ.spaceStationOrbitRadius + planetRadius * civ.spaceStationOrbitRadiusPlanetMultiplier;
        }

        private bool RollHabitable(PlanetTypeDefinition definition, StarView hostStar)
        {
            if (definition == null || definition.habitability <= 0f || hostStar == null)
                return false;

            var bonus = Balance.baseHabitableRollBonus +
                        _controller.Upgrades.HabitablePlanetChanceLevel *
                        Balance.habitableChancePerLevel;

            var chance = definition.habitability + bonus + Balance.planets.GetHabitabilityBonus(hostStar.Stage);
            return UnityEngine.Random.value < Mathf.Clamp01(chance);
        }

        private void TickAutoFormation(float deltaTime)
        {
            _autoFormationTimer += deltaTime;
            if (_autoFormationTimer < Balance.autoFormationCheckInterval)
                return;

            _autoFormationTimer = 0f;

            var chance = Balance.baseAutoFormationChance +
                         _controller.Upgrades.AutoPlanetFormationLevel *
                         Balance.autoFormationChancePerLevel;

            var createdAny = false;
            foreach (var hostStar in GetStarsWithOpenPlanetSlots())
            {
                if (UnityEngine.Random.value < chance &&
                    TryCreatePlanetForStar(hostStar, free: true, playCreationSound: false))
                {
                    createdAny = true;
                }
            }

            if (createdAny)
                _controller.SfxManager?.PlayPlanetCreated();
        }

        private List<StarView> GetStarsWithOpenPlanetSlots()
        {
            var candidates = new List<StarView>();
            if (_controller == null)
                return candidates;

            var maxPlanets = GetMaxPlanets();
            foreach (var star in _controller.Stars)
            {
                if (star == null || !star.IsInteractable)
                    continue;
                if (GetPlanetCountForStar(star.StarId) < maxPlanets)
                    candidates.Add(star);
            }

            return candidates;
        }

        private void TickCivilization(float deltaTime)
        {
            _civilizationTimer += deltaTime;
            if (_civilizationTimer < Balance.civilizationTickInterval)
                return;

            _civilizationTimer = 0f;

            foreach (var planet in _planets.Where(p => p.IsAlive && p.IsHabitable))
            {
                var definition = planet.Definition;
                if (definition == null || !definition.canCivilize)
                    continue;

                var hostStar = GetHostStar(planet);
                if (hostStar == null)
                    continue;

                var durabilityFactor = planet.Durability / planet.MaxDurability;
                var stageFactor = Balance.planets.GetCivilizationSpeedMultiplier(hostStar.Stage);
                var speciesFactor = planet.HasSpecies
                    ? Balance.species.GetCivilizationSpeedMultiplier(planet.Intelligence, planet.Aggression)
                    : 1f;

                var progress = Balance.lifeProgressBase *
                               definition.habitability *
                               durabilityFactor *
                               stageFactor *
                               speciesFactor *
                               GetIntelligenceProgressFactor(planet) *
                               _controller.GetEffectiveAgeGainMultiplier();

                if (planet.CivilizationStage == CivilizationStage.SpacePhase)
                    progress *= _controller.GetSpaceAgeProgressionMultiplier();

                if (planet.CivilizationStage == CivilizationStage.NoLife)
                {
                    if (UnityEngine.Random.value < Mathf.Clamp01(
                            progress * PrestigeModifiers.GetLifeEmergenceChanceMultiplier(_controller.Prestige)))
                    {
                        EnsureLifeIdentity(planet);
                        planet.ForceLifeStage(CivilizationStage.PrimitiveLife);
                        RecordLifeIfNeeded(planet);

                        NotifyCivilizationAdvanced(planet, CivilizationStage.PrimitiveLife);
                    }
                }
                else if (planet.TryAddCivilizationProgress(
                             progress * PrestigeModifiers.GetCivilizationProgressMultiplier(_controller.Prestige),
                             Balance.civilization,
                             nextStage => CanPlanetEnterCivilizationStage(planet, nextStage),
                             out var advancedTo))
                {
                    NotifyCivilizationAdvanced(planet, advancedTo);
                }

                TickAggressionRisk(planet);
                TickHardSpaceCompletion(planet);

                var rank = (int)planet.CivilizationStage;
                if (rank > HighestCivilizationRank)
                {
                    HighestCivilizationRank = rank;
                    _controller.RunStats.RecordHighestCivilization(
                        planet.CivilizationStage,
                        planet.SpeciesName);
                }

                if (_views.TryGetValue(planet.Id, out var view))
                    view.RefreshVisual();
            }
        }

        private void RecordLifeIfNeeded(Planet planet)
        {
            if (planet == null || planet.CivilizationStage < CivilizationStage.PrimitiveLife ||
                planet.LifeCountedForStats || _controller == null)
                return;

            planet.LifeCountedForStats = true;
            _controller.RunStats.RecordLifePlanet();
            _controller.NotifyStateChanged();
        }

        private void NotifyCivilizationAdvanced(Planet planet, CivilizationStage stage)
        {
            if (stage == CivilizationStage.PrimitiveLife)
                _controller?.SfxManager?.PlayLifeEmerged();

            if (stage == CivilizationStage.SpacePhase && planet != null)
            {
                planet.AssignSpaceProgramName(SpeciesNaming.GenerateSpaceProgramName(planet.SpeciesName));
                OnCivilizationEvent?.Invoke(
                    $"Space Age Reached: {planet.SpeciesName} launched {planet.SpaceProgramName} from {GetPlanetDisplayName(planet)}.");
                _controller?.TryEnterPhase3FromSpecies(planet);
            }
            else if (stage == CivilizationStage.HardSpace && planet != null)
            {
                // Destroy Protocol is Phase 2 only. Phase 3 Hard Space uses the ascension win path.
                if (_controller != null && !GameplayFeatures.UsesUniverseCollapse(_controller.GameplayMode))
                {
                    OnCivilizationEvent?.Invoke(
                        $"Destroy Protocol: {planet.SpeciesName} launched annihilation rockets from {GetPlanetDisplayName(planet)}.");
                    StartDestroyProtocol(planet);
                }
                else
                {
                    OnCivilizationEvent?.Invoke(
                        $"Hard Space Reached: {planet.SpeciesName} begins final ascension from {GetPlanetDisplayName(planet)}.");
                }
            }

            OnCivilizationAdvanced?.Invoke(planet, stage);
        }

        private bool CanPlanetEnterCivilizationStage(Planet planet, CivilizationStage stage)
        {
            if (stage != CivilizationStage.HardSpace || _controller == null || Balance?.phase3 == null)
                return true;

            return _controller.CanSpeciesEnterHardSpace(planet);
        }

        private void TickHardSpaceCompletion(Planet planet)
        {
            if (_destroyProtocolStarted || planet == null || _controller == null || Balance?.phase3 == null ||
                planet.CivilizationStage != CivilizationStage.HardSpace)
                return;

            // Before Phase 3, Hard Space always ends via the destroy protocol (game over).
            if (!GameplayFeatures.UsesUniverseCollapse(_controller.GameplayMode))
                return;

            // Absolute progress per civilization tick toward the Phase 3 ascension win.
            var progress = Balance.phase3.hardSpaceProgressPerCivilizationTick *
                           GetIntelligenceProgressFactor(planet) *
                           Mathf.Max(0.25f, _controller.GetSpaceAgeProgressionMultiplier());
            if (planet.AddHardSpaceCompletionProgress(progress,
                    Balance.phase3.hardSpaceCompletionRequirement))
            {
                _controller.CompleteHardSpaceWin(planet);
            }
        }

        private static float GetIntelligenceProgressFactor(Planet planet)
        {
            if (planet == null || !planet.HasSpecies)
                return 1f;

            return Mathf.Lerp(0.7f, 1.7f, Mathf.Clamp01(planet.Intelligence / 100f));
        }

        private void EnsureLifeIdentity(Planet planet)
        {
            if (planet == null || planet.HasSpecies)
                return;

            var speciesBalance = Balance.species;
            var planetName = SpeciesNaming.GeneratePlanetName(planet.Id);
            var speciesName = SpeciesNaming.GenerateSpeciesName();
            planet.AssignLifeIdentity(
                planetName,
                speciesName,
                SpeciesNaming.GenerateFlavor(planetName, speciesName),
                SpeciesNaming.GenerateCivilizationName(speciesName),
                speciesBalance.RollTrait(),
                speciesBalance.RollTrait());
        }

        private void TickAggressionRisk(Planet planet)
        {
            if (planet == null || !planet.HasSpecies ||
                planet.CivilizationStage < Balance.species.selfDamageMinimumStage)
                return;

            var damageChance = Balance.species.GetSelfDamageChance(planet.Aggression);
            if (damageChance > 0f && UnityEngine.Random.value < damageChance)
            {
                planet.Damage(Balance.species.aggressionSelfDamageAmount);
                OnCivilizationEvent?.Invoke(
                    $"{planet.SpeciesName} conflict damaged {GetPlanetDisplayName(planet)}.");

                if (!planet.IsAlive)
                {
                    if (_views.TryGetValue(planet.Id, out var view) && view != null)
                        PlayPlanetDestroyEffect(view);

                    DestroyPlanet(planet.Id, causeStarDamage: true, grantDna: true);
                    return;
                }
            }

            if (planet.CivilizationStage < Balance.species.selfDestructionMinimumStage)
                return;

            var selfDestructionChance = Balance.species.GetSelfDestructionChance(planet.Aggression) *
                                        Balance.civilization.GetSelfDestructionRiskMultiplier(
                                            planet.CivilizationStage);
            if (selfDestructionChance <= 0f || UnityEngine.Random.value >= selfDestructionChance)
                return;

            planet.ForceLifeStage(Balance.species.selfDestructionRegressToStage);
            OnCivilizationEvent?.Invoke($"{planet.SpeciesName} civilization collapsed on {GetPlanetDisplayName(planet)}.");
        }

        private void TickDnaGeneration(float deltaTime)
        {
            _dnaTimer += deltaTime;
            if (_dnaTimer < Balance.dnaTickInterval)
                return;

            _dnaTimer = 0f;

            foreach (var planet in _planets.Where(p => p.IsAlive))
            {
                var definition = planet.Definition;
                if (definition == null)
                    continue;

                var civMult = CivilizationUtility.GetDnaMultiplier(
                    planet.CivilizationStage,
                    Balance.civilization);
                if (civMult <= 0f && definition.dnaChance <= 0f)
                    continue;

                var chance = (definition.dnaChance + civMult * Balance.planets.civilizationDnaChanceMultiplier) *
                             (1f + _controller.Upgrades.PlanetDnaChanceLevel *
                              Balance.planetDnaChancePerLevel);

                if (planet.HasLife)
                    chance *= Balance.planets.lifeDnaChanceMultiplier;

                if (UnityEngine.Random.value < chance)
                {
                    var speciesMultiplier = planet.HasSpecies
                        ? Balance.species.GetDnaPotentialMultiplier(planet.Intelligence, planet.Aggression)
                        : 1f;
                    var amount = (Balance.planets.baseDnaPotentialAmount + civMult) *
                                 speciesMultiplier *
                                 _controller.GetAdvancedSpeciesDnaMultiplier(planet.CivilizationStage);
                    var position = GetPlanetWorldPosition(planet);
                    _controller.TrySpawnDnaPotential(position, amount);
                }

                var speciesDnaAmount = _controller.GetSpeciesDnaPotentialPerTick(planet);
                if (speciesDnaAmount > 0f)
                    _controller.TrySpawnDnaPotential(GetPlanetWorldPosition(planet), speciesDnaAmount);
            }
        }

        private void TickSpaceTravel(float deltaTime)
        {
            if (Balance == null || _controller == null ||
                _activeSpaceships >= Balance.civilization.maxActiveSpaceships)
                return;

            _spaceTravelTimer += deltaTime;
            if (_spaceTravelTimer < Balance.civilization.spaceshipTripInterval)
                return;

            _spaceTravelTimer = 0f;
            TryLaunchSpaceshipTrip();
        }

        private void TryLaunchSpaceshipTrip()
        {
            var sources = _planets
                .Where(p => p.IsAlive && p.HasSpecies &&
                            p.CivilizationStage >= Balance.civilization.spaceshipMinimumStage)
                .ToList();
            if (sources.Count == 0)
                return;

            var destinations = _planets.Where(p => p.IsAlive).ToList();
            if (destinations.Count < 2)
                return;

            var source = sources[UnityEngine.Random.Range(0, sources.Count)];
            var validDestinations = destinations.Where(p => p.Id != source.Id).ToList();
            if (validDestinations.Count == 0)
                return;

            var destination = validDestinations[UnityEngine.Random.Range(0, validDestinations.Count)];
            var start = GetPlanetWorldPosition(source);
            var end = GetPlanetWorldPosition(destination);

            var go = Balance.civilization.spaceshipPrefab != null
                ? Instantiate(Balance.civilization.spaceshipPrefab, planetsRoot)
                : new GameObject("CivilizationSpaceship");
            go.transform.SetParent(planetsRoot, true);
            var ship = go.GetComponent<SpaceshipView>() ?? go.AddComponent<SpaceshipView>();
            _spaceships.Add(go);
            _activeSpaceships++;
            ship.Initialize(
                source,
                Balance.civilization.spaceshipSprites,
                Balance.civilization.spaceshipAlienPortraitPrefab,
                start,
                end,
                () => GetPlanetWorldPosition(destination),
                Balance.civilization.spaceshipSpeed,
                Balance.civilization.spaceshipPrefab != null,
                arrivalPosition =>
                {
                    _spaceships.Remove(go);
                    _activeSpaceships = Mathf.Max(0, _activeSpaceships - 1);
                    _controller.TrySpawnDnaPotential(
                        arrivalPosition,
                        Balance.civilization.spaceshipDnaPotentialPerTrip * _controller.GetOrbitalDnaMultiplier());
                    TryColonizeOnArrival(source, destination);
                });
        }

        private void TickSpaceStations(float deltaTime)
        {
            if (Balance == null || _controller == null)
                return;

            var civ = Balance.civilization;
            if (civ == null || civ.spaceStationPrefab == null || civ.maxActiveSpaceStations <= 0)
                return;

            _spaceStations.RemoveAll(station => station == null);
            if (_spaceStations.Count >= civ.maxActiveSpaceStations)
                return;

            _spaceStationTimer += deltaTime;
            if (_spaceStationTimer < civ.spaceStationSpawnInterval)
                return;

            _spaceStationTimer = 0f;
            TrySpawnSpaceStation();
        }

        private void TrySpawnSpaceStation()
        {
            if (!TryPickSpaceStationLaunch(out var source, out var destination))
                return;

            var civ = Balance.civilization;
            var sprites = civ.spaceStationSprites != null && civ.spaceStationSprites.Length > 0
                ? civ.spaceStationSprites
                : civ.spaceshipSprites;

            var go = Instantiate(civ.spaceStationPrefab, planetsRoot);
            go.transform.SetParent(planetsRoot, true);
            var station = go.GetComponent<SpaceStationView>() ?? go.AddComponent<SpaceStationView>();
            _spaceStations.Add(station);
            station.Initialize(
                this,
                _controller,
                source,
                destination,
                civ,
                sprites,
                civ.spaceshipAlienPortraitPrefab,
                civ.spaceStationPrefab != null);
        }

        public bool TryPickSpaceStationLaunch(out Planet source, out Planet destination)
        {
            source = PickSpaceStationPlanet();
            destination = null;
            if (source == null)
                return false;

            var sourceId = source.Id;
            var destinations = _planets.Where(p => p.IsAlive && p.Id != sourceId).ToList();
            if (destinations.Count == 0)
                return false;

            destination = destinations[UnityEngine.Random.Range(0, destinations.Count)];
            return true;
        }

        public Planet PickSpaceStationDestinationPlanet(int excludePlanetId = -1)
        {
            var destinations = _planets.Where(p => p.IsAlive && p.Id != excludePlanetId).ToList();
            if (destinations.Count == 0)
                return null;

            return destinations[UnityEngine.Random.Range(0, destinations.Count)];
        }

        public Planet PickSpaceStationPlanet(int excludePlanetId = -1) =>
            PickSpaceStationPlanetAtStage(excludePlanetId, Balance.civilization.spaceStationMinimumStage);

        private Planet PickSpaceStationPlanetAtStage(int excludePlanetId, CivilizationStage minimumStage)
        {
            var candidates = _planets
                .Where(p => p.IsAlive && p.Id != excludePlanetId && p.HasSpecies &&
                            p.CivilizationStage >= minimumStage)
                .ToList();
            if (candidates.Count == 0)
                return null;

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        public void UnregisterSpaceStation(SpaceStationView station) => _spaceStations.Remove(station);

        public Planet GetPlanetById(int planetId) => _planets.FirstOrDefault(p => p.Id == planetId);

        public void DestroyPlanetById(int planetId, bool grantDna = true) =>
            DestroyPlanet(planetId, recordDestruction: true, causeStarDamage: true, grantDna: grantDna);

        private void StartDestroyProtocol(Planet sourcePlanet)
        {
            if (_destroyProtocolStarted || _controller == null || Balance?.civilization == null)
                return;

            if (GameplayFeatures.UsesUniverseCollapse(_controller.GameplayMode))
                return;

            var civ = Balance.civilization;
            if (!civ.destroyProtocolEnabled)
                return;

            _destroyProtocolStarted = true;
            EnsureDestroyRocketPool();
            StartCoroutine(DestroyProtocolRoutine(sourcePlanet));
        }

        private void EnsureDestroyRocketPool()
        {
            if (_destroyRocketPool != null)
                return;

            _destroyRocketPool = FindAnyObjectByType<DestroyRocketPool>(FindObjectsInactive.Include);
        }

        private IEnumerator DestroyProtocolRoutine(Planet sourcePlanet)
        {
            var civ = Balance.civilization;
            var origin = sourcePlanet != null
                ? GetPlanetWorldPosition(sourcePlanet)
                : Vector3.zero;
            var speciesName = sourcePlanet != null ? sourcePlanet.SpeciesName : "Unknown";
            var targets = BuildDestroyProtocolTargets(sourcePlanet);

            if (targets.Count == 0 || _destroyRocketPool == null || _destroyRocketPool.AvailableCount <= 0)
            {
                WipeAllForDestroyProtocol();
                var fallbackDelay = Mathf.Max(0.2f, civ.destroyProtocolCollapseDelay);
                yield return new WaitForSeconds(fallbackDelay);
                _controller?.BeginDestroyProtocolCollapse(speciesName);
                yield break;
            }

            var pending = 0;
            var launchIndex = 0;
            while (launchIndex < targets.Count)
            {
                while (launchIndex < targets.Count && _destroyRocketPool.AvailableCount > 0)
                {
                    var target = targets[launchIndex++];
                    pending++;
                    LaunchDestroyRocket(
                        origin,
                        target.GetPosition(),
                        target.GetPosition,
                        _ =>
                        {
                            target.OnHit?.Invoke();
                            pending = Mathf.Max(0, pending - 1);
                        });
                }

                yield return null;
            }

            var timeout = 12f;
            while (pending > 0 && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            WipeAllForDestroyProtocol();

            var delay = Mathf.Max(0.15f, civ.destroyProtocolCollapseDelay);
            yield return new WaitForSeconds(delay);

            _controller?.BeginDestroyProtocolCollapse(speciesName);
        }

        private List<DestroyRocketTarget> BuildDestroyProtocolTargets(Planet sourcePlanet)
        {
            var targets = new List<DestroyRocketTarget>();
            var sourceId = sourcePlanet?.Id ?? -1;

            foreach (var planet in _planets.Where(p => p.IsAlive).ToList())
            {
                var planetId = planet.Id;
                targets.Add(new DestroyRocketTarget(
                    () =>
                    {
                        var live = GetPlanetById(planetId);
                        return live != null && live.IsAlive
                            ? GetPlanetWorldPosition(live)
                            : Vector3.zero;
                    },
                    () =>
                    {
                        if (planetId == sourceId)
                            return;
                        if (_views.TryGetValue(planetId, out var view) && view != null)
                            PlayPlanetDestroyEffect(view);
                        DestroyPlanet(planetId, recordDestruction: true, causeStarDamage: false, grantDna: false);
                    }));
            }

            if (_controller != null)
            {
                foreach (var star in _controller.Stars.Where(s => s != null).ToList())
                {
                    var targetStar = star;
                    targets.Add(new DestroyRocketTarget(
                        () => targetStar != null ? targetStar.transform.position : Vector3.zero,
                        () =>
                        {
                            if (targetStar != null)
                                _controller.DestroyStarFromDestroyProtocol(targetStar);
                        }));
                }
            }

            return targets;
        }

        private void WipeAllForDestroyProtocol()
        {
            foreach (var planet in _planets.Where(p => p.IsAlive).Select(p => p.Id).ToList())
                DestroyPlanet(planet, recordDestruction: true, causeStarDamage: false, grantDna: false);

            if (_controller == null)
                return;

            foreach (var star in _controller.Stars.Where(s => s != null).ToList())
                _controller.DestroyStarFromDestroyProtocol(star);
        }

        private void LaunchDestroyRocket(Vector3 start, Vector3 end, Func<Vector3> getEndPosition,
            Action<Vector3> onArrived)
        {
            EnsureDestroyRocketPool();
            var rocket = _destroyRocketPool != null ? _destroyRocketPool.Rent() : null;
            if (rocket == null)
            {
                onArrived?.Invoke(end);
                return;
            }

            rocket.Launch(start, end, getEndPosition, Balance.civilization.destroyRocketSpeed, onArrived);
        }

        private readonly struct DestroyRocketTarget
        {
            public readonly Func<Vector3> GetPosition;
            public readonly Action OnHit;

            public DestroyRocketTarget(Func<Vector3> getPosition, Action onHit)
            {
                GetPosition = getPosition;
                OnHit = onHit;
            }
        }

        private void TryColonizeOnArrival(Planet source, Planet destination)
        {
            var balance = Balance.civilization;
            if (balance == null || !balance.colonizationEnabled || source == null || destination == null ||
                !destination.IsAlive)
                return;

            if (source.CivilizationStage < balance.colonizationMinimumSourceStage)
                return;

            if (destination.HasSpecies)
                return;

            if (balance.colonizationRequiresHabitable && !destination.IsHabitable)
                return;

            if (destination.Definition == null || !destination.Definition.canCivilize)
                return;

            if (UnityEngine.Random.value >= balance.GetColonizationChance(source))
                return;

            destination.InheritSpeciesFrom(source);
            destination.ForceLifeStage(balance.colonizationTargetStage);
            RecordLifeIfNeeded(destination);

            if (_views.TryGetValue(destination.Id, out var view) && view != null)
                view.RefreshVisual();

            OnCivilizationEvent?.Invoke(
                $"{source.SpeciesName} colonized {GetPlanetDisplayName(destination)} from {GetPlanetDisplayName(source)}.");
            OnPlanetsChanged?.Invoke();
            _controller?.NotifyStateChanged();
            _controller?.SfxManager?.PlayLifeEmerged();
        }

        public string GetPlanetDisplayName(Planet planet)
        {
            if (planet == null)
                return "Planet";

            return !string.IsNullOrWhiteSpace(planet.PlanetName)
                ? planet.PlanetName
                : PlanetTypeUtility.GetLabel(planet.Definition);
        }

        public float GetPassiveStardustForStar(int starId)
        {
            if (Balance == null || _controller == null)
                return 0f;

            var planetCount = _planets.Count(p => p.IsAlive && p.HostStarId == starId);
            if (planetCount <= 0)
                return 0f;

            var percentPerLevel = Balance.multiStar.planetPassiveProductionPercentPerLevel;
            var percentMultiplier = Mathf.Pow(
                1f + percentPerLevel,
                _controller.Upgrades.PlanetPassiveProductionLevel);
            var perPlanet = Balance.multiStar.basePlanetPassiveStardust * percentMultiplier;
            return Mathf.Max(0f, planetCount * perPlanet);
        }

        public float GetStarAgeBurdenMultiplier(int starId)
        {
            if (Balance == null)
                return 1f;

            var overloadCount = Mathf.Max(0,
                _planets.Count(p => p.IsAlive && p.HostStarId == starId) -
                Balance.multiStar.planetCountBeforeOverload);
            return 1f + overloadCount * Balance.multiStar.planetOverloadAgeGainPerPlanet;
        }

        public Vector3 GetPlanetWorldPosition(Planet planet)
        {
            if (_views.TryGetValue(planet.Id, out var view) && view != null)
                return view.transform.position;

            var hostStar = GetHostStar(planet);
            if (hostStar == null)
                return Vector3.zero;

            return hostStar.transform.position + GetOrbitOffset(planet);
        }

        public Vector3 GetPlanetWorldPosition(int planetId)
        {
            var planet = GetPlanetById(planetId);
            return planet == null ? Vector3.zero : GetPlanetWorldPosition(planet);
        }

        private StarView GetHostStar(Planet planet) =>
            planet != null && _controller != null ? _controller.GetStarById(planet.HostStarId) : null;

        public void ApplySupernovaToStar(StarView star, float damageAmount, float destroyChance)
        {
            if (star == null)
                return;

            foreach (var planet in _planets.Where(p => p.IsAlive && p.HostStarId == star.StarId).ToList())
            {
                if (UnityEngine.Random.value < destroyChance)
                    planet.Damage(planet.MaxDurability);
                else
                    planet.Damage(damageAmount);

                if (!planet.IsAlive)
                {
                    if (_views.TryGetValue(planet.Id, out var destroyedView) && destroyedView != null)
                        PlayPlanetDestroyEffect(destroyedView);
                    DestroyPlanet(planet.Id, causeStarDamage: false, grantDna: true);
                }
                else if (_views.TryGetValue(planet.Id, out var damagedView) && damagedView != null)
                {
                    damagedView.RefreshVisual();
                }
            }
        }

        public void DestroyPlanetsForStar(StarView star)
        {
            if (star == null)
                return;

            foreach (var planet in _planets.Where(p => p.IsAlive && p.HostStarId == star.StarId).ToList())
            {
                if (_views.TryGetValue(planet.Id, out var view) && view != null)
                    PlayPlanetDestroyEffect(view);

                DestroyPlanet(planet.Id, causeStarDamage: false, grantDna: true);
            }
        }

        public void PullPlanetsTowardBlackHole(Vector3 holePosition, float pullRadius, float pullSpeed, float deltaTime)
        {
            if (pullRadius <= 0f || pullSpeed <= 0f || deltaTime <= 0f)
                return;

            foreach (var planet in _planets.Where(p => p.IsAlive).ToList())
            {
                if (!_views.TryGetValue(planet.Id, out var view) || view == null)
                    continue;

                view.PullToward(holePosition, pullRadius, pullSpeed, deltaTime);
            }
        }

        public int ConsumePlanetsByBlackHole(Vector3 holePosition, float consumeRadius, float dnaPotentialReward)
        {
            if (consumeRadius <= 0f)
                return 0;

            var consumed = 0;
            foreach (var planet in _planets.Where(p => p.IsAlive).ToList())
            {
                var position = GetPlanetWorldPosition(planet);
                if (Vector3.Distance(position, holePosition) > consumeRadius)
                    continue;

                if (_views.TryGetValue(planet.Id, out var view) && view != null)
                    PlayPlanetDestroyEffect(view);

                DestroyPlanet(planet.Id, causeStarDamage: false, grantDna: true);
                _controller?.TrySpawnDnaPotential(position, dnaPotentialReward);
                consumed++;
            }

            if (consumed > 0)
            {
                OnPlanetsChanged?.Invoke();
                _controller?.NotifyStateChanged();
            }

            return consumed;
        }

        public int DamagePlanetsInRadius(Vector3 origin, float radius, float damage, bool destroyUnstable,
            bool grantDna, string eventMessage = null)
        {
            if (radius <= 0f || damage <= 0f)
                return 0;

            var affected = 0;
            foreach (var planet in _planets.Where(p => p.IsAlive).ToList())
            {
                var position = GetPlanetWorldPosition(planet);
                if (Vector3.Distance(position, origin) > radius)
                    continue;

                affected++;
                var lethal = destroyUnstable && planet.Durability <= damage;
                planet.Damage(lethal ? planet.MaxDurability : damage);

                if (!planet.IsAlive)
                {
                    if (_views.TryGetValue(planet.Id, out var destroyedView) && destroyedView != null)
                        PlayPlanetDestroyEffect(destroyedView);

                    DestroyPlanet(planet.Id, causeStarDamage: false, grantDna: grantDna);
                }
                else if (_views.TryGetValue(planet.Id, out var damagedView) && damagedView != null)
                {
                    damagedView.RefreshVisual();
                }
            }

            if (affected > 0 && !string.IsNullOrWhiteSpace(eventMessage))
                OnCivilizationEvent?.Invoke(eventMessage);

            return affected;
        }

        private void TickPlanetCollisions()
        {
            if (Balance == null || Balance.multiStar.planetCollisionDistance <= 0f)
                return;

            var aliveWithViews = _planets
                .Where(p => p.IsAlive && _views.ContainsKey(p.Id))
                .ToList();

            for (var i = 0; i < aliveWithViews.Count; i++)
            {
                for (var j = i + 1; j < aliveWithViews.Count; j++)
                {
                    var a = aliveWithViews[i];
                    var b = aliveWithViews[j];
                    if (a.HostStarId == b.HostStarId)
                        continue;

                    if (!_views.TryGetValue(a.Id, out var viewA) || viewA == null ||
                        !_views.TryGetValue(b.Id, out var viewB) || viewB == null)
                        continue;

                    if (Vector3.Distance(viewA.transform.position, viewB.transform.position) >
                        Balance.multiStar.planetCollisionDistance)
                        continue;

                    _controller.SfxManager?.PlayPlanetCrossStarCollision();
                    PlayPlanetCrossStarCollisionDestroyEffect(viewA);
                    PlayPlanetCrossStarCollisionDestroyEffect(viewB);

                    var midpoint = (viewA.transform.position + viewB.transform.position) * 0.5f;
                    _controller.TrySpawnDnaPotential(midpoint,
                        _controller.GetCollisionDnaPotential(Balance.multiStar.planetCollisionDnaPotential));
                    _controller.AddEntropy(Balance.multiStar.planetCollisionEntropy);
                    OnCivilizationEvent?.Invoke("Cross-star planet collision! Both planets were destroyed and released DNA.");

                    DestroyPlanet(a.Id, causeStarDamage: true, grantDna: false);
                    DestroyPlanet(b.Id, causeStarDamage: true, grantDna: false);
                    return;
                }
            }
        }

        private void DestroyPlanet(int id, bool recordDestruction = true, bool causeStarDamage = false,
            bool grantDna = false)
        {
            var planet = _planets.FirstOrDefault(p => p.Id == id);
            if (planet == null)
                return;

            var position = GetPlanetWorldPosition(planet);
            var hostStar = GetHostStar(planet);
            planet.Damage(planet.MaxDurability);

            if (_views.TryGetValue(id, out var view) && view != null)
                Destroy(view.gameObject);

            _views.Remove(id);
            _planets.Remove(planet);

            if (recordDestruction)
                _controller.RunStats.RecordPlanetDestroyed();

            if (grantDna && Balance != null && Balance.multiStar.planetDeathDnaPotential > 0f)
                _controller.TrySpawnDnaPotential(position, Balance.multiStar.planetDeathDnaPotential);

            if (recordDestruction)
            {
                var prestigeDna = PrestigeModifiers.GetPlanetDestructionDnaPotential(_controller.Prestige);
                if (prestigeDna > 0f)
                    _controller.TrySpawnDnaPotential(position, prestigeDna);

                var prestigeStardust = PrestigeModifiers.GetPlanetExplosionStardust(_controller.Prestige);
                if (prestigeStardust > 0f)
                    _controller.CreditStardustDirect(prestigeStardust);
            }

            if (recordDestruction && Balance != null && Balance.multiStar.planetDeathEntropy > 0f)
                _controller.AddEntropy(Balance.multiStar.planetDeathEntropy);

            if (causeStarDamage && hostStar != null && Balance != null &&
                Balance.multiStar.planetDeathStarAgeDamage > 0f)
            {
                hostStar.AddAge(Balance.multiStar.planetDeathStarAgeDamage, 1f);
                hostStar.RefreshVisual();
                if (hostStar.HasReachedMaxAge)
                    _controller.QueueSupernova(hostStar);
            }

            OnPlanetsChanged?.Invoke();
        }

        private void PlayPlanetCrossStarCollisionDestroyEffect(PlanetView view)
        {
            if (view?.Planet?.Definition == null)
                return;

            var definition = view.Planet.Definition;
            var prefab = definition.crossStarCollisionDestroyEffectPrefab != null
                ? definition.crossStarCollisionDestroyEffectPrefab
                : definition.destroyEffectPrefab;

            definition.PlayEffect(prefab, view.transform.position, planetsRoot);
        }
    }
}
