using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Universes.Game
{
    public struct ParticleCollectResult
    {
        public int StardustValue;
        public int DnaFragments;

        public bool AnyCollected => StardustValue > 0 || DnaFragments > 0;
    }

    public class CosmicParticleManager : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private WorldParticle stardustParticlePrefab;
        [SerializeField] private WorldParticle dnaParticlePrefab;
        [SerializeField] private float collectibleFlySpeedMultiplier = 0.85f;
        [SerializeField] private float collectibleSpawnStagger = 0.04f;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform stardustCollector;
        [SerializeField] private RectTransform dnaCollector;
        [SerializeField] private Canvas hudCanvas;

        private readonly List<WorldParticle> _particles = new();

        public float CollectibleFlySpeedMultiplier => collectibleFlySpeedMultiplier;

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<GameController>();

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (hudCanvas == null)
                hudCanvas = GetComponentInParent<Canvas>();
        }

        public void Configure(RectTransform stardustTarget, RectTransform dnaTarget, Canvas canvas, Camera cam)
        {
            stardustCollector = stardustTarget;
            dnaCollector = dnaTarget;
            hudCanvas = canvas;
            worldCamera = cam;
        }

        public float GetCollectibleFlySpeed(int value) =>
            ParticleTiers.GetFlySpeed(value) * collectibleFlySpeedMultiplier;

        public Vector3 GetStardustTargetWorldPosition()
        {
            EnsureCollectorsConfigured();
            return GetCollectorWorldPosition(stardustCollector);
        }

        public Vector3 GetDnaTargetWorldPosition()
        {
            EnsureCollectorsConfigured();
            return GetCollectorWorldPosition(dnaCollector);
        }

        private void EnsureCollectorsConfigured()
        {
            if (stardustCollector != null && dnaCollector != null)
                return;

            var hud = FindAnyObjectByType<HUD>();
            hud?.ConfigureParticleManager(this);
        }

        public Vector3 ScreenToWorldPosition(Vector3 screenPosition)
        {
            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null)
                return Vector3.zero;

            var depth = Mathf.Abs(worldCamera.transform.position.z);
            var world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
            world.z = 0f;
            return world;
        }

        public void EmitStardustBurst(int totalAmount, Vector3 worldPosition, StarStage stage,
            float particleEvolutionChance = 0f)
        {
            if (totalAmount <= 0)
                return;

            var chunks = ParticleTiers.SplitIntoChunks(totalAmount, particleEvolutionChance);
            for (var i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var chunkStage = chunk >= 5
                    ? ParticleTiers.GetPreferredStageForTier(chunk)
                    : stage;
                var delay = i * collectibleSpawnStagger;
                SpawnStardustParticle(chunk, worldPosition, chunkStage, delay);
            }
        }

        public void SpawnStardustParticle(int value, Vector3 worldPosition, StarStage stage, float spawnDelay = 0f)
        {
            if (stardustParticlePrefab == null)
            {
                Debug.LogWarning("Stardust particle prefab is not assigned on CosmicParticleManager.", this);
                return;
            }

            var particle = Instantiate(stardustParticlePrefab, transform);
            particle.name = "StardustParticle";
            particle.InitStardust(this, value, worldPosition, stage, spawnDelay);
            _particles.Add(particle);
        }

        public void SpawnDnaFragment(Vector3 worldPosition) =>
            SpawnDnaParticle(worldPosition, 0f);

        public void SpawnDnaPotential(Vector3 worldPosition, float amount)
        {
            if (amount <= 0f)
                return;

            SpawnDnaParticle(worldPosition, amount);
        }

        private void SpawnDnaParticle(Vector3 worldPosition, float potentialAmount)
        {
            var prefab = dnaParticlePrefab != null ? dnaParticlePrefab : stardustParticlePrefab;
            if (prefab == null)
            {
                Debug.LogWarning("DNA particle prefab is not assigned on CosmicParticleManager.", this);
                return;
            }

            var particle = Instantiate(prefab, transform);
            particle.name = potentialAmount > 0f ? "DnaPotentialParticle" : "DnaParticle";
            particle.InitDna(this, worldPosition, potentialAmount);
            _particles.Add(particle);
        }

        public void OnStardustCollected(int value, Vector3 position)
        {
            _particles.RemoveAll(p => p == null);
            controller?.CreditStardust(value);

            if (value >= 100 && Random.value < CosmicBalance.MassiveParticleDnaChance)
                controller?.TrySpawnDnaFragment(position);
        }

        public void OnDnaCollected()
        {
            _particles.RemoveAll(p => p == null);
            controller?.CreditDnaFragment();
        }

        public void OnDnaPotentialCollected(float amount)
        {
            _particles.RemoveAll(p => p == null);
            controller?.AddDnaPotential(amount);
            controller?.NotifyDnaPotentialGained();
        }

        public void OnParticleConsumedByBlackHole(WorldParticle particle, Transform blackHole)
        {
            _particles.Remove(particle);
            controller?.OnParticleConsumedByBlackHole(particle.Value, blackHole);
        }

        public void TickBlackHolePull(IReadOnlyList<BlackHoleView> blackHoles)
        {
            _particles.RemoveAll(p => p == null);

            foreach (var particle in _particles)
            {
                if (particle == null || particle.IsDna)
                    continue;

                Transform closest = null;
                var closestDist = float.MaxValue;

                foreach (var hole in blackHoles)
                {
                    if (hole == null)
                        continue;

                    var dist = Vector3.Distance(particle.Position, hole.transform.position);
                    if (dist <= CosmicBalance.BlackHoleParticlePullRadius && dist < closestDist)
                    {
                        closestDist = dist;
                        closest = hole.transform;
                    }
                }

                if (closest != null)
                    particle.SetBlackHolePull(closest);
                else
                    particle.ClearBlackHolePull();
            }
        }

        public void ClearAll()
        {
            foreach (var particle in _particles)
            {
                if (particle != null)
                    Destroy(particle.gameObject);
            }

            _particles.Clear();
        }

        public ParticleCollectResult CollectParticlesInRadius(Vector3 center, float radius)
        {
            _particles.RemoveAll(p => p == null);

            var result = new ParticleCollectResult();
            for (var i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];
                if (particle == null)
                    continue;

                if (Vector3.Distance(particle.Position, center) > radius)
                    continue;

                if (particle.IsDna)
                    result.DnaFragments++;
                else
                    result.StardustValue += particle.Value;

                particle.CollectNow();
                _particles.RemoveAt(i);
            }

            return result;
        }

        public int CollectStardustInRadius(Vector3 center, float radius) =>
            CollectParticlesInRadius(center, radius).StardustValue;

        private Vector3 GetCollectorWorldPosition(RectTransform collector)
        {
            if (collector == null || worldCamera == null)
                return Vector3.zero;

            Vector3 screenPoint;
            if (hudCanvas != null && hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                screenPoint = collector.position;
            else
            {
                screenPoint = RectTransformUtility.WorldToScreenPoint(
                    hudCanvas != null ? hudCanvas.worldCamera : null,
                    collector.position);
            }

            var depth = Mathf.Abs(worldCamera.transform.position.z);
            var world = worldCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
            world.z = 0f;
            return world;
        }
    }
}
