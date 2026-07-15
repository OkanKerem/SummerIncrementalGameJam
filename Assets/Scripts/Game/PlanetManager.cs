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
        private int _nextPlanetId = 1;
        private float _autoFormationTimer;
        private float _civilizationTimer;
        private float _dnaTimer;
        private float _spaceTravelTimer;
        private int _activeSpaceships;

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

            _planets.Clear();
            _views.Clear();
            _spaceships.Clear();
            _nextPlanetId = 1;
            _autoFormationTimer = 0f;
            _civilizationTimer = 0f;
            _dnaTimer = 0f;
            _spaceTravelTimer = 0f;
            _activeSpaceships = 0;
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

        public bool TryCreatePlanet(bool free = false)
        {
            if (_controller == null || _hostStar == null || !_hostStar.IsInteractable)
                return false;

            if (!HasOpenSlot())
                return false;

            if (!free)
            {
                var cost = GetCreatePlanetCost();
                if (_controller.Stardust < cost)
                    return false;

                _controller.SpendStardust(cost);
            }

            CreateRandomPlanet();
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

        private void CreateRandomPlanet() => StartCoroutine(CreateRandomPlanetRoutine());

        private IEnumerator CreateRandomPlanetRoutine()
        {
            var hostStar = _hostStar;
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

            if (habitable && planet.IsHabitable &&
                UnityEngine.Random.value < Mathf.Clamp01(
                    Balance.lifeProgressBase * Balance.planets.initialLifeChanceMultiplier *
                    PrestigeModifiers.GetLifeEmergenceChanceMultiplier(_controller.Prestige)))
            {
                EnsureLifeIdentity(planet);
                planet.ForceLifeStage(CivilizationStage.PrimitiveLife);
            }

            OnPlanetsChanged?.Invoke();
            _controller.NotifyStateChanged();

            PlayPlanetSpawnEffect(planet);

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

        private void PlayPlanetSpawnEffect(Planet planet)
        {
            if (planet?.Definition == null)
                return;

            var hostStar = GetHostStar(planet);
            if (hostStar == null)
                return;

            var position = hostStar.transform.position + GetOrbitOffset(planet);
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
            if (!HasOpenSlot())
                return;

            var chance = Balance.baseAutoFormationChance +
                         _controller.Upgrades.AutoPlanetFormationLevel *
                         Balance.autoFormationChancePerLevel;

            if (UnityEngine.Random.value < chance)
                TryCreatePlanet(free: true);
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

                if (planet.CivilizationStage == CivilizationStage.NoLife)
                {
                    if (UnityEngine.Random.value < Mathf.Clamp01(
                            progress * PrestigeModifiers.GetLifeEmergenceChanceMultiplier(_controller.Prestige)))
                    {
                        EnsureLifeIdentity(planet);
                        planet.ForceLifeStage(CivilizationStage.PrimitiveLife);
                        if (!planet.LifeCountedForStats)
                        {
                            planet.LifeCountedForStats = true;
                            _controller.RunStats.RecordLifePlanet();
                        }

                        NotifyCivilizationAdvanced(planet, CivilizationStage.PrimitiveLife);
                    }
                }
                else if (planet.TryAddCivilizationProgress(
                             progress * PrestigeModifiers.GetCivilizationProgressMultiplier(_controller.Prestige),
                             Balance.civilization,
                             out var advancedTo))
                {
                    NotifyCivilizationAdvanced(planet, advancedTo);
                }

                TickAggressionRisk(planet);

                var rank = (int)planet.CivilizationStage;
                if (rank > HighestCivilizationRank)
                {
                    HighestCivilizationRank = rank;
                    _controller.RunStats.RecordHighestCivilization(planet.CivilizationStage);
                }

                if (_views.TryGetValue(planet.Id, out var view))
                    view.RefreshVisual();
            }
        }

        private void NotifyCivilizationAdvanced(Planet planet, CivilizationStage stage)
        {
            if (stage == CivilizationStage.PrimitiveLife)
                _controller?.SfxManager?.PlayLifeEmerged();

            OnCivilizationAdvanced?.Invoke(planet, stage);
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
                    var amount = (Balance.planets.baseDnaPotentialAmount + civMult) * speciesMultiplier;
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
                    _controller.TrySpawnDnaPotential(arrivalPosition, Balance.civilization.spaceshipDnaPotentialPerTrip);
                });
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

            var perPlanet = Balance.multiStar.basePlanetPassiveStardust +
                            _controller.Upgrades.PlanetPassiveProductionLevel *
                            Balance.multiStar.planetPassiveStardustPerUpgradeLevel;
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

        private Vector3 GetPlanetWorldPosition(Planet planet)
        {
            if (_views.TryGetValue(planet.Id, out var view) && view != null)
                return view.transform.position;

            var hostStar = GetHostStar(planet);
            if (hostStar == null)
                return Vector3.zero;

            return hostStar.transform.position + GetOrbitOffset(planet);
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
