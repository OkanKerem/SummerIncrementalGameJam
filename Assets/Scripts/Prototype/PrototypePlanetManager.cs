using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Universes.Prototype
{
    public class PrototypePlanetManager : MonoBehaviour
    {
        [SerializeField] private PrototypePlanetView planetPrefab;
        [SerializeField] private PrototypePlanetTypeCatalog planetTypeCatalog;
        [SerializeField] private Transform planetsRoot;
        [SerializeField] private Vector2 orbitEllipseScale = new(1.25f, 0.58f);

        private PrototypeGameController _controller;
        private PrototypeStarView _hostStar;
        private readonly List<PrototypePlanet> _planets = new();
        private readonly Dictionary<int, PrototypePlanetView> _views = new();
        private int _nextPlanetId = 1;
        private float _autoFormationTimer;
        private float _civilizationTimer;
        private float _dnaTimer;

        public IReadOnlyList<PrototypePlanet> Planets => _planets;
        public int PlanetCount => _planets.Count(p => p.IsAlive);
        public int HighestCivilizationRank { get; private set; }

        public bool IsInitialized => _controller != null;

        public event Action OnPlanetsChanged;
        public event Action<PrototypePlanet, PrototypeCivilizationStage> OnCivilizationAdvanced;
        public event Action<string> OnCivilizationEvent;

        private PrototypeSingleStarBalance Balance => _controller?.SingleStarBalance;

        public void Initialize(PrototypeGameController controller, Transform root,
            PrototypePlanetTypeCatalog catalog = null)
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

        public void BindHostStar(PrototypeStarView star) => _hostStar = star;

        public void ResetAll()
        {
            foreach (var view in _views.Values)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }

            _planets.Clear();
            _views.Clear();
            _nextPlanetId = 1;
            _autoFormationTimer = 0f;
            _civilizationTimer = 0f;
            _dnaTimer = 0f;
            HighestCivilizationRank = 0;
            StopAllCoroutines();
        }

        public int GetMaxPlanets() =>
            _controller != null
                ? Balance.baseMaxPlanets + _controller.Upgrades.MaxPlanetCountLevel
                : PrototypePlanetBalance.BaseMaxPlanets;

        public float GetOrbitSpeed() =>
            Balance != null ? Balance.orbitSpeed : PrototypePlanetBalance.OrbitSpeed;

        public Vector3 GetOrbitOffset(PrototypePlanet planet)
        {
            if (planet == null)
                return Vector3.zero;

            var rad = planet.OrbitAngle * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Cos(rad) * planet.OrbitRadius * orbitEllipseScale.x,
                Mathf.Sin(rad) * planet.OrbitRadius * orbitEllipseScale.y,
                0f);
        }

        public double GetCreatePlanetCost() =>
            Balance != null ? Balance.createPlanetCost : PrototypePlanetBalance.CreatePlanetCost;

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
                if (_controller.Stardust < Balance.createPlanetCost)
                    return false;

                _controller.SpendStardust(Balance.createPlanetCost);
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

            TickAutoFormation(deltaTime);
            TickCivilization(deltaTime);
            TickDnaGeneration(deltaTime);
        }

        public void OnPlanetClicked(PrototypePlanetView view)
        {
            if (_controller == null || view?.Planet == null || !view.Planet.IsAlive)
                return;

            var planet = view.Planet;
            var reward = _controller.GetPlanetClickReward(planet);
            var damage = _controller.GetPlanetClickDamage();

            _controller.CreditStardustDirect(reward);
            _controller.RunStats.RecordPlanetClick();
            _controller.FloatingTextSpawner?.Spawn(view.transform.position, reward, view.GetDisplayColor());
            planet.Damage(damage);
            view.RefreshVisual();
            PlayPlanetClickEffect(view);

            if (!planet.IsAlive)
            {
                PlayPlanetDestroyEffect(view);
                DestroyPlanet(planet.Id);
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
                Debug.LogWarning("No planet type definitions available. Assign Planet Type Catalog on PrototypePlanetManager.",
                    this);
                yield break;
            }

            var habitable = RollHabitable(definition, hostStar);
            var orbitRadius = CalculateOrbitRadius(hostStar.StarId, slot, definition);
            var angle = UnityEngine.Random.Range(0f, 360f);

            var planet = new PrototypePlanet(_nextPlanetId++, hostStar.StarId, definition, slot, orbitRadius, angle, habitable,
                Balance.planets.fallbackBaseDurability);
            _planets.Add(planet);

            if (habitable && planet.IsHabitable &&
                UnityEngine.Random.value < Balance.lifeProgressBase * Balance.planets.initialLifeChanceMultiplier)
            {
                EnsureLifeIdentity(planet);
                planet.ForceLifeStage(PrototypeCivilizationStage.Life);
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

        private void SpawnView(PrototypePlanet planet)
        {
            if (planetPrefab == null)
                return;

            var hostStar = GetHostStar(planet);
            if (hostStar == null)
                return;

            var view = Instantiate(planetPrefab, planetsRoot);
            view.name = $"Planet_{PrototypePlanetTypeUtility.GetLabel(planet.Definition)}";
            view.Bind(planet, this, hostStar.transform);
            _views[planet.Id] = view;
        }

        private PrototypePlanetTypeDefinition RollPlanetTypeDefinition()
        {
            if (planetTypeCatalog != null)
            {
                var rolled = planetTypeCatalog.RollRandom();
                if (rolled != null)
                    return rolled;
            }

            return PrototypePlanetTypeUtility.CreateFallbackDefinition(
                PrototypePlanetTypeUtility.RollRandomFallback(Balance.planets),
                Balance.planets);
        }

        private void PlayPlanetSpawnEffect(PrototypePlanet planet)
        {
            if (planet?.Definition == null)
                return;

            var hostStar = GetHostStar(planet);
            if (hostStar == null)
                return;

            var position = hostStar.transform.position + GetOrbitOffset(planet);
            planet.Definition.PlayEffect(planet.Definition.spawnEffectPrefab, position, planetsRoot);
        }

        private void PlayPlanetClickEffect(PrototypePlanetView view)
        {
            if (view?.Planet?.Definition == null)
                return;

            view.Planet.Definition.PlayEffect(
                view.Planet.Definition.clickEffectPrefab,
                view.transform.position,
                planetsRoot);
        }

        private void PlayPlanetDestroyEffect(PrototypePlanetView view)
        {
            if (view?.Planet?.Definition == null)
                return;

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

        private float CalculateOrbitRadius(int hostStarId, int slot, PrototypePlanetTypeDefinition definition)
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

        private static float GetPlanetVisualRadius(PrototypePlanetTypeDefinition definition)
        {
            if (definition == null)
                return 0.5f;

            return Mathf.Max(0.05f, definition.visualScale * 0.5f);
        }

        private bool RollHabitable(PrototypePlanetTypeDefinition definition, PrototypeStarView hostStar)
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
                               _controller.GetEffectiveAgeGainMultiplier();

                if (planet.CivilizationStage == PrototypeCivilizationStage.NoLife)
                {
                    if (UnityEngine.Random.value < progress)
                    {
                        EnsureLifeIdentity(planet);
                        planet.ForceLifeStage(PrototypeCivilizationStage.Life);
                        if (!planet.LifeCountedForStats)
                        {
                            planet.LifeCountedForStats = true;
                            _controller.RunStats.RecordLifePlanet();
                        }

                        NotifyCivilizationAdvanced(planet, PrototypeCivilizationStage.Life);
                    }
                }
                else if (planet.TryAddCivilizationProgress(progress, Balance.civilization, out var advancedTo))
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

        private void NotifyCivilizationAdvanced(PrototypePlanet planet, PrototypeCivilizationStage stage) =>
            OnCivilizationAdvanced?.Invoke(planet, stage);

        private void EnsureLifeIdentity(PrototypePlanet planet)
        {
            if (planet == null || planet.HasSpecies)
                return;

            var speciesBalance = Balance.species;
            var planetName = PrototypeSpeciesNaming.GeneratePlanetName(planet.Id);
            var speciesName = PrototypeSpeciesNaming.GenerateSpeciesName();
            planet.AssignLifeIdentity(
                planetName,
                speciesName,
                PrototypeSpeciesNaming.GenerateFlavor(planetName, speciesName),
                PrototypeSpeciesNaming.GenerateCivilizationName(speciesName),
                speciesBalance.RollTrait(),
                speciesBalance.RollTrait());
        }

        private void TickAggressionRisk(PrototypePlanet planet)
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

                    DestroyPlanet(planet.Id);
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

                var civMult = PrototypeCivilizationUtility.GetDnaMultiplier(
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
            }
        }

        public string GetPlanetDisplayName(PrototypePlanet planet)
        {
            if (planet == null)
                return "Planet";

            return !string.IsNullOrWhiteSpace(planet.PlanetName)
                ? planet.PlanetName
                : PrototypePlanetTypeUtility.GetLabel(planet.Definition);
        }

        private Vector3 GetPlanetWorldPosition(PrototypePlanet planet)
        {
            if (_views.TryGetValue(planet.Id, out var view) && view != null)
                return view.transform.position;

            var hostStar = GetHostStar(planet);
            if (hostStar == null)
                return Vector3.zero;

            return hostStar.transform.position + GetOrbitOffset(planet);
        }

        private PrototypeStarView GetHostStar(PrototypePlanet planet) =>
            planet != null && _controller != null ? _controller.GetStarById(planet.HostStarId) : null;

        public void ApplySupernovaToStar(PrototypeStarView star, float damageAmount, float destroyChance)
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
                    DestroyPlanet(planet.Id);
                }
                else if (_views.TryGetValue(planet.Id, out var damagedView) && damagedView != null)
                {
                    damagedView.RefreshVisual();
                }
            }
        }

        private void DestroyPlanet(int id, bool recordDestruction = true)
        {
            var planet = _planets.FirstOrDefault(p => p.Id == id);
            if (planet == null)
                return;

            planet.Damage(planet.MaxDurability);

            if (_views.TryGetValue(id, out var view) && view != null)
                Destroy(view.gameObject);

            _views.Remove(id);
            _planets.Remove(planet);

            if (recordDestruction)
                _controller.RunStats.RecordPlanetDestroyed();

            OnPlanetsChanged?.Invoke();
        }
    }
}
