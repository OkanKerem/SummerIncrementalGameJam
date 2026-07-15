using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Universes.Prototype
{
    public class PrototypeGameController : MonoBehaviour
    {
        public const int BaseAgePerClick = 1;
        public const int BaseAgePerPassiveTick = 1;
        public const int BaseSupernovaBonus = 50;
        public const int DefaultMaxStarHealth = 100;
        public const float PassiveTickInterval = 1f;

        [SerializeField] private PrototypeGameplayMode gameplayMode = PrototypeGameplayMode.SingleStarSystemAge;
        [SerializeField] private PrototypeStarView starViewPrefab;
        [SerializeField] private PrototypeBlackHoleView blackHolePrefab;
        [SerializeField] private PrototypePlanetManager planetManager;
        [SerializeField] private PrototypePlanetTypeCatalog planetTypeCatalog;
        [SerializeField] private PrototypeSingleStarBalance singleStarBalance;
        [SerializeField] private Transform worldRoot;
        [SerializeField] private PrototypeFloatingTextSpawner floatingTextSpawner;
        [SerializeField] private PrototypeCosmicParticleManager particleManager;
        [SerializeField] private PrototypeParticleEffectManager effectManager;
        [SerializeField] private PrototypeSfxManager sfxManager;
        [SerializeField] private Vector2 spawnAreaMin = new(-4f, -2.5f);
        [SerializeField] private Vector2 spawnAreaMax = new(4f, 2.5f);
        [SerializeField] private float minStarSeparation = 1.2f;

        public PrototypeUpgrades Upgrades { get; } = new();
        public PrototypePrestigeState Prestige { get; } = new();
        public PrototypeRunStats RunStats { get; } = new();
        public PrototypeCollapseBreakdown LastCollapseBreakdown { get; private set; }
        public PrototypeStarSystemBreakdown LastStarSystemBreakdown { get; private set; }

        public PrototypeFloatingTextSpawner FloatingTextSpawner => floatingTextSpawner;
        public PrototypeCosmicParticleManager ParticleManager => particleManager;
        public PrototypeSfxManager SfxManager => sfxManager;
        public PrototypeSingleStarBalance SingleStarBalance =>
            singleStarBalance != null ? singleStarBalance : _runtimeSingleStarBalance;
        public PrototypeGameplayMode GameplayMode => gameplayMode;
        public bool IsSingleStarMode => PrototypeGameplayFeatures.IsSingleStarMode(gameplayMode);
        public PrototypePlanetManager PlanetManager => planetManager;
        public PrototypeStarView CentralStar => _centralStar;
        public PrototypeStarView SelectedStar { get; private set; }

        public double Stardust { get; private set; }
        public double DnaPotential { get; private set; }
        public int DnaFragments { get; private set; }
        public float Entropy { get; private set; }
        public bool IsCollapsed { get; private set; }
        public bool IsStarSystemEnded { get; private set; }
        public bool IsRunEnded => IsSingleStarMode ? IsStarSystemEnded : IsCollapsed;
        public int ActiveBlackHoleCount => _blackHoles.Count;

        public bool HasActiveStar => GetActiveStars().Count > 0;
        public int ActiveStarCount => GetActiveStars().Count;
        public IReadOnlyList<PrototypeStarView> Stars => _stars;
        public int MaxStarSlots => PrototypeGameplayFeatures.UsesMultiStar(gameplayMode)
            ? Mathf.Clamp(SingleStarBalance.multiStar.initialStarSlots + Upgrades.MaxStarCountLevel, 1,
                SingleStarBalance.multiStar.maxStarSlots)
            : 1;
        public int AbsoluteMaxStarSlots => SingleStarBalance.multiStar.maxStarSlots;
        public bool HasOpenStarSlot => ActiveStarCount < MaxStarSlots && ActiveStarCount < AbsoluteMaxStarSlots;
        public int StarAge => HasActiveStar ? GetActiveStars()[0].StarAge : 0;
        public PrototypeStarStage Stage =>
            HasActiveStar ? GetActiveStars()[0].Stage : PrototypeStarStage.Supernova;

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

        private PrototypeStarView _centralStar;
        private PrototypeSingleStarBalance _runtimeSingleStarBalance;

        private readonly List<PrototypeStarView> _stars = new();
        private readonly List<PrototypeBlackHoleView> _blackHoles = new();
        private readonly Queue<PrototypeStarView> _supernovaQueue = new();
        private int _nextStarId = 1;
        private float _passiveTimer;
        private bool _processingSupernovas;
        private Coroutine _cameraZoomRoutine;

        private void Awake()
        {
            if (singleStarBalance == null)
                _runtimeSingleStarBalance = PrototypeSingleStarBalance.CreateRuntimeDefault();
            else
                singleStarBalance.EnsureNestedConfigs();

            EnsureWorldRoot();
            EnsureManagersInitialized();
        }

        private void Start()
        {
            if (floatingTextSpawner == null)
                floatingTextSpawner = FindAnyObjectByType<PrototypeFloatingTextSpawner>();

            PrototypePrestigeSave.Load(Prestige);
            ResetToPhase1StartupState();
            EnsureCameraDragPan();

            if (IsSingleStarMode)
                StartNewStarSystem();
            else
                StartNewUniverse();
        }

        private void ResetToPhase1StartupState()
        {
            gameplayMode = PrototypeGameplayMode.SingleStarSystemAge;
            PrototypeExpansionSave.SetExpanded(false);
            Entropy = 0f;
            IsCollapsed = false;
            IsStarSystemEnded = false;

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

            var dragPan = camera.GetComponent<PrototypeCameraDragPan>() ??
                          camera.gameObject.AddComponent<PrototypeCameraDragPan>();
            dragPan.Configure(
                SingleStarBalance.cameraDragEnabled,
                SingleStarBalance.cameraDragMouseButton,
                SingleStarBalance.cameraDragSpeed);
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
                particleManager = GetComponent<PrototypeCosmicParticleManager>();
                if (particleManager == null)
                    particleManager = gameObject.AddComponent<PrototypeCosmicParticleManager>();
            }

            if (effectManager == null)
            {
                effectManager = GetComponent<PrototypeParticleEffectManager>();
                if (effectManager == null)
                    effectManager = gameObject.AddComponent<PrototypeParticleEffectManager>();
            }

            if (sfxManager == null)
            {
                sfxManager = GetComponent<PrototypeSfxManager>();
                if (sfxManager == null)
                    sfxManager = gameObject.AddComponent<PrototypeSfxManager>();
            }

            if (planetManager == null)
            {
                planetManager = GetComponent<PrototypePlanetManager>();
                if (planetManager == null)
                    planetManager = gameObject.AddComponent<PrototypePlanetManager>();
            }

            planetManager.Initialize(this, worldRoot, planetTypeCatalog);
        }

        private void Update()
        {
            if (IsRunEnded)
                return;

            RunStats.SurvivalTimeSeconds += Time.deltaTime;

            if (IsSingleStarMode)
            {
                planetManager?.Tick(Time.deltaTime);
            }
            else if (PrototypeGameplayFeatures.UsesMultiStar(gameplayMode) &&
                     !PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode))
            {
                TickEntropy(Time.deltaTime);
                TickConfiguredStarDrift(Time.deltaTime);
                planetManager?.Tick(Time.deltaTime);
            }
            else
            {
                TickEntropy(Time.deltaTime);
                TryVacuumParticlesOnClick();
                particleManager?.TickBlackHolePull(_blackHoles);
                TickStarDriftAndCollisions();
                TickParallelEcho(Time.deltaTime);
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

        public int GetClickReward(PrototypeStarView star)
        {
            if (IsRunEnded || star == null || !star.IsInteractable)
                return 0;

            if (IsSingleStarMode)
                return ApplyClickRewardMultiplier(SingleStarBalance.baseClickReward +
                                                  Upgrades.GetClickPowerBonus(SingleStarBalance.upgrades));

            var planetClickBonus = PrototypeGameplayFeatures.UsesMultiStar(gameplayMode)
                ? GetPlanetCountForStar(star) *
                  Upgrades.StarPlanetClickValueLevel *
                  SingleStarBalance.multiStar.starClickValuePerPlanetPerUpgradeLevel
                : 0f;
            var mult = GetProductionMultiplier();
            return ApplyClickRewardMultiplier((PrototypeStarStageUtility.GetClickReward(star.Stage) +
                                               Upgrades.GetClickPowerBonus(SingleStarBalance.upgrades) +
                                               planetClickBonus) * mult);
        }

        private int ApplyClickRewardMultiplier(float reward)
        {
            var percentPerLevel = SingleStarBalance.upgrades.clickRewardPercentPerLevel;
            var percentMultiplier = Mathf.Pow(1f + percentPerLevel, Upgrades.ClickPowerPercentLevel);
            return Mathf.RoundToInt(reward * percentMultiplier);
        }

        public int GetPassivePerSecond(PrototypeStarView star)
        {
            if (IsRunEnded || star == null || !star.IsInteractable)
                return 0;

            var percentMultiplier = Mathf.Pow(
                1f + SingleStarBalance.upgrades.starPassiveProductionPercentPerLevel,
                Upgrades.StarPassiveProductionPercentLevel);
            if (IsSingleStarMode)
                return Mathf.RoundToInt((SingleStarBalance.basePassivePerSecond +
                                         Upgrades.GetPassiveProductionBonus(SingleStarBalance.upgrades)) *
                                        percentMultiplier);

            var mult = GetProductionMultiplier();
            return Mathf.RoundToInt((PrototypeStarStageUtility.GetPassivePerSecond(star.Stage) +
                                     Upgrades.GetPassiveProductionBonus(SingleStarBalance.upgrades)) *
                                    mult *
                                    percentMultiplier);
        }

        public float GetPassiveTickInterval() =>
            PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? PassiveTickInterval
                : SingleStarBalance.passiveTickInterval;

        public int GetAgePerClick() =>
            PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? BaseAgePerClick
                : SingleStarBalance.agePerClick;

        public int GetAgePerPassiveTick() =>
            PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? BaseAgePerPassiveTick
                : SingleStarBalance.agePerPassiveTick;

        public int GetMaxStarHealth() =>
            PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? DefaultMaxStarHealth
                : SingleStarBalance.maxStarHealth;

        public int GetTotalPassivePerSecond()
        {
            var total = 0;
            foreach (var star in GetActiveStars())
            {
                total += GetPassivePerSecond(star);
                if (planetManager != null && PrototypeGameplayFeatures.UsesMultiStar(gameplayMode))
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

        public bool CanCreateNewStar(out string reason)
        {
            reason = string.Empty;

            if (!PrototypeGameplayFeatures.UsesMultiStar(gameplayMode))
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

            if (!MeetsStarCreationRequirement(out reason))
                return false;

            var cost = GetCreateStarCost();
            if (Stardust < cost)
            {
                reason = $"Needs {cost:0} Stardust";
                return false;
            }

            return true;
        }

        private bool MeetsStarCreationRequirement(out string reason)
        {
            reason = string.Empty;
            var nextStarNumber = RunStats.StarsCreated + 1;
            if (nextStarNumber <= 2)
                return true;

            var requiredMaxStarLevel = nextStarNumber - 1;
            if (Upgrades.MaxStarCountLevel >= requiredMaxStarLevel)
                return true;

            reason = $"Requires Max Star Count Level {requiredMaxStarLevel}";
            return false;
        }

        public int GetPlanetCountForStar(PrototypeStarView star) =>
            planetManager != null && star != null ? planetManager.GetPlanetCountForStar(star.StarId) : 0;

        public PrototypeStarView GetStarById(int starId)
        {
            foreach (var star in _stars)
            {
                if (star != null && star.StarId == starId)
                    return star;
            }

            return null;
        }

        public void SelectStar(PrototypeStarView star)
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
            PrototypePrestigeModifiers.GetAgeGainMultiplier(Prestige);

        public float GetEffectiveAgeGainMultiplier(PrototypeStarView star)
        {
            var multiplier = GetEffectiveAgeGainMultiplier();
            if (star != null && PrototypeGameplayFeatures.UsesMultiStar(gameplayMode) && planetManager != null)
                multiplier *= planetManager.GetStarAgeBurdenMultiplier(star.StarId);
            return multiplier;
        }

        public float GetEntropyGainMultiplier() => PrototypePrestigeModifiers.GetEntropyMultiplier(Prestige);

        public float GetProductionMultiplier() => PrototypePrestigeModifiers.GetProductionMultiplier(Prestige);

        public float GetDoubleStardustChance() =>
            PrototypePrestigeModifiers.GetDoubleStardustChance(Prestige);

        public float GetBlackHoleDnaMultiplier() =>
            PrototypePrestigeModifiers.GetBlackHoleDnaMultiplier(Prestige);

        public bool TryPurchasePrestigeUpgrade(PrototypePrestigeUpgradeDefinition definition)
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

            var stage = HasActiveStar ? Stage : PrototypeStarStage.Yellow;
            effectManager?.PlayCollectAreaEffect(worldPosition, collectRadius, stage);
        }

        private static bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
                return false;

            return EventSystem.current.IsPointerOverGameObject();
        }

        public bool TryPurchaseUpgrade(PrototypeUpgradeDefinition definition)
        {
            if (IsRunEnded || definition == null)
                return false;

            if (definition.upgradeType == PrototypeUpgradeType.ExpandUniverse)
                return TryExpandUniverse(definition);

            if (!definition.ArePrerequisitesMet(this, out _))
                return false;

            var balance = Stardust;
            if (!Upgrades.TryPurchase(definition, ref balance))
                return false;

            Stardust = balance;
            OnStateChanged?.Invoke();
            return true;
        }

        public bool TryExpandUniverse(PrototypeUpgradeDefinition definition = null)
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

        private void ApplyUniverseExpansion()
        {
            PrototypeExpansionSave.SetExpanded(true);
            gameplayMode = PrototypeGameplayMode.MultiStarSystemAge;
            PromoteCurrentStarSystemToMultiStar();
            AnimatePhase2CameraZoom();
            OnExpansionFeedback?.Invoke("The system expands! Welcome to Step 2: Multi-Star System Age.");
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

        public void OnStarClicked(PrototypeStarView star)
        {
            if (IsRunEnded || star == null || !star.IsInteractable)
                return;

            if (PrototypeGameplayFeatures.UsesMultiStar(gameplayMode))
                SelectStar(star);

            var stage = star.Stage;
            var reward = GetClickReward(star);

            sfxManager?.PlayStarClick();
            effectManager?.PlayClickEffect(star.transform.position, stage);

            double creditedReward;
            if (IsSingleStarMode)
                creditedReward = CreditStardustDirect(reward);
            else if (PrototypeGameplayFeatures.UsesParticleVacuum(gameplayMode))
            {
                EmitStardust(reward, star.transform.position, stage);
                creditedReward = reward;
            }
            else
                creditedReward = CreditStardustDirect(reward);

            floatingTextSpawner?.Spawn(star.transform.position, Mathf.RoundToInt((float)creditedReward), stage);
            RunStats.RecordClick();

            star.AddAge(GetAgePerClick(), GetEffectiveAgeGainMultiplier(star));

            if (PrototypeGameplayFeatures.UsesEntropy(gameplayMode))
                AddEntropy(PrototypeEntropyBalance.EntropyPerClick);

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

            if (PrototypeGameplayFeatures.UsesEntropy(gameplayMode))
                AddEntropy((float)(amount * PrototypeEntropyBalance.EntropyPerStardustProduced));

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
            if (IsRunEnded || PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode) ||
                particleManager == null || amount <= 0f)
                return;

            particleManager.SpawnDnaPotential(position, amount);
        }

        public float GetCollisionDnaPotential(float baseAmount) =>
            Mathf.Max(0f, baseAmount +
                          Upgrades.CollisionDnaProductionLevel *
                          SingleStarBalance.upgrades.collisionDnaPotentialPerLevel);

        public float GetSpeciesDnaPotentialPerTick(PrototypePlanet planet)
        {
            if (planet == null || !planet.HasSpecies || Upgrades.SpeciesDnaProductionLevel <= 0)
                return 0f;

            var speciesMultiplier = SingleStarBalance.species.GetDnaPotentialMultiplier(
                planet.Intelligence,
                planet.Aggression);
            return Upgrades.SpeciesDnaProductionLevel *
                   SingleStarBalance.upgrades.speciesDnaPotentialPerLevel *
                   speciesMultiplier;
        }

        public int GetPlanetClickReward(PrototypePlanet planet)
        {
            if (planet == null || !planet.IsAlive)
                return 0;

            var definition = planet.Definition;
            var baseReward = (definition != null
                                 ? definition.baseClickValue
                                 : SingleStarBalance.planets.fallbackBaseClickReward) +
                             Upgrades.PlanetClickValueLevel *
                             (IsSingleStarMode
                                 ? SingleStarBalance.planetClickValuePerLevel
                                 : PrototypePlanetBalance.PlanetClickValuePerLevel);
            var civilizationMultiplier =
                SingleStarBalance.civilization.GetStageClickBonusMultiplier(planet.CivilizationStage);
            var speciesMultiplier = planet.HasSpecies
                ? SingleStarBalance.species.GetClickStardustMultiplier(planet.Intelligence, planet.Aggression)
                : 1f;

            return Mathf.RoundToInt((float)baseReward * civilizationMultiplier * speciesMultiplier);
        }

        public float GetPlanetClickDamage() =>
            IsSingleStarMode
                ? SingleStarBalance.basePlanetClickDamage
                : PrototypePlanetBalance.BasePlanetClickDamage;

        public void NotifyStateChanged() => OnStateChanged?.Invoke();

        public bool TryCreatePlanet()
        {
            if (planetManager == null)
                return false;

            if ((SelectedStar == null || !SelectedStar.IsInteractable) && HasActiveStar)
                SelectStar(GetActiveStars()[0]);

            return planetManager.TryCreatePlanet();
        }

        public void StartNewStarSystem() => StartNewRun(resetPrestigeBonuses: false);

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

        public void AddBlackHoleDnaPotential(float amount) =>
            RunStats.AddBlackHoleDnaPotential(amount);

        public void OnParticleConsumedByBlackHole(int particleValue, Transform blackHole)
        {
            if (IsCollapsed)
                return;

            CreditStardust(particleValue * 0.5);
            if (UnityEngine.Random.value < PrototypeCosmicBalance.BlackHoleConsumeDnaChance)
                TrySpawnDnaFragment(blackHole.position);
        }

        public void AddEntropy(float amount)
        {
            if (!PrototypeGameplayFeatures.UsesEntropy(gameplayMode) || IsCollapsed || amount <= 0f)
                return;

            var upgradeReduction = Mathf.Clamp01(
                Upgrades.EntropyReductionLevel * SingleStarBalance.upgrades.entropyReductionPerLevel);
            Entropy = Mathf.Min(100f, Entropy + amount * GetEntropyGainMultiplier() * (1f - upgradeReduction));
            OnEntropyChanged?.Invoke(Entropy);

            if (Entropy >= 100f && PrototypeGameplayFeatures.UsesEntropy(gameplayMode))
                CollapseUniverse(false);
        }

        public void ClampStarPosition(PrototypeStarView star) =>
            star?.TickDrift(0f, GetStarSpawnAreaMin(), GetStarSpawnAreaMax());

        private void TickPassiveProduction()
        {
            if (IsRunEnded)
                return;

            var changed = false;
            foreach (var star in GetActiveStars().ToList())
            {
                var reward = GetPassivePerSecond(star);
                var planetReward = planetManager != null && PrototypeGameplayFeatures.UsesMultiStar(gameplayMode)
                    ? Mathf.RoundToInt(planetManager.GetPassiveStardustForStar(star.StarId))
                    : 0;
                var totalReward = reward + planetReward;
                if (totalReward <= 0)
                    continue;

                if (IsSingleStarMode || !PrototypeGameplayFeatures.UsesParticleVacuum(gameplayMode))
                    CreditStardustDirect(totalReward);
                else
                    EmitStardust(totalReward, star.transform.position, star.Stage);

                star.AddAge(GetAgePerPassiveTick(), GetEffectiveAgeGainMultiplier(star));

                if (PrototypeGameplayFeatures.UsesEntropy(gameplayMode))
                    AddEntropy(PrototypeEntropyBalance.EntropyPerPassiveTick);

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
            var passiveRate = GetTotalPassivePerSecond();
            var rate = PrototypeEntropyBalance.BaseEntropyPerSecond +
                       passiveRate * PrototypeEntropyBalance.EntropyFromPassiveRate;
            if (PrototypeGameplayFeatures.UsesMultiStar(gameplayMode) &&
                !PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode))
            {
                rate += ActiveStarCount * SingleStarBalance.multiStar.entropyPerActiveStarPerSecond;
                rate += (planetManager?.PlanetCount ?? 0) * SingleStarBalance.multiStar.entropyPerPlanetPerSecond;
            }
            AddEntropy(rate * deltaTime);
        }

        private void EmitStardust(int amount, Vector3 position, PrototypeStarStage stage, bool playEmitVfx = true)
        {
            if (amount <= 0)
                return;

            if (playEmitVfx)
                effectManager?.PlayStardustEmitEffect(position, stage);

            if (particleManager != null)
                particleManager.EmitStardustBurst(amount, position, stage);
            else
                CreditStardust(amount);
        }

        public void QueueSupernova(PrototypeStarView star) => EnqueueSupernova(star);

        private void EnqueueSupernova(PrototypeStarView star)
        {
            if (star == null || !star.BeginSupernova())
                return;

            _supernovaQueue.Enqueue(star);
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

        private void ExecuteSupernova(PrototypeStarView star)
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

            if (PrototypeGameplayFeatures.UsesMultiStar(gameplayMode) &&
                !PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode))
            {
                CreditStardustDirect(SingleStarBalance.multiStar.supernovaStardustBonus +
                                     Upgrades.SupernovaBonusLevel * SingleStarBalance.upgrades.supernovaBonusPerLevel);
                AddEntropy(PrototypeEntropyBalance.EntropyPerSupernova);
                planetManager?.DestroyPlanetsForStar(star);
                ApplySupernovaAreaEffect(position, star);
                OnSupernova?.Invoke();
                NotifyStateChanged();
                star.PlaySupernovaEffect();
                return;
            }

            var bonus = GetSupernovaBonus() + PrototypeCosmicBalance.SupernovaParticleBurst;
            EmitStardust(bonus, position, PrototypeStarStage.Supernova, playEmitVfx: false);
            AddEntropy(PrototypeEntropyBalance.EntropyPerSupernova);
            ApplySupernovaAreaEffect(position, star);

            if (UnityEngine.Random.value < PrototypeCosmicBalance.SupernovaDnaChance +
                PrototypePrestigeModifiers.GetSupernovaDnaChanceBonus(Prestige))
                TrySpawnDnaFragment(position);

            if (UnityEngine.Random.value < PrototypeCosmicBalance.SupernovaBlackHoleChance)
                TrySpawnBlackHole(position);

            OnSupernova?.Invoke();
            NotifyStateChanged();
            star.PlaySupernovaEffect();
        }

        private void ApplySupernovaAreaEffect(Vector3 origin, PrototypeStarView source)
        {
            foreach (var other in GetActiveStars().ToList())
            {
                if (other == null || other == source || !other.IsInteractable)
                    continue;

                var radius = PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode)
                    ? PrototypeCosmicBalance.SupernovaRadius
                    : SingleStarBalance.multiStar.supernovaNearbyStarRadius;
                if (Vector3.Distance(other.transform.position, origin) > radius)
                    continue;

                var damage = PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode)
                    ? PrototypeCosmicBalance.SupernovaAgeBurst
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
                        PrototypeCosmicBalance.StarCollisionDistance)
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

        private void HandleStarCollision(PrototypeStarView a, PrototypeStarView b)
        {
            if (a == null || b == null || !a.IsInteractable || !b.IsInteractable)
                return;

            var midpoint = (a.transform.position + b.transform.position) * 0.5f;
            var mergedAge = Mathf.Clamp((a.StarAge + b.StarAge) / 2 + (int)PrototypeCosmicBalance.CollisionAgeBonus, 0, 100);

            RemoveStarImmediate(a);
            RemoveStarImmediate(b);

            EmitStardust((int)PrototypeCosmicBalance.CollisionStardustBurst +
                         PrototypeCosmicBalance.CollisionParticleBurst, midpoint, PrototypeStarStage.RedGiant);

            AddEntropy(PrototypeEntropyBalance.EntropyPerCollision);
            RunStats.RecordCollision();

            if (UnityEngine.Random.value < PrototypeCosmicBalance.CollisionDnaChance)
                TrySpawnDnaFragment(midpoint);

            var collisionDnaPotential = GetCollisionDnaPotential(0f);
            if (collisionDnaPotential > 0f)
                TrySpawnDnaPotential(midpoint, collisionDnaPotential);

            if (UnityEngine.Random.value < PrototypeCosmicBalance.CollisionBlackHoleChance)
                TrySpawnBlackHole(midpoint);

            var merged = SpawnStarAt(midpoint, mergedAge);
            if (merged != null && merged.StarAge >= 100)
                EnqueueSupernova(merged);

            OnStarCollision?.Invoke();
            OnStateChanged?.Invoke();
        }

        private void TrySpawnBlackHole(Vector3 position)
        {
            if (IsCollapsed)
                return;

            PrototypeBlackHoleView hole;
            if (blackHolePrefab != null)
                hole = Instantiate(blackHolePrefab, position, Quaternion.identity, worldRoot);
            else
            {
                var go = new GameObject("BlackHole");
                go.transform.SetParent(worldRoot);
                hole = go.AddComponent<PrototypeBlackHoleView>();
            }

            hole.Init(this, position);
            _blackHoles.Add(hole);
            RunStats.RecordBlackHole();
            OnBlackHoleSpawned?.Invoke();
            OnStateChanged?.Invoke();
        }

        public void RemoveBlackHole(PrototypeBlackHoleView hole)
        {
            if (hole == null)
                return;

            _blackHoles.Remove(hole);
            Destroy(hole.gameObject);
            OnStateChanged?.Invoke();
        }

        public bool TryCreateNewStar()
        {
            if (!CanCreateNewStar(out _))
                return false;

            Stardust -= GetCreateStarCost();
            var star = SpawnStar();
            if (star != null)
                SelectStar(star);
            OnStateChanged?.Invoke();
            return true;
        }

        public void TryCollapseUniverse()
        {
            if (!IsCollapsed)
                CollapseUniverse(true);
        }

        public void StartNewUniverse() => StartNewRun(resetPrestigeBonuses: true);

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
            particleManager?.ClearAll();
            planetManager?.ResetAll();

            Stardust = PrototypePrestigeModifiers.GetStartingStardust(Prestige);
            DnaPotential = 0;
            DnaFragments = 0;
            Entropy = 0;
            IsCollapsed = false;
            IsStarSystemEnded = false;
            _passiveTimer = 0f;
            if (resetUpgrades)
                Upgrades.Reset();
            RunStats.Reset();
            LastCollapseBreakdown = null;
            LastStarSystemBreakdown = null;

            OnEntropyChanged?.Invoke(Entropy);

            if (IsSingleStarMode)
                SpawnCentralStar();
            else if (PrototypeGameplayFeatures.UsesMultiStar(gameplayMode) &&
                     !PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode))
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
            LastStarSystemBreakdown = PrototypeStarSystemBreakdown.Calculate(RunStats, DnaPotential, civRank);
            Prestige.AddDna(LastStarSystemBreakdown.TotalGained);

            OnStarSystemEnded?.Invoke();
            NotifyStateChanged();
        }

        public void OnStarRemoved(PrototypeStarView star)
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

        private void RemoveStarImmediate(PrototypeStarView star)
        {
            if (star == null)
                return;

            _stars.Remove(star);
            Destroy(star.gameObject);
        }

        private void CollapseUniverse(bool manual)
        {
            if (IsCollapsed)
                return;

            IsCollapsed = true;
            Entropy = 100f;
            RunStats.SetFinalEntropy(Entropy);

            LastCollapseBreakdown = PrototypeCollapseBreakdown.Calculate(RunStats);
            Prestige.AddDna(LastCollapseBreakdown.TotalGained);
            Prestige.RecordCollapse();

            foreach (var star in _stars)
            {
                if (star != null)
                    star.SetInputEnabled(false);
            }

            OnEntropyChanged?.Invoke(Entropy);
            OnUniverseCollapsed?.Invoke(manual);
            OnStateChanged?.Invoke();
        }

        public void ReloadActiveSceneForNewUniverse()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex);
            else
                SceneManager.LoadScene(activeScene.name);
        }

        private void TickParallelEcho(float deltaTime)
        {
            var rate = PrototypePrestigeModifiers.GetParallelEchoPerSecond(Prestige);
            if (rate <= 0)
                return;

            CreditStardust(rate * deltaTime);
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

        private PrototypeStarView SpawnStar() => SpawnStarAt(GetRandomSpawnPosition(), 0);

        private PrototypeStarView SpawnStarAt(Vector3 position, int startingAge)
        {
            if (starViewPrefab == null)
                return null;

            var starId = _nextStarId++;
            var view = Instantiate(starViewPrefab, position, Quaternion.identity, worldRoot);
            view.Bind(this);
            view.ConfigureMaxAge(GetMaxStarHealth());
            view.ConfigureIdentity(starId, PrototypeSpeciesNaming.GenerateStarName(starId));
            ConfigureStarDrift(view, PrototypeGameplayFeatures.UsesMultiStar(gameplayMode));
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
            foreach (var star in _stars)
            {
                if (star == null)
                    continue;

                if (Vector3.Distance(star.transform.position, position) < GetMinStarSeparation())
                    return false;
            }

            return true;
        }

        private Vector2 GetStarSpawnAreaMin() =>
            PrototypeGameplayFeatures.UsesMultiStar(gameplayMode) && !PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? SingleStarBalance.multiStar.spawnAreaMin
                : spawnAreaMin;

        private Vector2 GetStarSpawnAreaMax() =>
            PrototypeGameplayFeatures.UsesMultiStar(gameplayMode) && !PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? SingleStarBalance.multiStar.spawnAreaMax
                : spawnAreaMax;

        private float GetMinStarSeparation() =>
            PrototypeGameplayFeatures.UsesMultiStar(gameplayMode) && !PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? SingleStarBalance.multiStar.minStarSeparation
                : minStarSeparation;

        private void ConfigureStarDrift(PrototypeStarView star, bool enabled)
        {
            if (star == null)
                return;

            star.SetDriftEnabled(enabled);
            star.ConfigureDriftSpeed(PrototypeGameplayFeatures.UsesMultiStar(gameplayMode) &&
                                     !PrototypeGameplayFeatures.UsesUniverseCollapse(gameplayMode)
                ? SingleStarBalance.multiStar.starDriftSpeed
                : PrototypeCosmicBalance.StarDriftSpeed);
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

        private List<PrototypeStarView> GetActiveStars()
        {
            _stars.RemoveAll(star => star == null);
            return _stars.Where(star => star.IsInteractable).ToList();
        }
    }

    public class PrototypeBlackHoleView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer coreRenderer;
        [SerializeField] private float spinSpeed = 90f;

        private PrototypeGameController _controller;
        private float _lifetime;
        private float _dnaTimer;
        private float _age;

        public float DnaPotential { get; private set; }
        public float Age => _age;

        public void Init(PrototypeGameController controller, Vector3 position)
        {
            _controller = controller;
            transform.position = position;
            _lifetime = PrototypeCosmicBalance.BlackHoleLifetimeSeconds;
            _dnaTimer = PrototypeCosmicBalance.BlackHoleDnaIntervalSeconds *
                        PrototypeCosmicBalance.BlackHoleInitialDnaTimerMultiplier;
            _age = 0f;
            DnaPotential = 0f;

            if (coreRenderer == null)
            {
                coreRenderer = gameObject.AddComponent<SpriteRenderer>();
                coreRenderer.sprite = CreateDiscSprite();
                coreRenderer.color = new Color(0.15f, 0.05f, 0.25f, 0.95f);
                coreRenderer.sortingOrder = 15;
            }

            transform.localScale = Vector3.one * 0.55f;
        }

        private void Update()
        {
            if (_controller == null || _controller.IsCollapsed)
                return;

            _age += Time.deltaTime;
            _lifetime -= Time.deltaTime;
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

            var pulse = 1f + Mathf.Sin(_age * 4f) * 0.06f;
            transform.localScale = Vector3.one * (0.55f * pulse);

            _controller.AddEntropy(PrototypeCosmicBalance.BlackHoleEntropyPerSecond * Time.deltaTime);

            _dnaTimer -= Time.deltaTime;
            if (_dnaTimer <= 0f)
            {
                _dnaTimer = PrototypeCosmicBalance.BlackHoleDnaIntervalSeconds;
                DnaPotential += 1f;
                _controller.AddBlackHoleDnaPotential(_controller.GetBlackHoleDnaMultiplier());

                if (UnityEngine.Random.value < PrototypeCosmicBalance.BlackHoleDnaFragmentChance)
                    _controller.TrySpawnDnaFragment(transform.position);
            }

            if (_lifetime <= 0f)
                _controller.RemoveBlackHole(this);
        }

        private static Sprite CreateDiscSprite()
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / (size * 0.5f);
                    var ring = Mathf.Clamp01(1f - Mathf.Abs(dist - 0.65f) * 6f);
                    var core = dist < 0.35f ? 1f : 0f;
                    var alpha = Mathf.Max(ring * 0.85f, core);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
