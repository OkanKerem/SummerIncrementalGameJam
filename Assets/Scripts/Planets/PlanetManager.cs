using System;
using System.Collections.Generic;
using Universes.Core;
using Universes.Prestige;
using Universes.Presentation;
using Universes.Stars;
using UnityEngine;

namespace Universes.Planets
{
    public class PlanetManager : MonoBehaviour
    {
        [SerializeField] private Transform worldRoot;
        [SerializeField] private PlanetView planetViewPrefab;

        private readonly List<Planet> _planets = new();
        private readonly Dictionary<int, PlanetView> _views = new();
        private GameBalance _balance;
        private RunModifiers _modifiers;
        private StardustWallet _wallet;
        private StarManager _starManager;
        private int _nextId = 1;
        private int _planetsCreated;
        private int _lifePlanets;
        private int _destroyedPlanets;

        public IReadOnlyList<Planet> Planets => _planets;
        public int PlanetsCreated => _planetsCreated;
        public int LifePlanets => _lifePlanets;
        public int DestroyedPlanets => _destroyedPlanets;

        public event Action OnPlanetsChanged;

        public void Initialize(GameBalance balance, RunModifiers modifiers, StardustWallet wallet, StarManager starManager, Transform root)
        {
            _balance = balance;
            _modifiers = modifiers;
            _wallet = wallet;
            _starManager = starManager;
            if (root != null)
                worldRoot = root;
        }

        public void Reset()
        {
            foreach (var view in _views.Values)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }

            _planets.Clear();
            _views.Clear();
            _nextId = 1;
            _planetsCreated = 0;
            _lifePlanets = 0;
            _destroyedPlanets = 0;
        }

        public double GetCreatePlanetCost()
        {
            var cost = _balance.createPlanetBaseCost * (1 - Math.Min(_modifiers.PlanetCostReduction, 0.9));
            return Math.Max(cost, 1);
        }

        public bool CanCreatePlanetOn(Star star)
        {
            if (star == null || !star.CanHostPlanet())
                return false;

            var maxPlanets = _modifiers.GetMaxPlanetsPerStar(_balance);
            return star.PlanetIds.Count < maxPlanets;
        }

        public bool TryCreatePlanet(Star star)
        {
            if (!CanCreatePlanetOn(star))
                return false;

            var cost = GetCreatePlanetCost();
            if (!_wallet.TrySpend(cost))
                return false;

            var orbitIndex = star.PlanetIds.Count;
            var orbitRadius = 0.6f + orbitIndex * 0.35f;
            var orbitAngle = UnityEngine.Random.Range(0f, 360f);

            var planet = new Planet(_nextId++, star.Id, orbitIndex, orbitAngle, orbitRadius);
            planet.OnLifeDeveloped += HandleLifeDeveloped;
            planet.OnDestroyed += HandlePlanetDestroyed;

            _planets.Add(planet);
            star.PlanetIds.Add(planet.Id);
            _planetsCreated++;

            if (planetViewPrefab != null && worldRoot != null)
            {
                var starView = _starManager.GetView(star);
                var parent = starView != null ? starView.transform : worldRoot;
                var view = Instantiate(planetViewPrefab, parent.position, Quaternion.identity, worldRoot);
                view.Bind(planet, parent);
                _views[planet.Id] = view;
            }

            RefreshStarPlanetBonus(star);
            OnPlanetsChanged?.Invoke();
            return true;
        }

        public void Tick(float deltaTime)
        {
            foreach (var planet in _planets)
            {
                if (planet.HasLife)
                    continue;

                var star = GetStar(planet.HostStarId);
                if (star == null || star.IsDead)
                    continue;

                if (star.Stage == StarStage.RedGiant)
                    planet.IsUnstable = true;

                var baseChance = star.Stage == StarStage.Orange
                    ? _balance.lifeChanceOrangePerTick
                    : _balance.lifeChanceYellowPerTick;

                var chance = baseChance * _modifiers.LifeChanceMultiplier * _modifiers.VariantLifeChanceMultiplier;
                if (UnityEngine.Random.value < chance)
                    planet.DevelopLife();
            }

            UpdateOrbitVisuals(deltaTime);
        }

        private void UpdateOrbitVisuals(float deltaTime)
        {
            foreach (var kvp in _views)
            {
                if (kvp.Value != null)
                    kvp.Value.TickOrbit(deltaTime);
            }
        }

        public void HandleStarSupernova(Star star)
        {
            var toDestroy = new List<Planet>();
            foreach (var planet in _planets)
            {
                if (planet.HostStarId == star.Id)
                    toDestroy.Add(planet);
            }

            foreach (var planet in toDestroy)
                DestroyPlanet(planet, countForStats: !planet.IsUnstable);
        }

        private void DestroyPlanet(Planet planet, bool countForStats)
        {
            if (!_planets.Contains(planet))
                return;

            if (!countForStats)
                _destroyedPlanets++;

            planet.Destroy();
            _planets.Remove(planet);
            starRemovePlanetId(planet.HostStarId, planet.Id);

            if (_views.TryGetValue(planet.Id, out var view) && view != null)
            {
                Destroy(view.gameObject);
                _views.Remove(planet.Id);
            }

            var star = GetStar(planet.HostStarId);
            if (star != null)
                RefreshStarPlanetBonus(star);

            OnPlanetsChanged?.Invoke();
        }

        private void starRemovePlanetId(int starId, int planetId)
        {
            var star = GetStar(starId);
            star?.PlanetIds.Remove(planetId);
        }

        private void HandleLifeDeveloped(Planet planet)
        {
            _lifePlanets++;
            if (_views.TryGetValue(planet.Id, out var view) && view != null)
                view.RefreshVisual();
            OnPlanetsChanged?.Invoke();
        }

        private void HandlePlanetDestroyed(Planet planet)
        {
            if (planet.HasLife)
                _lifePlanets = Math.Max(0, _lifePlanets - 1);
            OnPlanetsChanged?.Invoke();
        }

        private void RefreshStarPlanetBonus(Star star)
        {
            double bonus = 1;
            foreach (var planet in _planets)
            {
                if (planet.HostStarId != star.Id)
                    continue;

                bonus += planet.HasLife
                    ? _balance.lifePlanetProductionBonus
                    : _balance.planetProductionBonus;
            }

            _starManager.SetPlanetBonus(star.Id, bonus);
        }

        private Star GetStar(int starId)
        {
            foreach (var star in _starManager.Stars)
            {
                if (star.Id == starId)
                    return star;
            }

            return null;
        }

        public int CountLifePlanets() => _lifePlanets;
        public int CountPlanets() => _planets.Count;
    }
}
