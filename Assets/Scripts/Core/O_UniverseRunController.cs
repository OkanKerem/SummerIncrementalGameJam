using System;
using Universes.Core;
using Universes.Planets;
using Universes.Prestige;
using Universes.Stars;
using UnityEngine;

namespace Universes.Core
{
    public enum O_RunPhase
    {
        Idle,
        Active,
        Collapsed,
        AwaitingVariant
    }

    public class O_UniverseRunController : MonoBehaviour
    {
        [SerializeField] private O_GameBalance balance;
        [SerializeField] private O_UpgradeCatalog upgradeCatalog;
        [SerializeField] private O_VariantCatalog variantCatalog;
        [SerializeField] private O_GameClock gameClock;
        [SerializeField] private O_StarManager starManager;
        [SerializeField] private O_PlanetManager planetManager;
        [SerializeField] private Transform worldRoot;

        public O_GameBalance Balance => balance;
        public O_PrestigeState Prestige { get; private set; } = new();
        public O_StardustWallet Wallet { get; } = new();
        public O_EntropySystem Entropy { get; } = new();
        public O_RunModifiers Modifiers { get; private set; }
        public O_RunPhase Phase { get; private set; } = O_RunPhase.Idle;
        public O_RunStats CurrentStats { get; } = new();
        public O_CollapseBreakdown LastCollapse { get; private set; }
        public float RunTime { get; private set; }

        public event Action OnRunStarted;
        public event Action<O_CollapseBreakdown> OnRunCollapsed;
        public event Action OnPhaseChanged;

        private void Awake()
        {
            if (O_SaveSystem.Instance != null)
                O_SaveSystem.Instance.ApplyToPrestige(Prestige);

            RefreshModifiers();
            WireEvents();
        }

        private void Start()
        {
            if (gameClock != null)
                gameClock.OnTick += HandleTick;

            if (starManager != null)
                starManager.OnStarSupernova += planetManager.HandleStarSupernova;

            Prestige.OnChanged += HandlePrestigeChanged;
        }

        private void OnDestroy()
        {
            if (gameClock != null)
                gameClock.OnTick -= HandleTick;

            if (starManager != null)
                starManager.OnStarSupernova -= planetManager.HandleStarSupernova;

            Prestige.OnChanged -= HandlePrestigeChanged;
        }

        private void WireEvents()
        {
            Wallet.OnTotalProducedChanged += total => CurrentStats.TotalStardustProduced = total;
        }

        private void HandlePrestigeChanged()
        {
            if (O_SaveSystem.Instance != null)
            {
                O_SaveSystem.Instance.PullFromPrestige(Prestige);
                O_SaveSystem.Instance.Save();
            }
        }

        public void RefreshModifiers()
        {
            Modifiers = O_RunModifiers.From(Prestige, upgradeCatalog, variantCatalog, balance);
        }

        public bool CanBigBang() => Phase == O_RunPhase.Idle || Phase == O_RunPhase.AwaitingVariant;

        public void BigBang()
        {
            if (!CanBigBang())
                return;

            RefreshModifiers();
            ResetRunState();

            var startingStars = balance.baseStartingStars + Modifiers.BonusStartingStars;
            var startingStardust = balance.baseBigBangStardust + Modifiers.BonusStartingStardust
                + Modifiers.ParallelEchoPerSecond * 5;

            Wallet.Reset(startingStardust);
            starManager.Initialize(balance, Modifiers, Wallet, worldRoot);
            planetManager.Initialize(balance, Modifiers, Wallet, starManager, worldRoot);
            starManager.Reset();
            planetManager.Reset();
            Entropy.Reset();

            starManager.SpawnStartingStars(Mathf.Max(1, startingStars));

            Phase = O_RunPhase.Active;
            OnRunStarted?.Invoke();
            OnPhaseChanged?.Invoke();
        }

        private void ResetRunState()
        {
            RunTime = 0;
            CurrentStats.TotalStardustProduced = 0;
            CurrentStats.StarsCreated = 0;
            CurrentStats.PlanetsCreated = 0;
            CurrentStats.LifePlanets = 0;
            CurrentStats.SupernovaCount = 0;
            CurrentStats.BlackHoleSeedCount = 0;
            CurrentStats.RunDurationSeconds = 0;
        }

        private void HandleTick(float deltaTime)
        {
            if (Phase != O_RunPhase.Active)
                return;

            RunTime += deltaTime;
            CurrentStats.RunDurationSeconds = RunTime;

            starManager.Tick(deltaTime);
            planetManager.Tick(deltaTime);
            Entropy.Tick(deltaTime, balance, Modifiers, starManager, planetManager);

            SyncStats();

            if (Entropy.IsCollapsed)
                Collapse(manualCollapse: false);
        }

        private void SyncStats()
        {
            CurrentStats.StarsCreated = starManager.StarsCreated;
            CurrentStats.PlanetsCreated = planetManager.PlanetsCreated;
            CurrentStats.LifePlanets = planetManager.LifePlanets;
            CurrentStats.SupernovaCount = starManager.SupernovaCount;
            CurrentStats.BlackHoleSeedCount = starManager.BlackHoleSeedCount;
            CurrentStats.TotalStardustProduced = Wallet.TotalProduced;
        }

        public void Collapse(bool manualCollapse)
        {
            if (Phase != O_RunPhase.Active)
                return;

            SyncStats();
            LastCollapse = O_CollapseCalculator.Calculate(CurrentStats, balance, Modifiers, Entropy.Entropy, manualCollapse);

            Prestige.AddDna(LastCollapse.dnaGained);
            Prestige.TotalCollapses++;
            Prestige.TotalStardustLifetime += CurrentStats.TotalStardustProduced;

            Phase = O_RunPhase.AwaitingVariant;
            OnRunCollapsed?.Invoke(LastCollapse);
            OnPhaseChanged?.Invoke();

            if (O_SaveSystem.Instance != null)
            {
                O_SaveSystem.Instance.PullFromPrestige(Prestige);
                O_SaveSystem.Instance.Save();
            }
        }

        public void SelectVariant(string variantId)
        {
            Prestige.ActiveVariantId = variantId;
            RefreshModifiers();
            Phase = O_RunPhase.Idle;
            OnPhaseChanged?.Invoke();

            if (O_SaveSystem.Instance != null)
            {
                O_SaveSystem.Instance.PullFromPrestige(Prestige);
                O_SaveSystem.Instance.Save();
            }
        }

        public bool TryCreateStar() => Phase == O_RunPhase.Active && starManager.TryCreateStar();
        public bool TryCreatePlanet() => Phase == O_RunPhase.Active && planetManager.TryCreatePlanet(starManager.SelectedStar);
        public bool TrySlowEntropy() => Phase == O_RunPhase.Active && Entropy.TryPurchaseSlowdown(balance, Wallet);

        public double GetCreateStarCost() => starManager.GetCreateStarCost();
        public double GetCreatePlanetCost() => planetManager.GetCreatePlanetCost();
    }
}
