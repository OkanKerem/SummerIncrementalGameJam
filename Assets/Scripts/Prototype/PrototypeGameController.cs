using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Universes.Prototype
{
    public class PrototypeGameController : MonoBehaviour
    {
        public const int BaseAgePerClick = 1;
        public const int BaseAgePerPassiveTick = 1;
        public const int BaseSupernovaBonus = 50;
        public const int CreateStarCost = 20;
        public const float PassiveTickInterval = 1f;

        [SerializeField] private PrototypeStarView starViewPrefab;
        [SerializeField] private PrototypeBlackHoleView blackHolePrefab;
        [SerializeField] private Transform worldRoot;
        [SerializeField] private PrototypeFloatingTextSpawner floatingTextSpawner;
        [SerializeField] private PrototypeCosmicParticleManager particleManager;
        [SerializeField] private PrototypeParticleEffectManager effectManager;
        [SerializeField] private Vector2 spawnAreaMin = new(-4f, -2.5f);
        [SerializeField] private Vector2 spawnAreaMax = new(4f, 2.5f);
        [SerializeField] private float minStarSeparation = 1.2f;

        public PrototypeUpgrades Upgrades { get; } = new();
        public PrototypePrestigeState Prestige { get; } = new();
        public PrototypeRunStats RunStats { get; } = new();
        public PrototypeCollapseBreakdown LastCollapseBreakdown { get; private set; }

        public double Stardust { get; private set; }
        public int DnaFragments { get; private set; }
        public float Entropy { get; private set; }
        public bool IsCollapsed { get; private set; }
        public int ActiveBlackHoleCount => _blackHoles.Count;

        public bool HasActiveStar => GetActiveStars().Count > 0;
        public int ActiveStarCount => GetActiveStars().Count;
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

        private readonly List<PrototypeStarView> _stars = new();
        private readonly List<PrototypeBlackHoleView> _blackHoles = new();
        private readonly Queue<PrototypeStarView> _supernovaQueue = new();
        private float _passiveTimer;
        private bool _processingSupernovas;

        private void Start()
        {
            if (worldRoot == null)
            {
                var root = new GameObject("WorldRoot");
                worldRoot = root.transform;
            }

            if (floatingTextSpawner == null)
                floatingTextSpawner = FindAnyObjectByType<PrototypeFloatingTextSpawner>();

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

            PrototypePrestigeSave.Load(Prestige);
            StartNewUniverse();
        }

        private void Update()
        {
            if (IsCollapsed)
                return;

            RunStats.SurvivalTimeSeconds += Time.deltaTime;
            TickEntropy(Time.deltaTime);
            TryVacuumParticlesOnClick();
            particleManager?.TickBlackHolePull(_blackHoles);

            TickStarDriftAndCollisions();
            TickParallelEcho(Time.deltaTime);

            _passiveTimer += Time.deltaTime;
            while (_passiveTimer >= PassiveTickInterval)
            {
                _passiveTimer -= PassiveTickInterval;
                TickPassiveProduction();
            }

            ProcessSupernovaQueue();
        }

        public int GetClickReward(PrototypeStarView star)
        {
            if (IsCollapsed || star == null || !star.IsInteractable)
                return 0;

            return Mathf.RoundToInt((PrototypeStarStageUtility.GetClickReward(star.Stage) + Upgrades.ClickPowerLevel) *
                                    GetProductionMultiplier());
        }

        public int GetPassivePerSecond(PrototypeStarView star)
        {
            if (IsCollapsed || star == null || !star.IsInteractable)
                return 0;

            return Mathf.RoundToInt((PrototypeStarStageUtility.GetPassivePerSecond(star.Stage) +
                                     Upgrades.PassiveProductionLevel) * GetProductionMultiplier());
        }

        public int GetTotalPassivePerSecond()
        {
            var total = 0;
            foreach (var star in GetActiveStars())
                total += GetPassivePerSecond(star);
            return total;
        }

        public int GetClickReward() =>
            HasActiveStar ? GetClickReward(GetActiveStars()[0]) : 0;

        public int GetPassivePerSecond() => GetTotalPassivePerSecond();

        public int GetSupernovaBonus() =>
            BaseSupernovaBonus + Upgrades.SupernovaBonusLevel * PrototypeUpgrades.SupernovaBonusPerLevel;

        public float GetClickCollectRadius() => Upgrades.GetClickCollectRadius();

        public float GetEffectiveAgeGainMultiplier() =>
            Upgrades.GetAgeGainMultiplier() * PrototypePrestigeModifiers.GetAgeGainMultiplier(Prestige);

        public float GetEntropyGainMultiplier() => PrototypePrestigeModifiers.GetEntropyMultiplier(Prestige);

        public float GetProductionMultiplier() => PrototypePrestigeModifiers.GetProductionMultiplier(Prestige);

        public float GetParticleEvolutionChance() =>
            PrototypePrestigeModifiers.GetParticleEvolutionChance(Prestige);

        public float GetBlackHoleDnaMultiplier() =>
            PrototypePrestigeModifiers.GetBlackHoleDnaMultiplier(Prestige);

        public bool TryPurchasePrestigeUpgrade(PrototypePrestigeUpgradeDefinition definition)
        {
            if (!IsCollapsed || definition == null)
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
            if (IsCollapsed || definition == null)
                return false;

            var balance = Stardust;
            if (!Upgrades.TryPurchase(definition, ref balance))
                return false;

            Stardust = balance;
            OnStateChanged?.Invoke();
            return true;
        }

        public void OnStarClicked(PrototypeStarView star)
        {
            if (IsCollapsed || star == null || !star.IsInteractable)
                return;

            var stage = star.Stage;
            var reward = GetClickReward(star);

            effectManager?.PlayClickEffect(star.transform.position, stage);
            EmitStardust(reward, star.transform.position, stage);
            floatingTextSpawner?.Spawn(star.transform.position, reward, stage);
            RunStats.RecordClick();

            star.AddAge(BaseAgePerClick, GetEffectiveAgeGainMultiplier());
            AddEntropy(PrototypeEntropyBalance.EntropyPerClick);
            star.RefreshVisual();
            star.PlayClickPop();

            if (star.StarAge >= 100)
                EnqueueSupernova(star);
            else
                OnStateChanged?.Invoke();
        }

        public void CreditStardust(double amount)
        {
            if (IsCollapsed || amount <= 0)
                return;

            Stardust += amount;
            RunStats.RecordStardustProduced(amount);
            AddEntropy((float)(amount * PrototypeEntropyBalance.EntropyPerStardustProduced));
            OnStardustGained?.Invoke((int)amount);
            OnStateChanged?.Invoke();
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
            if (IsCollapsed || amount <= 0f)
                return;

            Entropy = Mathf.Min(100f, Entropy + amount * GetEntropyGainMultiplier());
            OnEntropyChanged?.Invoke(Entropy);

            if (Entropy >= 100f)
                CollapseUniverse(false);
        }

        public void ClampStarPosition(PrototypeStarView star) =>
            star?.TickDrift(0f, spawnAreaMin, spawnAreaMax);

        private void TickPassiveProduction()
        {
            if (IsCollapsed)
                return;

            var changed = false;
            foreach (var star in GetActiveStars().ToList())
            {
                var reward = GetPassivePerSecond(star);
                if (reward <= 0)
                    continue;

                EmitStardust(reward, star.transform.position, star.Stage);
                star.AddAge(BaseAgePerPassiveTick, GetEffectiveAgeGainMultiplier());
                AddEntropy(PrototypeEntropyBalance.EntropyPerPassiveTick);
                star.RefreshVisual();
                changed = true;

                if (star.StarAge >= 100)
                    EnqueueSupernova(star);
            }

            if (changed)
                OnStateChanged?.Invoke();
        }

        private void TickEntropy(float deltaTime)
        {
            var passiveRate = GetTotalPassivePerSecond();
            var rate = PrototypeEntropyBalance.BaseEntropyPerSecond +
                       passiveRate * PrototypeEntropyBalance.EntropyFromPassiveRate;
            AddEntropy(rate * deltaTime);
        }

        private void EmitStardust(int amount, Vector3 position, PrototypeStarStage stage, bool playEmitVfx = true)
        {
            if (amount <= 0)
                return;

            if (playEmitVfx)
                effectManager?.PlayStardustEmitEffect(position, stage);

            if (particleManager != null)
                particleManager.EmitStardustBurst(amount, position, stage, GetParticleEvolutionChance());
            else
                CreditStardust(amount);
        }

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
            var bonus = GetSupernovaBonus() + PrototypeCosmicBalance.SupernovaParticleBurst;

            effectManager?.PlaySupernovaEffect(position);
            EmitStardust(bonus, position, PrototypeStarStage.Supernova, playEmitVfx: false);
            AddEntropy(PrototypeEntropyBalance.EntropyPerSupernova);
            RunStats.RecordSupernova();
            ApplySupernovaAreaEffect(position, star);

            if (UnityEngine.Random.value < PrototypeCosmicBalance.SupernovaDnaChance +
                PrototypePrestigeModifiers.GetSupernovaDnaChanceBonus(Prestige))
                TrySpawnDnaFragment(position);

            if (UnityEngine.Random.value < PrototypeCosmicBalance.SupernovaBlackHoleChance)
                TrySpawnBlackHole(position);

            OnSupernova?.Invoke();
            OnStateChanged?.Invoke();
            star.PlaySupernovaEffect();
        }

        private void ApplySupernovaAreaEffect(Vector3 origin, PrototypeStarView source)
        {
            foreach (var other in GetActiveStars().ToList())
            {
                if (other == null || other == source || !other.IsInteractable)
                    continue;

                if (Vector3.Distance(other.transform.position, origin) > PrototypeCosmicBalance.SupernovaRadius)
                    continue;

                other.AddAge(PrototypeCosmicBalance.SupernovaAgeBurst, GetEffectiveAgeGainMultiplier());
                other.RefreshVisual();

                if (other.StarAge >= 100)
                    EnqueueSupernova(other);
            }
        }

        private void TickStarDriftAndCollisions()
        {
            var active = GetActiveStars();
            foreach (var star in active)
                star.TickDrift(Time.deltaTime, spawnAreaMin, spawnAreaMax);

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
            if (IsCollapsed || Stardust < CreateStarCost)
                return false;

            Stardust -= CreateStarCost;
            SpawnStar();
            OnStateChanged?.Invoke();
            return true;
        }

        public void TryCollapseUniverse()
        {
            if (!IsCollapsed)
                CollapseUniverse(true);
        }

        public void StartNewUniverse()
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
            particleManager?.ClearAll();

            Stardust = PrototypePrestigeModifiers.GetStartingStardust(Prestige);
            DnaFragments = 0;
            Entropy = 0;
            IsCollapsed = false;
            _passiveTimer = 0f;
            Upgrades.Reset();
            RunStats.Reset();
            LastCollapseBreakdown = null;

            OnEntropyChanged?.Invoke(Entropy);
            SpawnStar();
            OnStateChanged?.Invoke();
        }

        public void OnStarRemoved(PrototypeStarView star)
        {
            if (star != null)
                _stars.Remove(star);

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

        private void TickParallelEcho(float deltaTime)
        {
            var rate = PrototypePrestigeModifiers.GetParallelEchoPerSecond(Prestige);
            if (rate <= 0)
                return;

            CreditStardust(rate * deltaTime);
        }

        private void SpawnStar() => SpawnStarAt(GetRandomSpawnPosition(), 0);

        private PrototypeStarView SpawnStarAt(Vector3 position, int startingAge)
        {
            if (starViewPrefab == null)
                return null;

            var view = Instantiate(starViewPrefab, position, Quaternion.identity, worldRoot);
            view.Bind(this);
            if (startingAge > 0)
                view.SetAge(startingAge);
            view.RefreshVisual();
            _stars.Add(view);
            RunStats.RecordStarCreated();
            return view;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            for (var attempt = 0; attempt < 24; attempt++)
            {
                var candidate = new Vector3(
                    UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                    UnityEngine.Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                    0f);

                if (IsFarEnoughFromOtherStars(candidate))
                    return candidate;
            }

            return new Vector3(
                UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                UnityEngine.Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                0f);
        }

        private bool IsFarEnoughFromOtherStars(Vector3 position)
        {
            foreach (var star in _stars)
            {
                if (star == null)
                    continue;

                if (Vector3.Distance(star.transform.position, position) < minStarSeparation)
                    return false;
            }

            return true;
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
            _dnaTimer = PrototypeCosmicBalance.BlackHoleDnaIntervalSeconds * 0.5f;
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

                if (UnityEngine.Random.value < 0.35f)
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
