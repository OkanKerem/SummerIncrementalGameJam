using System;
using Universes.Core;
using Universes.Planets;
using Universes.Prestige;
using Universes.Stars;
using UnityEngine;

namespace Universes.Core
{
    public enum RunPhase
    {
        Idle,
        Active,
        Collapsed,
        AwaitingVariant
    }

    public class UniverseRunController : MonoBehaviour
    {
        [SerializeField] private GameBalance balance;
        [SerializeField] private UpgradeCatalog upgradeCatalog;
        [SerializeField] private VariantCatalog variantCatalog;
        [SerializeField] private GameClock gameClock;
        [SerializeField] private StarManager starManager;
        [SerializeField] private PlanetManager planetManager;
        [SerializeField] private Transform worldRoot;

        public GameBalance Balance => balance;
        public PrestigeState Prestige { get; private set; } = new();
        public StardustWallet Wallet { get; } = new();
        public EntropySystem Entropy { get; } = new();
        public RunModifiers Modifiers { get; private set; }
        public RunPhase Phase { get; private set; } = RunPhase.Idle;
        public RunStats CurrentStats { get; } = new();
        public CollapseBreakdown LastCollapse { get; private set; }
        public float RunTime { get; private set; }

        public event Action OnRunStarted;
        public event Action<CollapseBreakdown> OnRunCollapsed;
        public event Action OnPhaseChanged;

        private void Awake()
        {
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.ApplyToPrestige(Prestige);

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
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.PullFromPrestige(Prestige);
                SaveSystem.Instance.Save();
            }
        }

        public void RefreshModifiers()
        {
            Modifiers = RunModifiers.From(Prestige, upgradeCatalog, variantCatalog, balance);
        }

        public bool CanBigBang() => Phase == RunPhase.Idle || Phase == RunPhase.AwaitingVariant;

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

            Phase = RunPhase.Active;
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
            if (Phase != RunPhase.Active)
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
            if (Phase != RunPhase.Active)
                return;

            SyncStats();
            LastCollapse = CollapseCalculator.Calculate(CurrentStats, balance, Modifiers, Entropy.Entropy, manualCollapse);

            Prestige.AddDna(LastCollapse.dnaGained);
            Prestige.TotalCollapses++;
            Prestige.TotalStardustLifetime += CurrentStats.TotalStardustProduced;

            Phase = RunPhase.AwaitingVariant;
            OnRunCollapsed?.Invoke(LastCollapse);
            OnPhaseChanged?.Invoke();

            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.PullFromPrestige(Prestige);
                SaveSystem.Instance.Save();
            }
        }

        public void SelectVariant(string variantId)
        {
            Prestige.ActiveVariantId = variantId;
            RefreshModifiers();
            Phase = RunPhase.Idle;
            OnPhaseChanged?.Invoke();

            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.PullFromPrestige(Prestige);
                SaveSystem.Instance.Save();
            }
        }

        public bool TryCreateStar() => Phase == RunPhase.Active && starManager.TryCreateStar();
        public bool TryCreatePlanet() => Phase == RunPhase.Active && planetManager.TryCreatePlanet(starManager.SelectedStar);
        public bool TrySlowEntropy() => Phase == RunPhase.Active && Entropy.TryPurchaseSlowdown(balance, Wallet);

        public double GetCreateStarCost() => starManager.GetCreateStarCost();
        public double GetCreatePlanetCost() => planetManager.GetCreatePlanetCost();
    }
}
