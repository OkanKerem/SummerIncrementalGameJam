using System;
using System.Collections.Generic;
using Universes.Core;
using Universes.Prestige;
using Universes.Presentation;
using UnityEngine;

namespace Universes.Stars
{
    public class O_StarManager : MonoBehaviour
    {
        [SerializeField] private Transform worldRoot;
        [SerializeField] private O_StarView starViewPrefab;

        private readonly List<O_Star> _stars = new();
        private readonly Dictionary<int, O_StarView> _views = new();
        private O_GameBalance _balance;
        private O_RunModifiers _modifiers;
        private O_StardustWallet _wallet;
        private int _nextId = 1;
        private int _starsCreated;
        private int _supernovaCount;
        private int _blackHoleSeeds;

        public IReadOnlyList<O_Star> Stars => _stars;
        public int StarsCreated => _starsCreated;
        public int SupernovaCount => _supernovaCount;
        public int BlackHoleSeedCount => _blackHoleSeeds;
        public O_Star SelectedStar { get; private set; }

        public event Action<O_Star> OnStarSelected;
        public event Action<O_Star, double> OnStarClickedReward;
        public event Action<O_Star> OnStarSupernova;
        public event Action OnStarsChanged;

        public void Initialize(O_GameBalance balance, O_RunModifiers modifiers, O_StardustWallet wallet, Transform root)
        {
            _balance = balance;
            _modifiers = modifiers;
            _wallet = wallet;
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

            _stars.Clear();
            _views.Clear();
            SelectedStar = null;
            _nextId = 1;
            _starsCreated = 0;
            _supernovaCount = 0;
            _blackHoleSeeds = 0;
        }

        public double GetCreateStarCost()
        {
            return _balance.createStarBaseCost * Math.Pow(_balance.createStarCostScale, _stars.Count);
        }

        public bool TryCreateStar()
        {
            var cost = GetCreateStarCost();
            if (!_wallet.TrySpend(cost))
                return false;

            SpawnStar(GetSpawnPosition());
            return true;
        }

        public void SpawnStartingStars(int count)
        {
            for (var i = 0; i < count; i++)
                SpawnStar(GetSpawnPosition());
        }

        private void SpawnStar(Vector2 position)
        {
            var star = new O_Star(_nextId++, position);
            star.OnSupernova += HandleSupernova;
            star.OnDied += HandleStarDied;
            star.OnStageChanged += HandleStageChanged;
            _stars.Add(star);
            _starsCreated++;

            if (starViewPrefab != null && worldRoot != null)
            {
                var view = Instantiate(starViewPrefab, position, Quaternion.identity, worldRoot);
                view.Bind(star, this);
                _views[star.Id] = view;
            }

            OnStarsChanged?.Invoke();
        }

        private Vector2 GetSpawnPosition()
        {
            var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            var radius = UnityEngine.Random.Range(_balance.starSpawnRadiusMin, _balance.starSpawnRadiusMax);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        public void SelectStar(O_Star star)
        {
            SelectedStar = star;
            OnStarSelected?.Invoke(star);
        }

        public void OnStarClicked(O_Star star)
        {
            if (star == null || star.IsDead)
                return;

            SelectStar(star);

            var stageMult = O_StarStageUtility.GetProductionMultiplier(star.Stage, _balance);
            var reward = _balance.clickStardustYield * stageMult * _modifiers.GetEffectiveProductionMultiplier() * _modifiers.ClickYieldMultiplier;
            _wallet.Add(reward);

            var agePenalty = _balance.clickAgePenalty * (1 - Math.Min(_modifiers.ClickAgeReduction, 0.9));
            star.ApplyClickAgePenalty((float)agePenalty);

            OnStarClickedReward?.Invoke(star, reward);
        }

        public void Tick(float deltaTime)
        {
            var ageRate = _modifiers.GetEffectiveAgeRate(_balance.baseAgeRatePerSecond);
            var productionMult = _modifiers.GetEffectiveProductionMultiplier();

            foreach (var star in _stars)
            {
                if (star.IsDead)
                    continue;

                star.TickAge(deltaTime, ageRate);

                var passive = _balance.basePassiveStardustPerSecond
                    * O_StarStageUtility.GetProductionMultiplier(star.Stage, _balance)
                    * productionMult
                    * deltaTime;

                passive *= GetPlanetBonusForStar(star);
                if (passive > 0)
                    _wallet.Add(passive);
            }
        }

        public double GetPlanetBonusForStar(O_Star star)
        {
            // Planet bonus applied externally via PlanetManager callback
            return _planetBonus.TryGetValue(star.Id, out var bonus) ? bonus : 1;
        }

        private readonly Dictionary<int, double> _planetBonus = new();

        public void SetPlanetBonus(int starId, double multiplier)
        {
            _planetBonus[starId] = multiplier;
        }

        public void RemovePlanetBonus(int starId)
        {
            _planetBonus.Remove(starId);
        }

        private void HandleStageChanged(O_Star star)
        {
            if (_views.TryGetValue(star.Id, out var view) && view != null)
                view.RefreshVisual();
        }

        private void HandleSupernova(O_Star star)
        {
            _supernovaCount++;
            var reward = _balance.supernovaStardustReward * _modifiers.GetEffectiveProductionMultiplier();
            _wallet.Add(reward);

            if (UnityEngine.Random.value < _balance.blackHoleSeedChance)
                _blackHoleSeeds++;

            OnStarSupernova?.Invoke(star);
            OnStarsChanged?.Invoke();
        }

        private void HandleStarDied(O_Star star)
        {
            if (_views.TryGetValue(star.Id, out var view) && view != null)
            {
                view.PlayDeathEffect();
                Destroy(view.gameObject, 0.5f);
                _views.Remove(star.Id);
            }

            OnStarsChanged?.Invoke();
        }

        public O_StarView GetView(O_Star star) =>
            star != null && _views.TryGetValue(star.Id, out var view) ? view : null;
    }
}
