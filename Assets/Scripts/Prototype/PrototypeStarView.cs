using System.Collections;
using UnityEngine;

namespace Universes.Prototype
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PrototypeStarView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer coreRenderer;
        [SerializeField] private SpriteRenderer glowRenderer;
        [SerializeField] private float popScalePeak = 1.38f;
        [SerializeField] private float popDuration = 0.28f;
        [SerializeField] private float popGlowBurst = 1.55f;

        private PrototypeGameController _controller;
        private CircleCollider2D _collider;
        private Vector3 _prefabRootScale = Vector3.one;
        private Vector3 _prefabGlowLocalScale = Vector3.one;
        private Vector3 _stageBaseScale = Vector3.one;
        private Coroutine _popRoutine;
        private float _glowPulse;
        private Color _currentStageColor = Color.white;
        private Color _currentGlowColor = Color.white;
        private bool _prefabCached;
        private bool _supernovaStarted;
        private float _ageFraction;

        public int StarAge { get; private set; }
        public int MaxStarAge { get; private set; } = 100;
        public bool HasReachedMaxAge => StarAge >= MaxStarAge;
        public PrototypeStarStage Stage => PrototypeStarStageUtility.FromAge(StarAge, MaxStarAge);
        public bool IsInteractable => StarAge < MaxStarAge && !_supernovaStarted && _inputEnabled;

        private bool _inputEnabled = true;
        private bool _driftEnabled = true;
        private Vector2 _driftVelocity;

        private void Awake()
        {
            CachePrefabTransforms();
            _driftVelocity = Random.insideUnitCircle.normalized * PrototypeCosmicBalance.StarDriftSpeed;
        }

        private void CachePrefabTransforms()
        {
            if (coreRenderer == null)
                coreRenderer = GetComponent<SpriteRenderer>();

            if (glowRenderer == null)
            {
                var glow = transform.Find("Glow");
                if (glow != null)
                    glowRenderer = glow.GetComponent<SpriteRenderer>();
            }

            _prefabRootScale = transform.localScale;
            if (glowRenderer != null)
                _prefabGlowLocalScale = glowRenderer.transform.localScale;

            _prefabCached = true;
        }

        public void Bind(PrototypeGameController controller)
        {
            _controller = controller;
            _collider = GetComponent<CircleCollider2D>();

            if (!_prefabCached)
                CachePrefabTransforms();

            RefreshVisual();
        }

        public void ConfigureMaxAge(int maxAge) => MaxStarAge = Mathf.Max(1, maxAge);

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (_collider != null)
                _collider.enabled = enabled && IsInteractable;
        }

        public void AddAge(float baseAmount, float stabilityMultiplier)
        {
            var amount = baseAmount * stabilityMultiplier;
            _ageFraction += amount;

            while (_ageFraction >= 1f && StarAge < MaxStarAge)
            {
                _ageFraction -= 1f;
                StarAge++;
            }

            if (StarAge >= MaxStarAge)
            {
                StarAge = MaxStarAge;
                _ageFraction = 0f;
            }
        }

        public void SetAge(int age)
        {
            StarAge = Mathf.Clamp(age, 0, MaxStarAge);
            _ageFraction = 0f;
        }

        public void SetDriftEnabled(bool enabled) => _driftEnabled = enabled;

        public void TickDrift(float deltaTime, Vector2 minBounds, Vector2 maxBounds)
        {
            if (!IsInteractable || !_driftEnabled)
                return;

            var pos = transform.position;
            pos += (Vector3)(_driftVelocity * deltaTime);

            if (pos.x < minBounds.x || pos.x > maxBounds.x)
            {
                _driftVelocity.x *= -1f;
                pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            }

            if (pos.y < minBounds.y || pos.y > maxBounds.y)
            {
                _driftVelocity.y *= -1f;
                pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            }

            transform.position = pos;
        }

        public bool BeginSupernova()
        {
            if (_supernovaStarted)
                return false;

            _supernovaStarted = true;
            SetInputEnabled(false);
            return true;
        }

        private void Update()
        {
            if (Stage != PrototypeStarStage.Supernova || _popRoutine != null)
                return;

            _glowPulse += Time.deltaTime * 6f;
            if (glowRenderer != null)
            {
                var pulse = 1f + Mathf.Sin(_glowPulse) * 0.08f;
                glowRenderer.transform.localScale = _prefabGlowLocalScale * pulse;
            }
        }

        public void RefreshVisual()
        {
            if (!_prefabCached)
                CachePrefabTransforms();

            var stage = Stage;
            _currentStageColor = PrototypeStarColors.GetStageColor(stage);
            _currentGlowColor = PrototypeStarColors.GetGlowColor(stage);
            _stageBaseScale = _prefabRootScale * PrototypeStarColors.GetStageScale(stage);

            if (_popRoutine == null)
                transform.localScale = _stageBaseScale;

            if (coreRenderer != null)
                coreRenderer.color = _currentStageColor;

            if (glowRenderer != null)
            {
                glowRenderer.color = _currentGlowColor;
                if (_popRoutine == null && Stage != PrototypeStarStage.Supernova)
                    glowRenderer.transform.localScale = _prefabGlowLocalScale;
            }
        }

        public void PlayClickPop()
        {
            if (_popRoutine != null)
                StopCoroutine(_popRoutine);

            _popRoutine = StartCoroutine(ClickPopRoutine());
        }

        private IEnumerator ClickPopRoutine()
        {
            var baseScale = _stageBaseScale;
            var peakScale = baseScale * popScalePeak;
            var glowBase = glowRenderer != null ? glowRenderer.transform.localScale : _prefabGlowLocalScale;
            var glowPeak = glowBase * popGlowBurst;
            var stageColor = _currentStageColor;
            var glowColor = _currentGlowColor;
            var flashColor = Color.Lerp(stageColor, Color.white, 0.55f);
            var glowFlash = glowColor;
            glowFlash.a = Mathf.Min(glowFlash.a + 0.35f, 1f);

            var elapsed = 0f;
            var upPhase = popDuration * 0.28f;
            var downPhase = popDuration - upPhase;

            while (elapsed < upPhase)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / upPhase);
                var eased = EaseOutCubic(t);

                transform.localScale = Vector3.LerpUnclamped(baseScale, peakScale, eased);

                if (coreRenderer != null)
                    coreRenderer.color = Color.Lerp(stageColor, flashColor, eased);

                if (glowRenderer != null)
                {
                    glowRenderer.color = Color.Lerp(glowColor, glowFlash, eased);
                    glowRenderer.transform.localScale = Vector3.LerpUnclamped(glowBase, glowPeak, eased);
                }

                yield return null;
            }

            elapsed = 0f;
            var startScale = transform.localScale;
            while (elapsed < downPhase)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / downPhase);
                var eased = EaseOutBack(t);

                transform.localScale = Vector3.LerpUnclamped(startScale, baseScale, eased);

                if (coreRenderer != null)
                    coreRenderer.color = Color.Lerp(flashColor, _currentStageColor, eased);

                if (glowRenderer != null)
                {
                    glowRenderer.color = Color.Lerp(glowFlash, _currentGlowColor, eased);
                    glowRenderer.transform.localScale = Vector3.LerpUnclamped(glowPeak, _prefabGlowLocalScale, eased);
                }

                yield return null;
            }

            transform.localScale = baseScale;
            if (coreRenderer != null)
                coreRenderer.color = _currentStageColor;
            if (glowRenderer != null)
            {
                glowRenderer.color = _currentGlowColor;
                glowRenderer.transform.localScale = _prefabGlowLocalScale;
            }

            _popRoutine = null;
        }

        public void PlaySupernovaEffect()
        {
            if (_popRoutine != null)
            {
                StopCoroutine(_popRoutine);
                _popRoutine = null;
            }

            StartCoroutine(SupernovaRoutine());
        }

        private IEnumerator SupernovaRoutine()
        {
            var duration = 0.65f;
            var elapsed = 0f;
            var startScale = transform.localScale;
            var peakScale = startScale * 2.4f;
            var flash = PrototypeStarColors.GetStageColor(PrototypeStarStage.Supernova);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;

                if (coreRenderer != null)
                {
                    var c = flash;
                    c.a = 1f - t;
                    coreRenderer.color = c;
                }

                if (glowRenderer != null)
                {
                    var g = flash;
                    g.a = (1f - t) * 0.8f;
                    glowRenderer.color = g;
                    glowRenderer.transform.localScale = _prefabGlowLocalScale * (1f + t * 1.5f);
                }

                transform.localScale = Vector3.Lerp(startScale, peakScale, t);
                yield return null;
            }

            _controller?.OnStarRemoved(this);
            Destroy(gameObject);
        }

        private void OnMouseDown()
        {
            if (_controller == null || !IsInteractable || _controller.IsCollapsed)
                return;

            _controller.OnStarClicked(this);
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
