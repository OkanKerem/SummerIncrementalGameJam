using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Universes.Game
{
    public class GameController : MonoBehaviour
    {
        public const int BaseAgePerClick = 1;
        public const int BaseAgePerPassiveTick = 1;
        public const int BaseSupernovaBonus = 50;
        public const int DefaultMaxStarHealth = 100;
        public const float PassiveTickInterval = 1f;

        [SerializeField] private GameplayMode gameplayMode = GameplayMode.SingleStarSystemAge;
        [SerializeField] private StarView starViewPrefab;
        [SerializeField] private BlackHoleView blackHolePrefab;
        [SerializeField] private UniverseCollapseSingularity collapseSingularityPrefab;
        [SerializeField] private PlanetManager planetManager;
        [SerializeField] private PlanetTypeCatalog planetTypeCatalog;
        [SerializeField] private SingleStarBalance singleStarBalance;
        [SerializeField] private Transform worldRoot;
        [SerializeField] private FloatingTextSpawner floatingTextSpawner;
        [SerializeField] private CosmicParticleManager particleManager;
        [SerializeField] private ParticleEffectManager effectManager;
        [SerializeField] private SfxManager sfxManager;
        [SerializeField] private Vector2 spawnAreaMin = new(-4f, -2.5f);
        [SerializeField] private Vector2 spawnAreaMax = new(4f, 2.5f);
        [SerializeField] private float minStarSeparation = 1.2f;

        public Upgrades Upgrades { get; } = new();
        public PrestigeState Prestige { get; } = new();
        public RunStats RunStats { get; } = new();
        public CollapseBreakdown LastCollapseBreakdown { get; private set; }
        public StarSystemBreakdown LastStarSystemBreakdown { get; private set; }

        public FloatingTextSpawner FloatingTextSpawner => floatingTextSpawner;
        public CosmicParticleManager ParticleManager => particleManager;
        public SfxManager SfxManager => sfxManager;
        public SingleStarBalance SingleStarBalance =>
            singleStarBalance != null ? singleStarBalance : _runtimeSingleStarBalance;
        public Phase3BalanceConfig Phase3Balance => SingleStarBalance.phase3;
        public GameplayMode GameplayMode => gameplayMode;
        public bool IsSingleStarMode => GameplayFeatures.IsSingleStarMode(gameplayMode);
        public PlanetManager PlanetManager => planetManager;
        public StarView CentralStar => _centralStar;
        public StarView SelectedStar { get; private set; }

        public double Stardust { get; private set; }
        public double DnaPotential { get; private set; }
        public int DnaFragments { get; private set; }
        public float Entropy { get; private set; }
        public float MaxEntropy => GameplayFeatures.UsesUniverseCollapse(gameplayMode)
            ? Mathf.Max(1f, Phase3Balance.maxEntropy)
            : GameplayFeatures.UsesEntropy(gameplayMode)
                ? Mathf.Max(1f, SingleStarBalance.multiStar.maxEntropy)
                : 100f;
        public bool IsCollapsed { get; private set; }
        public bool IsUniverseCollapsing => _collapseInProgress;
        public bool IsStarSystemEnded { get; private set; }
        public bool IsGameWon { get; private set; }
        public string EndingTitle { get; private set; }
        public string EndingText { get; private set; }
        public Planet WinningPlanet { get; private set; }
        public bool IsRunEnded =>
            IsGameWon || _collapseInProgress ||
            (IsSingleStarMode ? IsStarSystemEnded : IsCollapsed);
        public int ActiveBlackHoleCount => _blackHoles.Count;
        public bool HasBlackHoleDiscovery => RunStats.BlackHolesCreated > 0 || ActiveBlackHoleCount > 0;
        public float ActiveBlackHoleDnaPerSecond => ActiveBlackHoleCount * GetBlackHoleDnaPerSecond();
        public float ActiveBlackHoleInstabilityPerSecond =>
            ActiveBlackHoleCount * GetEffectiveBlackHoleEntropyPerSecond();

        public bool HasActiveStar => GetActiveStars().Count > 0;
        public int ActiveStarCount => GetActiveStars().Count;
        public IReadOnlyList<StarView> Stars => _stars;
        public int MaxStarSlots
        {
            get
            {
                if (GameplayFeatures.UsesUniverseCollapse(gameplayMode))
                    return Phase3Balance.maxStarSlots;

                if (GameplayFeatures.UsesMultiStar(gameplayMode))
                    return Mathf.Clamp(SingleStarBalance.multiStar.initialStarSlots + Upgrades.MaxStarCountLevel, 1,
                        SingleStarBalance.multiStar.maxStarSlots);

                return 1;
            }
        }

        public int AbsoluteMaxStarSlots => GameplayFeatures.UsesUniverseCollapse(gameplayMode)
            ? Phase3Balance.maxStarSlots
            : SingleStarBalance.multiStar.maxStarSlots;
        public bool HasOpenStarSlot => ActiveStarCount < MaxStarSlots && ActiveStarCount < AbsoluteMaxStarSlots;
        public int StarAge => HasActiveStar ? GetActiveStars()[0].StarAge : 0;
        public StarStage Stage =>
            HasActiveStar ? GetActiveStars()[0].Stage : StarStage.Supernova;

        public event Action OnStateChanged;
        public event Action<int> OnStardustGained;
        public event Action OnSupernova;
        public event Action OnStarCollision;
        public event Action OnBlackHoleSpawned;
        public event Action OnDnaGained;
        public event Action<float> OnEntropyChanged;
        public event Action<bool> OnUniverseCollapsed;
        public event Action OnStarSystemEnded;
        public event Action<string> OnExpansionFeedback;

        private StarView _centralStar;
        private SingleStarBalance _runtimeSingleStarBalance;

        private readonly List<StarView> _stars = new();
        private readonly List<BlackHoleView> _blackHoles = new();
        private readonly Queue<StarView> _supernovaQueue = new();
        private int _nextStarId = 1;
        private float _passiveTimer;
        private float _autoStarFormationTimer;
        private bool _processingSupernovas;
        private Coroutine _cameraZoomRoutine;
        private Coroutine _collapseRoutine;
        private bool _collapseInProgress;
        private UniverseCollapseSingularity _collapseSingularity;
        private string _pendingCollapseTitle;
        private string _pendingCollapseText;
        private float _runSaveTimer;

        private void Awake()
        {
            if (singleStarBalance == null)
                _runtimeSingleStarBalance = SingleStarBalance.CreateRuntimeDefault();
            else
                singleStarBalance.EnsureNestedConfigs();

            EnsureWorldRoot();
            EnsureManagersInitialized();
        }

        private void Start()
        {
            if (floatingTextSpawner == null)
                floatingTextSpawner = FindAnyObjectByType<FloatingTextSpawner>();

            var forceNewGame = RunSave.ConsumeNewGameRequest();
            var resetPrestige = RunSave.ConsumeResetPrestigeRequest();
            if (resetPrestige)
            {
                Prestige.Reset();
                PrestigeSave.Clear();
            }
            else
            {
                PrestigeSave.Load(Prestige);
            }

            EnsureCameraDragPan();
            GamePauseController.EnsureExists();

            if (!forceNewGame && RunSave.TryLoad(out var runData))
            {
                BeginFromSave(runData);
                return;
            }

            ResetToPhase1StartupState();
            if (IsSingleStarMode)
                StartNewStarSystem();
            else
                StartNewUniverse();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveCurrentRun();
        }

        private void OnApplicationQuit() => SaveCurrentRun();

        private void OnDestroy() => SaveCurrentRun();

        public void SaveCurrentRun()
        {
            if (!isActiveAndEnabled)
                return;

            RunSave.Save(this);
        }

        private void BeginFromSave(RunSaveData data)
        {
            Upgrades.LoadFromSave(data);
            gameplayMode = (GameplayMode)Mathf.Clamp(
                data.gameplayMode,
                0,
                (int)GameplayMode.FullCosmic);
            ExpansionSave.SetExpanded(Upgrades.IsUniverseExpanded);

            Entropy = 0f;
            IsCollapsed = false;
            IsStarSystemEnded = false;
            IsGameWon = false;
            EndingTitle = string.Empty;
            EndingText = string.Empty;
            WinningPlanet = null;
            _pendingCollapseTitle = null;
            _pendingCollapseText = null;

            StartNewRun(resetPrestigeBonuses: false, resetUpgrades: false);

            Stardust = Math.Max(0, data.stardust);
            DnaPotential = Math.Max(0, data.dnaPotential);
            DnaFragments = Math.Max(0, data.dnaFragments);
            Entropy = Mathf.Max(0f, data.entropy);
            RunStats.SurvivalTimeSeconds = Mathf.Max(0f, data.survivalTimeSeconds);

            if (gameplayMode == GameplayMode.FullCosmic)
            {
                foreach (var star in GetActiveStars())
                    ConfigureStarDrift(star, true);
            }

            NotifyStateChanged();
            SaveCurrentRun();
            GameSession.MarkGameStarted(SceneManager.GetActiveScene().name);
        }

        private void ResetToPhase1StartupState()
        {
            gameplayMode = GameplayMode.SingleStarSystemAge;
            ExpansionSave.SetExpanded(false);
            Entropy = 0f;
            IsCollapsed = false;
            IsStarSystemEnded = false;
            IsGameWon = false;
            EndingTitle = string.Empty;
            EndingText = string.Empty;
            WinningPlanet = null;
            _pendingCollapseTitle = null;
            _pendingCollapseText = null;

            if (_cameraZoomRoutine != null)
            {
                StopCoroutine(_cameraZoomRoutine);
                _cameraZoomRoutine = null;
            }

            var camera = Camera.main;
            if (camera != null && camera.orthographic)
            {
                camera.orthographicSize = SingleStarBalance.multiStar.phase1CameraSize;
                camera.transform.position = new Vector3(0f, 0f, camera.transform.position.z);
            }
        }

        private void EnsureCameraDragPan()
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            var dragPan = camera.GetComponent<CameraDragPan>() ??
                          camera.gameObject.AddComponent<CameraDragPan>();
            dragPan.Configure(
                SingleStarBalance.cameraDragEnabled,
                SingleStarBalance.cameraDragMouseButton,
                SingleStarBalance.cameraDragSpeed);
            dragPan.ConfigureZoom(
                SingleStarBalance.cameraZoomEnabled,
                SingleStarBalance.cameraZoomSpeed);
        }

        public void CancelCameraZoomAnimation()
        {
            if (_cameraZoomRoutine == null)
                return;

            StopCoroutine(_cameraZoomRoutine);
            _cameraZoomRoutine = null;
        }

        private void EnsureWorldRoot()
        {
            if (worldRoot != null)
                return;

            var root = new GameObject("WorldRoot");
            worldRoot = root.transform;
        }

        private void EnsureManagersInitialized()
        {
            if (particleManager == null)
            {
                particleManager = GetComponent<CosmicParticleManager>();
                if (particleManager == null)
                    particleManager = gameObject.AddComponent<CosmicParticleManager>();
            }

            if (effectManager == null)
            {
                effectManager = GetComponent<ParticleEffectManager>();
                if (effectManager == null)
                    effectManager = gameObject.AddComponent<ParticleEffectManager>();
            }

            if (sfxManager == null)
            {
                sfxManager = GetComponent<SfxManager>();
                if (sfxManager == null)
                    sfxManager = gameObject.AddComponent<SfxManager>();
            }

            if (planetManager == null)
            {
                planetManager = GetComponent<PlanetManager>();
                if (planetManager == null)
                    planetManager = gameObject.AddComponent<PlanetManager>();
            }

            planetManager.Initialize(this, worldRoot, planetTypeCatalog);
        }

        private void Update()
        {
            if (IsRunEnded)
                return;

            if (_collapseInProgress)
                return;

            RunStats.SurvivalTimeSeconds += Time.deltaTime;

            _runSaveTimer += Time.deltaTime;
            if (_runSaveTimer >= 5f)
            {
                _runSaveTimer = 0f;
                SaveCurrentRun();
            }

            if (IsSingleStarMode)
            {
                planetManager?.Tick(Time.deltaTime);
            }
            else if (GameplayFeatures.UsesMultiStar(gameplayMode) &&
                     !GameplayFeatures.UsesUniverseCollapse(gameplayMode))
            {
                TickEntropy(Time.deltaTime);
                TickConfiguredStarDrift(Time.deltaTime);
                TickAutoStarFormation(Time.deltaTime);
                planetManager?.Tick(Time.deltaTime);
            }
            else
            {
                TickEntropy(Time.deltaTime);
                TryVacuumParticlesOnClick();
                particleManager?.TickBlackHolePull(_blackHoles);
                TickStarDriftAndCollisions();
                TickParallelEcho(Time.deltaTime);
                TickAutoStarFormation(Time.deltaTime);
                planetManager?.Tick(Time.deltaTime);
            }

            _passiveTimer += Time.deltaTime;
            var passiveInterval = GetPassiveTickInterval();
            while (_passiveTimer >= passiveInterval)
            {
                _passiveTimer -= passiveInterval;
                TickPassiveProduction();
            }

            ProcessSupernovaQueue();
        }

        public int GetClickReward(StarView star)
        {
            if (IsRunEnded || star == null || !star.IsInteractable)
                return 0;

            if (IsSingleStarMode)
                return ApplyClickRewardMultiplier(SingleStarBalance.baseClickReward +
                                                  Upgrades.GetClickPowerBonus(SingleStarBalance.upgrades));

            var planetClickBonus = GameplayFeatures.UsesMultiStar(gameplayMode)
                ? GetPlanetCountForStar(star) *
                  Upgrades.StarPlanetClickValueLevel *
                  SingleStarBalance.multiStar.starClickValuePerPlanetPerUpgradeLevel
                : 0f;
            var mult = GetProductionMultiplier();
            return ApplyClickRewardMultiplier((StarStageUtility.GetClickReward(star.Stage) +
                                               Upgrades.GetClickPowerBonus(SingleStarBalance.upgrades) +
                                               planetClickBonus) * mult);
        }

        private int ApplyClickRewardMultiplier(float reward)
        {
            var percentPerLevel = SingleStarBalance.upgrades.clickRewardPercentPerLevel;
            var percentMultiplier = Mathf.Pow(1f + percentPerLevel, Upgrades.ClickPowerPercentLevel);
            return Mathf.RoundToInt(reward * percentMultiplier);
        }

        public int GetPassivePerSecond(StarView star)
        {
            if (IsRunEnded || star == null || !star.IsInteractable)
                return 0;

            if (IsSingleStarMode)
                return ApplyPassiveRewardMultiplier(SingleStarBalance.basePassivePerSecond +
                                                    Upgrades.GetPassiveProductionBonus(SingleStarBalance.upgrades));

            var mult = GetProductionMultiplier();
            return ApplyPassiveRewardMultiplier((StarStageUtility.GetPassivePerSecond(star.Stage) +
                                                 Upgrades.GetPassiveProductionBonus(SingleStarBalance.upgrades)) *
                                                mult);
        }

        private int ApplyPassiveRewardMultiplier(float reward)
        {
            var percentPerLevel = SingleStarBalance.upgrades.starPassiveProductionPercentPerLevel;
            var percentMultiplier = Mathf.Pow(1f + percentPerLevel, Upgrades.StarPassiveProductionPercentLevel);
            return Mathf.RoundToInt(reward * percentMultiplier);
        }

        public float GetPassiveTickInterval() =>
            GameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? PassiveTickInterval
                : SingleStarBalance.passiveTickInterval;

        public int GetAgePerClick() =>
            GameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? BaseAgePerClick
                : SingleStarBalance.agePerClick;

        public int GetAgePerPassiveTick() =>
            GameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? BaseAgePerPassiveTick
                : SingleStarBalance.agePerPassiveTick;

        public int GetMaxStarHealth() =>
            GameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? DefaultMaxStarHealth
                : SingleStarBalance.maxStarHealth;

        public int GetTotalPassivePerSecond()
        {
            var total = 0;
            foreach (var star in GetActiveStars())
            {
                total += GetPassivePerSecond(star);
                if (planetManager != null && GameplayFeatures.UsesMultiStar(gameplayMode))
                    total += Mathf.RoundToInt(planetManager.GetPassiveStardustForStar(star.StarId));
            }
            return total;
        }

        public int GetClickReward() =>
            HasActiveStar ? GetClickReward(GetActiveStars()[0]) : 0;

        public int GetPassivePerSecond() => GetTotalPassivePerSecond();

        public int GetSupernovaBonus() =>
            BaseSupernovaBonus + Upgrades.SupernovaBonusLevel *
            SingleStarBalance.upgrades.supernovaBonusPerLevel;

        public double GetCreateStarCost()
        {
            var createdAfterStartingStar = Mathf.Max(0, RunStats.StarsCreated - 1);
            var multiplier = 1.0 + SingleStarBalance.multiStar.createStarCostIncreasePercent / 100.0;
            return SingleStarBalance.multiStar.createStarBaseCost * Math.Pow(multiplier, createdAfterStartingStar);
        }

        public int GetConstellationStarCount() =>
            Mathf.Max(1, SingleStarBalance.multiStar.constellationStarCount);

        public double GetCreateConstellationCost()
        {
            // Constellation is priced as 3 sequential star purchases (cheaper than spawning 5).
            const int costStarCount = 3;
            var createdAfterStartingStar = Mathf.Max(0, RunStats.StarsCreated - 1);
            var multiplier = 1.0 + SingleStarBalance.multiStar.createStarCostIncreasePercent / 100.0;
            var baseCost = SingleStarBalance.multiStar.createStarBaseCost;
            double total = 0;
            for (var i = 0; i < costStarCount; i++)
                total += baseCost * Math.Pow(multiplier, createdAfterStartingStar + i);
            return total;
        }

        public bool CanCreateNewStar(out string reason)
        {
            reason = string.Empty;

            if (!GameplayFeatures.UsesMultiStar(gameplayMode))
            {
                reason = "Requires Multi-Star System Age";
                return false;
            }

            if (IsRunEnded)
            {
                reason = "Run ended";
                return false;
            }

            if (!HasOpenStarSlot)
            {
                reason = MaxStarSlots < AbsoluteMaxStarSlots
                    ? $"Requires Max Star Count Level {Upgrades.MaxStarCountLevel + 1}"
                    : $"Star slots full ({ActiveStarCount}/{MaxStarSlots})";
                return false;
            }

            if (!MeetsStarCreationRequirement(1, out reason))
                return false;

            var cost = GetCreateStarCost();
            if (Stardust < cost)
            {
                reason = $"Needs {cost:0} Stardust";
                return false;
            }

            return true;
        }

        public bool CanCreateConstellation(out string reason)
        {
            reason = string.Empty;

            if (!GameplayFeatures.UsesUniverseCollapse(gameplayMode))
            {
                reason = "Requires Full Cosmic Age";
                return false;
            }

            if (IsRunEnded)
            {
                reason = "Run ended";
                return false;
            }

            var needed = GetConstellationStarCount();
            var openSlots = Mathf.Max(0, MaxStarSlots - ActiveStarCount);
            if (openSlots < needed)
            {
                reason = openSlots <= 0
                    ? $"Star slots full ({ActiveStarCount}/{MaxStarSlots})"
                    : $"Needs {needed} open star slots ({openSlots} free)";
                return false;
            }

            var cost = GetCreateConstellationCost();
            if (Stardust < cost)
            {
                reason = $"Needs {cost:0} Stardust";
                return false;
            }

            return true;
        }

        public bool CanCreateStarOrConstellation(out string reason) =>
            GameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? CanCreateConstellation(out reason)
                : CanCreateNewStar(out reason);

        private bool MeetsStarCreationRequirement(out string reason) =>
            MeetsStarCreationRequirement(1, out reason);

        private bool MeetsStarCreationRequirement(int starsToCreate, out string reason)
        {
            reason = string.Empty;
            if (GameplayFeatures.UsesUniverseCollapse(gameplayMode))
                return true;

            starsToCreate = Mathf.Max(1, starsToCreate);
            var slotsAfterCreate = ActiveStarCount + starsToCreate;
            var initialSlots = SingleStarBalance.multiStar.initialStarSlots;
            if (slotsAfterCreate <= initialSlots)
                return true;

            var requiredMaxStarLevel = slotsAfterCreate - initialSlots;
            if (Upgrades.MaxStarCountLevel >= requiredMaxStarLevel)
                return true;

            reason = $"Requires Max Star Count Level {requiredMaxStarLevel}";
            return false;
        }

        public int GetPlanetCountForStar(StarView star) =>
            planetManager != null && star != null ? planetManager.GetPlanetCountForStar(star.StarId) : 0;

        public StarView GetStarById(int starId)
        {
            foreach (var star in _stars)
            {
                if (star != null && star.StarId == starId)
                    return star;
            }

            return null;
        }

        public void SelectStar(StarView star)
        {
            if (star == null || !star.IsInteractable)
                return;

            SelectedStar = star;
            planetManager?.BindHostStar(star);
            NotifyStateChanged();
        }

        public float GetClickCollectRadius() => Upgrades.GetClickCollectRadius(SingleStarBalance.upgrades);

        public float GetEffectiveAgeGainMultiplier() =>
            Upgrades.GetAgeGainMultiplier(SingleStarBalance.upgrades) *
            PrestigeModifiers.GetAgeGainMultiplier(Prestige);

        public float GetEffectiveAgeGainMultiplier(StarView star)
        {
            var multiplier = GetEffectiveAgeGainMultiplier();
            if (star != null && GameplayFeatures.UsesMultiStar(gameplayMode) && planetManager != null)
                multiplier *= planetManager.GetStarAgeBurdenMultiplier(star.StarId);
            return multiplier;
        }

        public float GetEntropyGainMultiplier() => PrestigeModifiers.GetEntropyMultiplier(Prestige);

        public float GetProductionMultiplier() => PrestigeModifiers.GetProductionMultiplier(Prestige);

        public float GetDoubleStardustChance() =>
            PrestigeModifiers.GetDoubleStardustChance(Prestige);

        public float GetBlackHoleDnaMultiplier() =>
            PrestigeModifiers.GetBlackHoleDnaMultiplier(Prestige) *
            (1f + Upgrades.BlackHoleMemoryLevel * SingleStarBalance.upgrades.blackHoleMemoryDnaMultiplierPerLevel);

        public float GetCosmicEventDnaChanceBonus() =>
            Upgrades.CosmicEventDnaLevel * SingleStarBalance.upgrades.cosmicEventDnaChancePerLevel;

        public float GetSpaceAgeDnaMultiplier() =>
            1f + Upgrades.SpaceAgeDnaLevel * SingleStarBalance.upgrades.spaceAgeDnaMultiplierPerLevel;

        public float GetSpaceAgeProgressionMultiplier() =>
            1f + Upgrades.SpaceAgeProgressionLevel * SingleStarBalance.upgrades.spaceAgeProgressionMultiplierPerLevel;

        public float GetSpaceAgeDnaMultiplierForLevel(int level) =>
            1f + level * SingleStarBalance.upgrades.spaceAgeDnaMultiplierPerLevel;

        public float GetOrbitalDnaMultiplier() =>
            1f + Upgrades.OrbitalDnaLevel * SingleStarBalance.upgrades.orbitalDnaMultiplierPerLevel;

        public float GetOrbitalDnaMultiplierForLevel(int level) =>
            1f + level * SingleStarBalance.upgrades.orbitalDnaMultiplierPerLevel;

        public float GetSpaceAgeProgressionMultiplierForLevel(int level) =>
            1f + level * SingleStarBalance.upgrades.spaceAgeProgressionMultiplierPerLevel;

        public float GetEffectiveBlackHoleEntropyPerSecond()
        {
            var reduction = Mathf.Clamp01(
                Upgrades.BlackHoleStabilizationLevel *
                SingleStarBalance.upgrades.blackHoleEntropyReductionPerLevel);
            return Phase3Balance.blackHoleEntropyPerSecond * (1f - reduction);
        }

        public float GetEffectiveStarDriftSpeed()
        {
            var baseSpeed = GameplayFeatures.UsesMultiStar(gameplayMode) &&
                            !GameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? SingleStarBalance.multiStar.starDriftSpeed
                : Phase3Balance.starDriftSpeed;
            return baseSpeed * (1f + Upgrades.CollisionAttractionLevel *
                SingleStarBalance.upgrades.collisionAttractionDriftMultiplierPerLevel);
        }

        public float GetEffectiveStarCollisionDistance() =>
            Phase3Balance.starCollisionDistance +
            Upgrades.CollisionAttractionLevel * SingleStarBalance.upgrades.collisionAttractionDistancePerLevel;

        public CivilizationStage GetHighestSpeciesStage()
        {
            var highest = CivilizationStage.NoLife;
            if (planetManager == null)
                return highest;

            foreach (var planet in planetManager.Planets)
            {
                if (planet == null || !planet.IsAlive || !planet.HasSpecies)
                    continue;

                if (planet.CivilizationStage > highest)
                    highest = planet.CivilizationStage;
            }

            return highest;
        }

        public string GetCurrentUniversePhaseLabel() =>
            CivilizationUtility.GetLabel(GetHighestSpeciesStage(), SingleStarBalance.civilization);

        public string GetClosestSpeciesToHardSpaceSummary()
        {
            if (planetManager == null)
                return "No living species";

            Planet best = null;
            foreach (var planet in planetManager.Planets)
            {
                if (planet == null || !planet.IsAlive || !planet.HasSpecies)
                    continue;

                if (best == null || planet.CivilizationStage > best.CivilizationStage ||
                    (planet.CivilizationStage == best.CivilizationStage &&
                     planet.CivilizationProgress > best.CivilizationProgress))
                {
                    best = planet;
                }
            }

            if (best == null)
                return "No living species";

            return $"{best.SpeciesName} on {planetManager.GetPlanetDisplayName(best)}";
        }

        private float GetBlackHoleDnaPerSecond() =>
            Phase3Balance.blackHoleDnaIntervalSeconds > 0f
                ? Phase3Balance.blackHoleDnaPotentialPerInterval / Phase3Balance.blackHoleDnaIntervalSeconds
                : 0f;

        public bool TryPurchasePrestigeUpgrade(PrestigeUpgradeDefinition definition)
        {
            if (!IsRunEnded || definition == null)
                return false;

            if (!Prestige.TryPurchase(definition))
                return false;

            OnStateChanged?.Invoke();
            return true;
        }

        private void TryVacuumParticlesOnClick()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            if (IsPointerOverUi())
                return;

            var collectRadius = GetClickCollectRadius();
            if (particleManager == null || collectRadius <= 0f)
                return;

            var worldPosition = particleManager.ScreenToWorldPosition(Input.mousePosition);
            var collected = particleManager.CollectParticlesInRadius(worldPosition, collectRadius);
            if (!collected.AnyCollected)
                return;

            var stage = HasActiveStar ? Stage : StarStage.Yellow;
            effectManager?.PlayCollectAreaEffect(worldPosition, collectRadius, stage);
        }

        private static bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
                return false;

            return EventSystem.current.IsPointerOverGameObject();
        }

        public bool TryPurchaseUpgrade(UpgradeDefinition definition)
        {
            if (IsRunEnded || definition == null)
                return false;

            if (definition.upgradeType == UpgradeType.ExpandUniverse)
                return TryExpandUniverse(definition);

            if (definition.upgradeType == UpgradeType.ExpandCosmic)
                return TryExpandCosmic(definition);

            if (!definition.ArePrerequisitesMet(this, out _))
                return false;

            var balance = Stardust;
            if (!Upgrades.TryPurchase(definition, ref balance))
                return false;

            Stardust = balance;

            if (definition.upgradeType == UpgradeType.CollisionAttraction)
                RefreshConfiguredStarDrift();

            if (definition.upgradeType == UpgradeType.EntropyEqualization)
                CompleteEntropyEqualizationWin();

            OnStateChanged?.Invoke();
            return true;
        }

        public bool TryExpandUniverse(UpgradeDefinition definition = null)
        {
            if (!IsSingleStarMode || IsRunEnded || Upgrades.IsUniverseExpanded)
                return false;

            if (definition == null)
                return false;

            var balance = Stardust;
            if (!Upgrades.TryPurchase(definition, ref balance))
                return false;

            Stardust = balance;
            ApplyUniverseExpansion();
            OnStateChanged?.Invoke();
            return true;
        }

        public bool TryExpandCosmic(UpgradeDefinition definition = null)
        {
            if (IsSingleStarMode || IsRunEnded || Upgrades.IsCosmicExpanded ||
                gameplayMode == GameplayMode.FullCosmic)
                return false;

            if (definition == null)
                return false;

            if (!Upgrades.IsUniverseExpanded)
                return false;

            var balance = Stardust;
            if (!Upgrades.TryPurchase(definition, ref balance))
                return false;

            Stardust = balance;
            ApplyCosmicExpansion(
                "The cosmos deepens! Welcome to Step 3: Full Cosmic Age with black holes, star collisions, and universe collapse.");
            OnStateChanged?.Invoke();
            return true;
        }

        private void ApplyUniverseExpansion()
        {
            ExpansionSave.SetExpanded(true);
            gameplayMode = GameplayMode.MultiStarSystemAge;
            PromoteCurrentStarSystemToMultiStar();
            AnimatePhase2CameraZoom();
            OnExpansionFeedback?.Invoke("The system expands! Welcome to Step 2: Multi-Star System Age.");
        }

        public void TryEnterPhase3FromSpecies(Planet planet)
        {
            if (planet == null || IsSingleStarMode || IsRunEnded || !Phase3Balance.enterPhase3WhenSpeciesReachesSpace ||
                gameplayMode == GameplayMode.FullCosmic)
                return;

            ApplyCosmicExpansion(
                $"{planet.SpeciesName} reached Space. Phase 3 begins: Black Holes, star collisions, and Hard Space are now active.");
        }

        private void ApplyCosmicExpansion(string feedbackMessage)
        {
            if (gameplayMode == GameplayMode.FullCosmic)
                return;

            gameplayMode = GameplayMode.FullCosmic;
            foreach (var star in GetActiveStars())
                ConfigureStarDrift(star, true);

            AnimatePhase3CameraZoom();
            OnExpansionFeedback?.Invoke(feedbackMessage);
            OnStateChanged?.Invoke();
        }

        public bool CanSpeciesEnterHardSpace(Planet planet)
        {
            if (planet == null || !planet.HasSpecies || planet.CivilizationStage != CivilizationStage.SpacePhase)
                return false;

            if (planet.Intelligence < Phase3Balance.hardSpaceRequiredIntelligence)
                return false;

            if (DnaPotential < Phase3Balance.hardSpaceRequiredDnaPotential)
                return false;

            // Black holes only exist in Phase 3; before that Hard Space uses the destroy-protocol ending.
            if (GameplayFeatures.UsesUniverseCollapse(gameplayMode) &&
                Phase3Balance.hardSpaceRequiresBlackHoleDiscovery &&
                !HasBlackHoleDiscovery)
                return false;

            return true;
        }

        public bool CanEndUniverse()
        {
            if (IsRunEnded || !GameplayFeatures.UsesUniverseCollapse(gameplayMode))
                return false;

            if (DnaPotential < Phase3Balance.endUniverseRequiredDnaPotential)
                return false;

            if (Entropy < Phase3Balance.endUniverseRequiredEntropy)
                return false;

            if (Phase3Balance.endUniverseRequiresBlackHoleDiscovery && !HasBlackHoleDiscovery)
                return false;

            if (Phase3Balance.endUniverseRequiresSpaceSpecies &&
                GetHighestSpeciesStage() < CivilizationStage.SpacePhase)
                return false;

            return true;
        }

        public void TryEndUniverse()
        {
            if (IsRunEnded || IsSingleStarMode || !GameplayFeatures.UsesEntropy(gameplayMode))
                return;

            BeginUniverseCollapseSequence(true);
        }

        public void CompleteHardSpaceWin(Planet planet)
        {
            if (IsRunEnded || planet == null || !GameplayFeatures.UsesUniverseCollapse(gameplayMode))
                return;

            IsGameWon = true;
            WinningPlanet = planet;
            EndingTitle = "Civilization Ascension";
            EndingText = "You created the universe.\nThey learned how it worked.\nNow they no longer need you.";
            FinalizeVictoryRun();
            OnExpansionFeedback?.Invoke($"{planet.SpeciesName} completed Hard Space. Civilization Ascension achieved.");
        }

        public void CompleteEntropyEqualizationWin()
        {
            if (IsRunEnded)
                return;

            IsGameWon = true;
            WinningPlanet = null;
            Entropy = 0f;
            EndingTitle = "Entropy Equalized";
            EndingText =
                "The universe finds balance.\nEntropy stabilizes across the cosmos.\nA new equilibrium is achieved.";
            FinalizeVictoryRun();
            OnExpansionFeedback?.Invoke("Entropy Equalized. The universe has reached stable equilibrium.");
        }

        private void FinalizeVictoryRun()
        {
            RunStats.SetFinalEntropy(Entropy);
            LastCollapseBreakdown = CollapseBreakdown.Calculate(RunStats, DnaPotential);
            Prestige.AddDna(LastCollapseBreakdown.TotalGained);
            Prestige.RecordCollapse();

            foreach (var star in _stars)
            {
                if (star != null)
                    star.SetInputEnabled(false);
            }

            IsCollapsed = true;
            OnUniverseCollapsed?.Invoke(true);
            OnStateChanged?.Invoke();
        }

        private void PromoteCurrentStarSystemToMultiStar()
        {
            IsStarSystemEnded = false;
            IsCollapsed = false;
            Entropy = 0f;
            _passiveTimer = 0f;
            _supernovaQueue.Clear();

            if (_centralStar == null)
                _centralStar = GetActiveStars().FirstOrDefault();

            if (_centralStar == null)
            {
                SpawnStartingMultiStar();
            }
            else
            {
                ConfigureStarDrift(_centralStar, SingleStarBalance.multiStar.moveCentralStarInPhase2);
                _centralStar.ConfigureMaxAge(SingleStarBalance.maxStarHealth);
                SelectedStar = _centralStar;
                planetManager?.BindHostStar(_centralStar);
            }

            OnEntropyChanged?.Invoke(Entropy);
            NotifyStateChanged();
        }

        public void OnStarClicked(StarView star)
        {
            if (IsRunEnded || star == null || !star.IsInteractable)
                return;

            if (GameplayFeatures.UsesMultiStar(gameplayMode))
                SelectStar(star);

            var stage = star.Stage;
            var reward = GetClickReward(star);

            sfxManager?.PlayStarClick();
            effectManager?.PlayClickEffect(star.transform.position, stage);

            double creditedReward;
            if (IsSingleStarMode)
                creditedReward = CreditStardustDirect(reward);
            else if (GameplayFeatures.UsesParticleVacuum(gameplayMode))
            {
                EmitStardust(reward, star.transform.position, stage);
                creditedReward = reward;
            }
            else
                creditedReward = CreditStardustDirect(reward);

            floatingTextSpawner?.Spawn(star.transform.position, Mathf.RoundToInt((float)creditedReward), stage);
            RunStats.RecordClick();

            star.AddAge(GetAgePerClick(), GetEffectiveAgeGainMultiplier(star));

            if (GameplayFeatures.UsesEntropy(gameplayMode))
                AddEntropy(EntropyBalance.EntropyPerClick);

            star.RefreshVisual();
            star.PlayClickPop();

            if (star.HasReachedMaxAge)
                EnqueueSupernova(star);
            else
                NotifyStateChanged();
        }

        public double CreditStardustDirect(double amount)
        {
            if (IsRunEnded || amount <= 0)
                return 0;

            amount = ApplyDoubleStardustChance(amount);

            Stardust += amount;
            RunStats.RecordStardustProduced(amount);

            OnStardustGained?.Invoke((int)amount);
            NotifyStateChanged();
            return amount;
        }

        private double ApplyDoubleStardustChance(double amount)
        {
            var chance = Mathf.Clamp01(GetDoubleStardustChance());
            return chance > 0f && UnityEngine.Random.value < chance ? amount * 2.0 : amount;
        }

        public bool SpendStardust(double amount)
        {
            if (IsRunEnded || Stardust < amount)
                return false;

            Stardust -= amount;
            NotifyStateChanged();
            return true;
        }

        public void AddDnaPotential(double amount)
        {
            if (IsRunEnded || amount <= 0)
                return;

            DnaPotential += amount;
            NotifyStateChanged();
        }

        public void NotifyDnaPotentialGained() => OnDnaGained?.Invoke();

        public void TrySpawnDnaPotential(Vector3 position, float amount)
        {
            if (IsRunEnded || particleManager == null || amount <= 0f)
                return;

            particleManager.SpawnDnaPotential(position, amount);
        }

        public float GetCollisionDnaPotential(float baseAmount) =>
            Mathf.Max(0f, baseAmount +
                          Upgrades.CollisionDnaProductionLevel *
                          SingleStarBalance.upgrades.collisionDnaPotentialPerLevel);

        public float GetSpeciesDnaPotentialPerTick(Planet planet)
        {
            if (planet == null || !planet.HasSpecies || Upgrades.SpeciesDnaProductionLevel <= 0)
                return 0f;

            var speciesMultiplier = SingleStarBalance.species.GetDnaPotentialMultiplier(
                planet.Intelligence,
                planet.Aggression);
            return Upgrades.SpeciesDnaProductionLevel *
                   SingleStarBalance.upgrades.speciesDnaPotentialPerLevel *
                   speciesMultiplier *
                   GetAdvancedSpeciesDnaMultiplier(planet.CivilizationStage);
        }

        public int GetPlanetClickReward(Planet planet)
        {
            if (planet == null || !planet.IsAlive)
                return 0;

            var definition = planet.Definition;
            var baseReward = (definition != null
                                 ? definition.baseClickValue
                                 : SingleStarBalance.planets.fallbackBaseClickReward) +
                             Upgrades.GetClickPowerBonus(SingleStarBalance.upgrades) +
                             Upgrades.PlanetClickValueLevel *
                             (IsSingleStarMode
                                 ? SingleStarBalance.planetClickValuePerLevel
                                 : PlanetBalance.PlanetClickValuePerLevel);
            var civilizationMultiplier =
                SingleStarBalance.civilization.GetStageClickBonusMultiplier(planet.CivilizationStage) *
                GetAdvancedSpeciesClickMultiplier(planet.CivilizationStage);
            var speciesMultiplier = planet.HasSpecies
                ? SingleStarBalance.species.GetClickStardustMultiplier(planet.Intelligence, planet.Aggression)
                : 1f;

            return ApplyClickRewardMultiplier((float)baseReward * civilizationMultiplier * speciesMultiplier);
        }

        public float GetAdvancedSpeciesDnaMultiplier(CivilizationStage stage)
        {
            var mult = stage switch
            {
                CivilizationStage.HardSpace => Phase3Balance.hardSpaceDnaMultiplier,
                CivilizationStage.SpacePhase => Phase3Balance.spacePhaseDnaMultiplier,
                _ => 1f
            };

            if (stage == CivilizationStage.SpacePhase)
                mult *= GetSpaceAgeDnaMultiplier();

            return mult;
        }

        private float GetAdvancedSpeciesClickMultiplier(CivilizationStage stage) => stage switch
        {
            CivilizationStage.HardSpace => Phase3Balance.hardSpacePlanetClickMultiplier,
            CivilizationStage.SpacePhase => Phase3Balance.spacePhasePlanetClickMultiplier,
            _ => 1f
        };

        public float GetPlanetClickDamage() =>
            IsSingleStarMode
                ? SingleStarBalance.basePlanetClickDamage
                : PlanetBalance.BasePlanetClickDamage;

        public void NotifyStateChanged() => OnStateChanged?.Invoke();

        public bool TryCreatePlanet()
        {
            if (planetManager == null)
                return false;

            if ((SelectedStar == null || !SelectedStar.IsInteractable) && HasActiveStar)
                SelectStar(GetActiveStars()[0]);

            return planetManager.TryCreatePlanet();
        }

        public void StartNewStarSystem()
        {
            StartNewRun(resetPrestigeBonuses: false);
            SaveCurrentRun();
        }

        public double CreditStardust(double amount)
        {
            return CreditStardustDirect(amount);
        }

        public void CreditDnaFragment()
        {
            if (IsCollapsed)
                return;

            DnaFragments++;
            RunStats.RecordDnaFragment();
            OnDnaGained?.Invoke();
            OnStateChanged?.Invoke();
        }

        public void TrySpawnDnaFragment(Vector3 position)
        {
            if (IsCollapsed || particleManager == null)
                return;

            particleManager.SpawnDnaFragment(position);
        }

        public void AddBlackHoleDnaPotential(float amount)
        {
            if (amount <= 0f || IsRunEnded)
                return;

            var finalAmount = amount * GetBlackHoleDnaMultiplier();
            RunStats.AddBlackHoleDnaPotential(finalAmount);
            AddDnaPotential(finalAmount);
        }

        public void OnParticleConsumedByBlackHole(int particleValue, Transform blackHole)
        {
            if (IsCollapsed)
                return;

            CreditStardust(particleValue * Phase3Balance.blackHoleConsumedStardustMultiplier);
            if (UnityEngine.Random.value < Phase3Balance.blackHoleConsumeDnaChance)
                TrySpawnDnaFragment(blackHole.position);
        }

        public void TickBlackHoleThreat(BlackHoleView hole, float deltaTime)
        {
            if (hole == null || IsRunEnded || deltaTime <= 0f)
                return;

            var holePosition = hole.transform.position;
            var pullRadius = Phase3Balance.blackHoleGravityPullRadius;
            var pullSpeed = Phase3Balance.blackHoleGravityPullSpeed;
            var starConsumeRadius = Phase3Balance.blackHoleStarConsumeRadius;
            var planetConsumeRadius = Phase3Balance.blackHolePlanetConsumeRadius;

            if (pullRadius > 0f && pullSpeed > 0f)
            {
                foreach (var star in GetActiveStars().ToList())
                {
                    if (star == null || !star.IsInteractable)
                        continue;

                    star.PullToward(holePosition, pullRadius, pullSpeed, deltaTime);
                }

                planetManager?.PullPlanetsTowardBlackHole(holePosition, pullRadius, pullSpeed, deltaTime);
            }

            foreach (var star in GetActiveStars().ToList())
            {
                if (star == null || !star.IsInteractable)
                    continue;

                var distance = Vector3.Distance(star.transform.position, holePosition);
                if (starConsumeRadius <= 0f || distance > starConsumeRadius)
                    continue;

                var position = star.transform.position;
                planetManager?.DestroyPlanetsForStar(star);
                RemoveStarImmediate(star);
                TrySpawnDnaPotential(position, Phase3Balance.blackHoleDnaPotentialPerInterval);
                OnExpansionFeedback?.Invoke("Black Hole consumed a nearby star system.");
            }

            var consumedPlanets = planetManager?.ConsumePlanetsByBlackHole(
                holePosition,
                planetConsumeRadius,
                Phase3Balance.blackHoleDnaPotentialPerInterval) ?? 0;

            if (consumedPlanets > 0)
                OnExpansionFeedback?.Invoke("Black Hole consumed nearby planets.");
        }

        public void AddBlackHoleEntropy(float amount)
        {
            if (amount <= 0f)
                return;

            var blackHoleReduction = Mathf.Clamp01(
                Upgrades.BlackHoleStabilizationLevel *
                SingleStarBalance.upgrades.blackHoleEntropyReductionPerLevel);
            ApplyEntropyGain(amount * (1f - blackHoleReduction));
        }

        public void AddEntropy(float amount)
        {
            if (amount <= 0f)
                return;

            ApplyEntropyGain(amount);
        }

        private void ApplyEntropyGain(float amount)
        {
            if (!GameplayFeatures.UsesEntropy(gameplayMode) || IsCollapsed || _collapseInProgress || amount <= 0f ||
                Upgrades.IsEntropyEqualized)
                return;

            var upgradeReduction = Mathf.Clamp01(
                Upgrades.EntropyReductionLevel * SingleStarBalance.upgrades.entropyReductionPerLevel);
            Entropy = Mathf.Min(MaxEntropy, Entropy + amount * GetEntropyGainMultiplier() * (1f - upgradeReduction));
            OnEntropyChanged?.Invoke(Entropy);

            if (Entropy >= MaxEntropy && GameplayFeatures.UsesEntropy(gameplayMode))
                BeginUniverseCollapseSequence(false);
        }

        public void ClampStarPosition(StarView star) =>
            star?.TickDrift(0f, GetStarSpawnAreaMin(), GetStarSpawnAreaMax());

        private void TickPassiveProduction()
        {
            if (IsRunEnded)
                return;

            var changed = false;
            foreach (var star in GetActiveStars().ToList())
            {
                var reward = GetPassivePerSecond(star);
                var planetReward = planetManager != null && GameplayFeatures.UsesMultiStar(gameplayMode)
                    ? Mathf.RoundToInt(planetManager.GetPassiveStardustForStar(star.StarId))
                    : 0;
                var totalReward = reward + planetReward;
                if (totalReward <= 0)
                    continue;

                if (IsSingleStarMode || !GameplayFeatures.UsesParticleVacuum(gameplayMode))
                    CreditStardustDirect(totalReward);
                else
                    EmitStardust(totalReward, star.transform.position, star.Stage);

                star.AddAge(GetAgePerPassiveTick(), GetEffectiveAgeGainMultiplier(star));

                if (GameplayFeatures.UsesEntropy(gameplayMode))
                    AddEntropy(EntropyBalance.EntropyPerPassiveTick);

                star.RefreshVisual();
                changed = true;

                if (star.HasReachedMaxAge)
                    EnqueueSupernova(star);
            }

            if (changed)
                NotifyStateChanged();
        }

        private void TickEntropy(float deltaTime)
        {
            var rate = EntropyBalance.BaseEntropyPerSecond;
            if (GameplayFeatures.UsesMultiStar(gameplayMode) &&
                !GameplayFeatures.UsesUniverseCollapse(gameplayMode))
            {
                rate += ActiveStarCount * SingleStarBalance.multiStar.entropyPerActiveStarPerSecond;
                rate += (planetManager?.PlanetCount ?? 0) * SingleStarBalance.multiStar.entropyPerPlanetPerSecond;
            }
            AddEntropy(rate * deltaTime);
        }

        private void EmitStardust(int amount, Vector3 position, StarStage stage, bool playEmitVfx = true)
        {
            if (amount <= 0)
                return;

            if (playEmitVfx)
                effectManager?.PlayStardustEmitEffect(position, stage);

            CreditStardustDirect(amount);
        }

        public void QueueSupernova(StarView star) => EnqueueSupernova(star);

        private void EnqueueSupernova(StarView star)
        {
            if (star == null || !star.BeginSupernova())
                return;

            _supernovaQueue.Enqueue(star);
            NotifyStateChanged();
        }

        private void ProcessSupernovaQueue()
        {
            if (_processingSupernovas || _supernovaQueue.Count == 0)
                return;

            _processingSupernovas = true;
            while (_supernovaQueue.Count > 0)
            {
                var star = _supernovaQueue.Dequeue();
                if (star != null)
                    ExecuteSupernova(star);
            }

            _processingSupernovas = false;
        }

        private void ExecuteSupernova(StarView star)
        {
            var position = star.transform.position;

            effectManager?.PlaySupernovaEffect(position);
            RunStats.RecordSupernova();

            if (IsSingleStarMode)
            {
                star.PlaySupernovaEffect();
                OnSupernova?.Invoke();
                EndStarSystem();
                return;
            }

            if (GameplayFeatures.UsesMultiStar(gameplayMode) &&
                !GameplayFeatures.UsesUniverseCollapse(gameplayMode))
            {
                CreditStardustDirect(SingleStarBalance.multiStar.supernovaStardustBonus +
                                     Upgrades.SupernovaBonusLevel * SingleStarBalance.upgrades.supernovaBonusPerLevel);
                AddEntropy(EntropyBalance.EntropyPerSupernova);
                planetManager?.DestroyPlanetsForStar(star);
                ApplySupernovaAreaEffect(position, star);
                OnSupernova?.Invoke();
                NotifyStateChanged();
                star.PlaySupernovaEffect();
                return;
            }

            var bonus = GetSupernovaBonus() + Phase3Balance.supernovaParticleBurst;
            EmitStardust(bonus, position, StarStage.Supernova, playEmitVfx: false);
            AddEntropy(EntropyBalance.EntropyPerSupernova);
            ApplySupernovaAreaEffect(position, star);

            planetManager?.DamagePlanetsInRadius(
                position,
                Phase3Balance.supernovaPlanetDamageRadius,
                Phase3Balance.supernovaPlanetDamage,
                destroyUnstable: true,
                grantDna: true,
                "Supernova shockwave damaged nearby planets.");

            if (UnityEngine.Random.value < Phase3Balance.supernovaDnaChance +
                PrestigeModifiers.GetSupernovaDnaChanceBonus(Prestige) + GetCosmicEventDnaChanceBonus())
                TrySpawnDnaFragment(position);

            if (UnityEngine.Random.value < Phase3Balance.supernovaBlackHoleChance)
                TrySpawnBlackHole(position);

            OnSupernova?.Invoke();
            NotifyStateChanged();
            star.PlaySupernovaEffect();
        }

        private void ApplySupernovaAreaEffect(Vector3 origin, StarView source)
        {
            foreach (var other in GetActiveStars().ToList())
            {
                if (other == null || other == source || !other.IsInteractable)
                    continue;

                var radius = GameplayFeatures.UsesUniverseCollapse(gameplayMode)
                    ? Phase3Balance.supernovaRadius
                    : SingleStarBalance.multiStar.supernovaNearbyStarRadius;
                if (Vector3.Distance(other.transform.position, origin) > radius)
                    continue;

                var damage = GameplayFeatures.UsesUniverseCollapse(gameplayMode)
                    ? Phase3Balance.supernovaAgeBurst
                    : SingleStarBalance.multiStar.supernovaNearbyStarAgeDamage;
                other.AddAge(damage, GetEffectiveAgeGainMultiplier(other));
                other.RefreshVisual();

                if (other.HasReachedMaxAge)
                    EnqueueSupernova(other);
            }
        }

        private void TickStarDriftAndCollisions()
        {
            var active = GetActiveStars();
            foreach (var star in active)
                star.TickDrift(Time.deltaTime, GetStarSpawnAreaMin(), GetStarSpawnAreaMax());

            for (var i = 0; i < active.Count; i++)
            {
                for (var j = i + 1; j < active.Count; j++)
                {
                    var a = active[i];
                    var b = active[j];
                    if (a == null || b == null || !a.IsInteractable || !b.IsInteractable)
                        continue;

                    if (Vector3.Distance(a.transform.position, b.transform.position) <=
                        GetEffectiveStarCollisionDistance())
                    {
                        HandleStarCollision(a, b);
                        return;
                    }
                }
            }
        }

        private void TickConfiguredStarDrift(float deltaTime)
        {
            foreach (var star in GetActiveStars())
                star.TickDrift(deltaTime, GetStarSpawnAreaMin(), GetStarSpawnAreaMax());
        }

        private void HandleStarCollision(StarView a, StarView b)
        {
            if (a == null || b == null || !a.IsInteractable || !b.IsInteractable)
                return;

            var midpoint = (a.transform.position + b.transform.position) * 0.5f;
            var maxAge = Mathf.Max(1, GetMaxStarHealth());
            var ageRatio = Mathf.Clamp01((a.StarAge + b.StarAge) * 0.5f / maxAge);

            planetManager?.DamagePlanetsInRadius(
                midpoint,
                Phase3Balance.collisionPlanetDamageRadius,
                Phase3Balance.collisionPlanetDamage,
                destroyUnstable: true,
                grantDna: true,
                "Star Collision Detected: nearby planets were damaged by the collapse.");
            planetManager?.DestroyPlanetsForStar(a);
            planetManager?.DestroyPlanetsForStar(b);

            RemoveStarImmediate(a);
            RemoveStarImmediate(b);

            EmitStardust((int)Phase3Balance.collisionStardustBurst +
                         Phase3Balance.collisionParticleBurst, midpoint, StarStage.RedGiant);

            AddEntropy(Phase3Balance.collisionEntropy);
            RunStats.RecordCollision();

            if (UnityEngine.Random.value < Phase3Balance.collisionDnaChance + GetCosmicEventDnaChanceBonus())
                TrySpawnDnaFragment(midpoint);

            var collisionDnaPotential = GetCollisionDnaPotential(Phase3Balance.collisionDnaPotential);
            if (collisionDnaPotential > 0f)
                TrySpawnDnaPotential(midpoint, collisionDnaPotential);

            // Older colliding stars favor black holes; otherwise the collision becomes a supernova.
            var blackHoleChance = Mathf.Clamp01(
                Phase3Balance.collisionBlackHoleChance + ageRatio * 0.45f);
            if (UnityEngine.Random.value < blackHoleChance)
            {
                TrySpawnBlackHole(midpoint);
                OnExpansionFeedback?.Invoke("Star collision collapsed into a Black Hole!");
            }
            else
            {
                TriggerCollisionSupernova(midpoint);
                OnExpansionFeedback?.Invoke("Star collision triggered a Supernova!");
            }

            OnStarCollision?.Invoke();
            OnStateChanged?.Invoke();
        }

        private void TriggerCollisionSupernova(Vector3 position)
        {
            effectManager?.PlaySupernovaEffect(position);
            RunStats.RecordSupernova();

            var bonus = GetSupernovaBonus() + Phase3Balance.supernovaParticleBurst;
            EmitStardust(bonus, position, StarStage.Supernova, playEmitVfx: false);
            AddEntropy(EntropyBalance.EntropyPerSupernova);
            ApplySupernovaAreaEffect(position, null);

            planetManager?.DamagePlanetsInRadius(
                position,
                Phase3Balance.supernovaPlanetDamageRadius,
                Phase3Balance.supernovaPlanetDamage,
                destroyUnstable: true,
                grantDna: true,
                "Collision supernova shockwave damaged nearby planets.");

            if (UnityEngine.Random.value < Phase3Balance.supernovaDnaChance +
                PrestigeModifiers.GetSupernovaDnaChanceBonus(Prestige) + GetCosmicEventDnaChanceBonus())
                TrySpawnDnaFragment(position);

            OnSupernova?.Invoke();
        }

        private void TrySpawnBlackHole(Vector3 position)
        {
            if (IsCollapsed)
                return;

            BlackHoleView hole;
            if (blackHolePrefab != null)
                hole = Instantiate(blackHolePrefab, position, Quaternion.identity, worldRoot);
            else
            {
                var go = new GameObject("BlackHole");
                go.transform.SetParent(worldRoot);
                hole = go.AddComponent<BlackHoleView>();
            }

            hole.Init(this, position);
            _blackHoles.Add(hole);
            RunStats.RecordBlackHole();
            OnBlackHoleSpawned?.Invoke();
            OnStateChanged?.Invoke();
        }

        public void RemoveBlackHole(BlackHoleView hole)
        {
            if (hole == null)
                return;

            _blackHoles.Remove(hole);
            Destroy(hole.gameObject);
            OnStateChanged?.Invoke();
        }

        public bool TryCreateNewStar()
        {
            if (GameplayFeatures.UsesUniverseCollapse(gameplayMode))
                return TryCreateConstellation();

            if (!CanCreateNewStar(out _))
                return false;

            Stardust -= GetCreateStarCost();
            var star = SpawnStar();
            if (star != null)
                SelectStar(star);
            OnStateChanged?.Invoke();
            return star != null;
        }

        public bool TryCreateConstellation()
        {
            if (!CanCreateConstellation(out _))
                return false;

            Stardust -= GetCreateConstellationCost();
            var spawned = SpawnConstellationNear(GetRandomSpawnPosition(), GetConstellationStarCount());
            if (spawned > 0)
            {
                OnExpansionFeedback?.Invoke($"Constellation formed ({spawned} stars).");
                OnStateChanged?.Invoke();
                return true;
            }

            return false;
        }

        public void TryCollapseUniverse()
        {
            TryEndUniverse();
        }

        public void StartNewUniverse()
        {
            StartNewRun(resetPrestigeBonuses: true);
            SaveCurrentRun();
        }

        private void StartNewRun(bool resetPrestigeBonuses) => StartNewRun(resetPrestigeBonuses, resetUpgrades: true);

        private void StartNewRun(bool resetPrestigeBonuses, bool resetUpgrades)
        {
            foreach (var star in _stars.ToList())
            {
                if (star != null)
                    Destroy(star.gameObject);
            }

            foreach (var hole in _blackHoles.ToList())
            {
                if (hole != null)
                    Destroy(hole.gameObject);
            }

            _stars.Clear();
            _blackHoles.Clear();
            _supernovaQueue.Clear();
            _centralStar = null;
            SelectedStar = null;
            _nextStarId = 1;
            _autoStarFormationTimer = 0f;
            particleManager?.ClearAll();
            planetManager?.ResetAll();

            Stardust = PrestigeModifiers.GetStartingStardust(Prestige);
            DnaPotential = 0;
            DnaFragments = 0;
            Entropy = 0;
            IsCollapsed = false;
            IsStarSystemEnded = false;
            IsGameWon = false;
            EndingTitle = string.Empty;
            EndingText = string.Empty;
            WinningPlanet = null;
            _pendingCollapseTitle = null;
            _pendingCollapseText = null;
            _passiveTimer = 0f;
            if (resetUpgrades)
                Upgrades.Reset();
            RunStats.Reset();
            LastCollapseBreakdown = null;
            LastStarSystemBreakdown = null;

            OnEntropyChanged?.Invoke(Entropy);

            if (IsSingleStarMode)
                SpawnCentralStar();
            else if (GameplayFeatures.UsesMultiStar(gameplayMode) &&
                     !GameplayFeatures.UsesUniverseCollapse(gameplayMode))
                SpawnStartingMultiStar();
            else
                SpawnStar();

            NotifyStateChanged();
        }

        private void EndStarSystem()
        {
            if (IsStarSystemEnded)
                return;

            IsStarSystemEnded = true;
            planetManager?.DestroyAllPlanets();

            foreach (var star in _stars)
            {
                if (star != null)
                    star.SetInputEnabled(false);
            }

            var civRank = planetManager != null ? planetManager.HighestCivilizationRank : 0;
            LastStarSystemBreakdown = StarSystemBreakdown.Calculate(RunStats, DnaPotential, civRank);
            Prestige.AddDna(LastStarSystemBreakdown.TotalGained);

            OnStarSystemEnded?.Invoke();
            NotifyStateChanged();
        }

        public void OnStarRemoved(StarView star)
        {
            if (star != null)
                _stars.Remove(star);

            if (SelectedStar == star)
            {
                SelectedStar = GetActiveStars().FirstOrDefault();
                if (SelectedStar != null)
                    planetManager?.BindHostStar(SelectedStar);
            }

            OnStateChanged?.Invoke();
        }

        private void RemoveStarImmediate(StarView star)
        {
            if (star == null)
                return;

            _stars.Remove(star);

            if (SelectedStar == star)
            {
                SelectedStar = GetActiveStars().FirstOrDefault();
                planetManager?.BindHostStar(SelectedStar);
            }

            Destroy(star.gameObject);
            NotifyStateChanged();
        }

        public void BeginDestroyProtocolCollapse(string speciesName)
        {
            if (IsRunEnded || _collapseInProgress || IsCollapsed ||
                GameplayFeatures.UsesUniverseCollapse(gameplayMode))
                return;

            _pendingCollapseTitle = "Destroy Protocol";
            _pendingCollapseText =
                $"{(string.IsNullOrWhiteSpace(speciesName) ? "A Hard Space civilization" : speciesName)} " +
                "launched annihilation rockets.\nEverything is erased.\nThe universe ends.";
            EndingTitle = _pendingCollapseTitle;
            EndingText = _pendingCollapseText;
            IsGameWon = false;

            // Rocket barrage already wiped the system — skip singularity and open the game-over panel.
            if (_collapseRoutine != null)
                StopCoroutine(_collapseRoutine);
            _collapseRoutine = StartCoroutine(DestroyProtocolGameOverRoutine());
        }

        public void DestroyStarFromDestroyProtocol(StarView star)
        {
            if (star == null)
                return;

            planetManager?.DestroyPlanetsForStar(star);
            if (star != null)
                RemoveStarImmediate(star);
        }

        private IEnumerator DestroyProtocolGameOverRoutine()
        {
            _collapseInProgress = true;
            Entropy = MaxEntropy;
            OnEntropyChanged?.Invoke(Entropy);

            foreach (var star in _stars)
            {
                if (star != null)
                    star.SetInputEnabled(false);
            }

            planetManager?.HideAllOrbitLines();

            yield return new WaitForSeconds(0.35f);

            CleanupCollapsedUniverseVisuals();
            FinalizeCollapseUniverse(false);
            _collapseInProgress = false;
            _collapseRoutine = null;
        }

        private void BeginUniverseCollapseSequence(bool manual)
        {
            if (_collapseInProgress || IsCollapsed)
                return;

            _collapseInProgress = true;
            Entropy = MaxEntropy;
            OnEntropyChanged?.Invoke(Entropy);

            foreach (var star in _stars)
            {
                if (star != null)
                    star.SetInputEnabled(false);
            }

            planetManager?.HideAllOrbitLines();

            if (_collapseRoutine != null)
                StopCoroutine(_collapseRoutine);

            _collapseRoutine = StartCoroutine(UniverseCollapseSequenceRoutine(manual));
        }

        private IEnumerator UniverseCollapseSequenceRoutine(bool manual)
        {
            var center = GetUniverseCollapseCenter();
            var balance = Phase3Balance;
            var duration = balance.universeCollapseDuration;
            var pullSpeed = balance.universeCollapsePullSpeed;
            var singularityScale = balance.universeCollapseSingularityScale;

            _collapseSingularity = UniverseCollapseSingularity.Spawn(
                collapseSingularityPrefab,
                worldRoot != null ? worldRoot : transform,
                center,
                singularityScale);

            particleManager?.PullAllToward(_collapseSingularity.transform);

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var easedPull = Mathf.Lerp(pullSpeed * 0.35f, pullSpeed * 1.6f, t * t);

                _collapseSingularity?.SetCollapseProgress(t);
                PullStarsToward(center, easedPull, Time.deltaTime);
                planetManager?.PullAllToward(center, easedPull, Time.deltaTime);
                PullBlackHolesToward(center, easedPull, Time.deltaTime);

                yield return null;
            }

            CleanupCollapsedUniverseVisuals();
            FinalizeCollapseUniverse(manual);
            _collapseInProgress = false;
            _collapseRoutine = null;
        }

        private Vector3 GetUniverseCollapseCenter()
        {
            var sum = Vector3.zero;
            var count = 0;

            foreach (var star in _stars)
            {
                if (star == null)
                    continue;

                sum += star.transform.position;
                count++;
            }

            if (count > 0)
                return sum / count;

            return Vector3.zero;
        }

        private void PullStarsToward(Vector3 center, float speed, float deltaTime)
        {
            foreach (var star in _stars.ToList())
            {
                if (star == null)
                    continue;

                star.transform.position = Vector3.MoveTowards(
                    star.transform.position,
                    center,
                    speed * deltaTime);
                star.transform.localScale = Vector3.Max(
                    star.transform.localScale * (1f - deltaTime * 1.1f),
                    Vector3.one * 0.01f);
            }
        }

        private void PullBlackHolesToward(Vector3 center, float speed, float deltaTime)
        {
            foreach (var hole in _blackHoles.ToList())
            {
                if (hole == null)
                    continue;

                hole.transform.position = Vector3.MoveTowards(
                    hole.transform.position,
                    center,
                    speed * deltaTime);
                hole.transform.localScale = Vector3.Max(
                    hole.transform.localScale * (1f - deltaTime * 1.1f),
                    Vector3.one * 0.01f);
            }
        }

        private void CleanupCollapsedUniverseVisuals()
        {
            if (_collapseSingularity != null)
            {
                Destroy(_collapseSingularity.gameObject);
                _collapseSingularity = null;
            }

            foreach (var star in _stars.ToList())
            {
                if (star != null)
                    Destroy(star.gameObject);
            }

            foreach (var hole in _blackHoles.ToList())
            {
                if (hole != null)
                    Destroy(hole.gameObject);
            }

            _stars.Clear();
            _blackHoles.Clear();
            _supernovaQueue.Clear();
            _centralStar = null;
            SelectedStar = null;
            planetManager?.DestroyAllPlanets();
            particleManager?.ClearAll();
        }

        private void FinalizeCollapseUniverse(bool manual)
        {
            if (IsCollapsed)
                return;

            IsCollapsed = true;
            if (manual)
            {
                IsGameWon = true;
                EndingTitle = "Controlled Collapse";
                EndingText = "The universe did not die by accident.\nIt was ended at the perfect moment, carrying its strongest traits into infinity.";
            }
            else if (!string.IsNullOrWhiteSpace(_pendingCollapseTitle))
            {
                IsGameWon = false;
                EndingTitle = _pendingCollapseTitle;
                EndingText = _pendingCollapseText;
            }

            _pendingCollapseTitle = null;
            _pendingCollapseText = null;

            RunStats.SetFinalEntropy(Entropy);
            LastCollapseBreakdown = CollapseBreakdown.Calculate(RunStats, DnaPotential);
            Prestige.AddDna(LastCollapseBreakdown.TotalGained);
            Prestige.RecordCollapse();

            OnEntropyChanged?.Invoke(Entropy);
            OnUniverseCollapsed?.Invoke(manual);
            OnStateChanged?.Invoke();
        }

        public void ReloadActiveSceneForNewUniverse()
        {
            RunSave.RequestNewGame();
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex);
            else
                SceneManager.LoadScene(activeScene.name);
        }

        private void TickParallelEcho(float deltaTime)
        {
            var rate = PrestigeModifiers.GetParallelEchoPerSecond(Prestige);
            if (rate <= 0)
                return;

            CreditStardust(rate * deltaTime);
        }

        private void TickAutoStarFormation(float deltaTime)
        {
            if (IsRunEnded ||
                !GameplayFeatures.UsesMultiStar(gameplayMode) ||
                Upgrades.AutoStarFormationLevel <= 0)
                return;

            var interval = Mathf.Max(0.1f, Phase3Balance.autoStarFormationCheckInterval);
            _autoStarFormationTimer += deltaTime;
            if (_autoStarFormationTimer < interval)
                return;

            _autoStarFormationTimer = 0f;
            if (!HasOpenStarSlot)
                return;

            var chance = GetAutoStarFormationChanceForLevel(Upgrades.AutoStarFormationLevel);
            if (chance <= 0f || UnityEngine.Random.value >= chance)
                return;

            TryAutoCreateStar();
        }

        public float GetAutoStarFormationChanceForLevel(int level)
        {
            if (level <= 0)
                return 0f;

            return Mathf.Clamp01(level * Phase3Balance.autoStarFormationChancePerLevel);
        }

        private bool TryAutoCreateStar()
        {
            if (!HasOpenStarSlot)
                return false;

            var star = SpawnStar();
            if (star == null)
                return false;

            OnExpansionFeedback?.Invoke("A new star formed automatically.");
            OnStateChanged?.Invoke();
            return true;
        }

        private int SpawnConstellationNear(Vector3 center, int requestedCount)
        {
            var openSlots = Mathf.Max(0, MaxStarSlots - ActiveStarCount);
            var targetCount = Mathf.Clamp(requestedCount, 1, openSlots);
            if (targetCount <= 0)
                return 0;

            var radius = Mathf.Max(0.25f, SingleStarBalance.multiStar.constellationRadius);
            var jitter = Mathf.Max(0f, SingleStarBalance.multiStar.constellationJitter);
            var rotation = UnityEngine.Random.Range(0f, 360f);
            var maxAge = GetMaxStarHealth();
            var spawned = 0;
            StarView first = null;

            for (var i = 0; i < targetCount; i++)
            {
                if (!HasOpenStarSlot)
                    break;

                Vector3 position;
                if (targetCount == 1)
                {
                    position = center;
                }
                else
                {
                    var angle = (rotation + i * (360f / targetCount)) * Mathf.Deg2Rad;
                    var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                    if (jitter > 0f)
                    {
                        offset += new Vector3(
                            UnityEngine.Random.Range(-jitter, jitter),
                            UnityEngine.Random.Range(-jitter, jitter),
                            0f);
                    }

                    position = center + offset;
                }

                // Cycle the three natural star stages: Yellow, Orange, Red Giant.
                var startingAge = GetConstellationStageAge(i, maxAge);
                var star = SpawnStarAt(position, startingAge);
                if (star == null)
                    continue;

                first ??= star;
                spawned++;
            }

            if (first != null)
                SelectStar(first);

            return spawned;
        }

        private static int GetConstellationStageAge(int index, int maxAge)
        {
            maxAge = Mathf.Max(1, maxAge);
            return (index % 3) switch
            {
                0 => 0, // Yellow
                1 => Mathf.RoundToInt(maxAge * 0.4f), // Orange
                _ => Mathf.RoundToInt(maxAge * 0.7f) // Red Giant
            };
        }

        private void SpawnCentralStar()
        {
            var star = SpawnStarAt(Vector3.zero, 0);
            if (star == null)
                return;

            star.SetDriftEnabled(false);
            star.ConfigureMaxAge(SingleStarBalance.maxStarHealth);
            _centralStar = star;
            SelectedStar = star;
            planetManager?.BindHostStar(star);
        }

        private void SpawnStartingMultiStar()
        {
            var star = SpawnStarAt(Vector3.zero, 0);
            if (star == null)
                return;

            ConfigureStarDrift(star, SingleStarBalance.multiStar.moveCentralStarInPhase2);
            star.ConfigureMaxAge(SingleStarBalance.maxStarHealth);
            _centralStar = star;
            SelectedStar = star;
            planetManager?.BindHostStar(star);
        }

        private StarView SpawnStar() => SpawnStarAt(GetRandomSpawnPosition(), 0);

        private StarView SpawnStarAt(Vector3 position, int startingAge)
        {
            if (starViewPrefab == null)
                return null;

            var starId = _nextStarId++;
            var view = Instantiate(starViewPrefab, position, Quaternion.identity, worldRoot);
            view.Bind(this);
            view.ConfigureMaxAge(GetMaxStarHealth());
            view.ConfigureIdentity(starId, SpeciesNaming.GenerateStarName(starId));
            ConfigureStarDrift(view, GameplayFeatures.UsesMultiStar(gameplayMode));
            if (startingAge > 0)
                view.SetAge(startingAge);
            view.RefreshVisual();
            _stars.Add(view);
            if (SelectedStar == null && view.IsInteractable)
                SelectedStar = view;
            RunStats.RecordStarCreated();
            return view;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            for (var attempt = 0; attempt < 24; attempt++)
            {
                var candidate = new Vector3(
                    UnityEngine.Random.Range(GetStarSpawnAreaMin().x, GetStarSpawnAreaMax().x),
                    UnityEngine.Random.Range(GetStarSpawnAreaMin().y, GetStarSpawnAreaMax().y),
                    0f);

                if (IsFarEnoughFromOtherStars(candidate))
                    return candidate;
            }

            return new Vector3(
                UnityEngine.Random.Range(GetStarSpawnAreaMin().x, GetStarSpawnAreaMax().x),
                UnityEngine.Random.Range(GetStarSpawnAreaMin().y, GetStarSpawnAreaMax().y),
                0f);
        }

        private bool IsFarEnoughFromOtherStars(Vector3 position)
        {
            var newStarOrbitRadius = planetManager != null
                ? planetManager.GetEstimatedEmptyStarOrbitFootprintRadius()
                : 0f;

            foreach (var star in _stars)
            {
                if (star == null)
                    continue;

                var existingOrbitRadius = planetManager != null
                    ? planetManager.GetStarOrbitFootprintRadius(star.StarId)
                    : 0f;
                var minDistance = GetMinStarSeparation() + existingOrbitRadius + newStarOrbitRadius;
                if (Vector3.Distance(star.transform.position, position) < minDistance)
                    return false;
            }

            return true;
        }

        private Vector2 GetStarSpawnAreaMin()
        {
            if (GameplayFeatures.UsesUniverseCollapse(gameplayMode))
                return Phase3Balance.spawnAreaMin;

            if (GameplayFeatures.UsesMultiStar(gameplayMode))
                return SingleStarBalance.multiStar.spawnAreaMin;

            return spawnAreaMin;
        }

        private Vector2 GetStarSpawnAreaMax()
        {
            if (GameplayFeatures.UsesUniverseCollapse(gameplayMode))
                return Phase3Balance.spawnAreaMax;

            if (GameplayFeatures.UsesMultiStar(gameplayMode))
                return SingleStarBalance.multiStar.spawnAreaMax;

            return spawnAreaMax;
        }

        private float GetMinStarSeparation()
        {
            if (GameplayFeatures.UsesUniverseCollapse(gameplayMode))
                return Phase3Balance.minStarSeparation;

            if (GameplayFeatures.UsesMultiStar(gameplayMode))
                return SingleStarBalance.multiStar.minStarSeparation;

            return minStarSeparation;
        }

        private void ConfigureStarDrift(StarView star, bool enabled)
        {
            if (star == null)
                return;

            star.SetDriftEnabled(enabled);
            star.ConfigureDriftSpeed(GetEffectiveStarDriftSpeed());
        }

        private void RefreshConfiguredStarDrift()
        {
            foreach (var star in _stars)
            {
                if (star == null)
                    continue;

                star.ConfigureDriftSpeed(GetEffectiveStarDriftSpeed());
            }
        }

        private void AnimatePhase2CameraZoom()
        {
            var camera = Camera.main;
            if (camera == null || !camera.orthographic)
                return;

            if (_cameraZoomRoutine != null)
                StopCoroutine(_cameraZoomRoutine);

            _cameraZoomRoutine = StartCoroutine(AnimateCameraSizeRoutine(camera,
                SingleStarBalance.multiStar.phase2CameraStartSize,
                SingleStarBalance.multiStar.phase2CameraTargetSize,
                SingleStarBalance.multiStar.phase2CameraZoomDuration));
        }

        private void AnimatePhase3CameraZoom()
        {
            var camera = Camera.main;
            if (camera == null || !camera.orthographic)
                return;

            if (_cameraZoomRoutine != null)
                StopCoroutine(_cameraZoomRoutine);

            var config = SingleStarBalance.multiStar;
            _cameraZoomRoutine = StartCoroutine(AnimateCameraSizeRoutine(camera,
                config.phase3CameraStartSize,
                config.phase3CameraTargetSize,
                config.phase3CameraZoomDuration));
        }

        private IEnumerator AnimateCameraSizeRoutine(Camera camera, float startSize, float targetSize, float duration)
        {
            startSize = Mathf.Max(0.1f, startSize);
            targetSize = Mathf.Max(0.1f, targetSize);

            if (duration <= 0f)
            {
                camera.orthographicSize = targetSize;
                _cameraZoomRoutine = null;
                yield break;
            }

            camera.orthographicSize = startSize;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                camera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                yield return null;
            }

            camera.orthographicSize = targetSize;
            _cameraZoomRoutine = null;
        }

        private List<StarView> GetActiveStars()
        {
            _stars.RemoveAll(star => star == null);
            return _stars.Where(star => star.IsInteractable).ToList();
        }
    }
}
