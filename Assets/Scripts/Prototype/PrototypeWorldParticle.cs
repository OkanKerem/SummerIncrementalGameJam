using UnityEngine;

namespace Universes.Prototype
{
    public class PrototypeWorldParticle : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool tintByStage = true;
        [SerializeField] private bool scaleByValue = true;
        [SerializeField] private float spawnScatter = 0.04f;
        [SerializeField] private float scatterBurstDuration = 0.18f;
        [SerializeField] private float homingBlendDuration = 0.55f;
        [SerializeField] private float arrivalDistance = 0.15f;
        [SerializeField] private float homingAcceleration = 2.5f;

        private PrototypeCosmicParticleManager _manager;
        private int _value;
        private bool _isDna;
        private float _speed;
        private float _spawnDelay;
        private float _currentSpeed;
        private float _flightTime;
        private Vector2 _burstDirection;
        private bool _pulledByBlackHole;
        private Transform _blackHoleTarget;
        private Vector3 _prefabBaseScale = Vector3.one;

        public int Value => _value;
        public bool IsDna => _isDna;
        public Vector3 Position => transform.position;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            _prefabBaseScale = transform.localScale;
        }

        public void InitStardust(PrototypeCosmicParticleManager manager, int value, Vector3 worldPosition,
            PrototypeStarStage stage, float spawnDelay = 0f)
        {
            _manager = manager;
            _value = value;
            _isDna = false;
            _spawnDelay = spawnDelay;
            _flightTime = 0f;
            _speed = manager.GetCollectibleFlySpeed(value);
            _currentSpeed = _speed * 0.35f;
            _burstDirection = Random.insideUnitCircle.normalized;
            if (_burstDirection.sqrMagnitude < 0.01f)
                _burstDirection = Vector2.up;

            transform.position = worldPosition + (Vector3)(Random.insideUnitCircle * spawnScatter);
            ApplyVisual(PrototypeStarColors.GetStageColor(stage), PrototypeParticleTiers.GetVisualScale(value));
        }

        public void InitDna(PrototypeCosmicParticleManager manager, Vector3 worldPosition)
        {
            _manager = manager;
            _value = 1;
            _isDna = true;
            _flightTime = 0f;
            _speed = manager.GetCollectibleFlySpeed(5);
            _currentSpeed = _speed * 0.35f;
            _burstDirection = Random.insideUnitCircle.normalized;
            transform.position = worldPosition + (Vector3)(Random.insideUnitCircle * (spawnScatter * 0.6f));

            if (tintByStage && spriteRenderer != null)
                spriteRenderer.color = new Color(0.55f, 1f, 0.75f);

            if (scaleByValue)
                transform.localScale = _prefabBaseScale;
        }

        private void ApplyVisual(Color tint, float valueScale)
        {
            if (spriteRenderer != null && tintByStage)
                spriteRenderer.color = tint;

            if (scaleByValue)
                transform.localScale = _prefabBaseScale * valueScale;
        }

        public void CollectNow()
        {
            if (_manager == null)
                return;

            if (_isDna)
                _manager.OnDnaCollected();
            else
                _manager.OnStardustCollected(_value, transform.position);

            Destroy(gameObject);
        }

        public void SetBlackHolePull(Transform target)
        {
            _pulledByBlackHole = true;
            _blackHoleTarget = target;
        }

        public void ClearBlackHolePull()
        {
            _pulledByBlackHole = false;
            _blackHoleTarget = null;
        }

        private void Update()
        {
            if (_manager == null)
            {
                Destroy(gameObject);
                return;
            }

            if (_spawnDelay > 0f)
            {
                _spawnDelay -= Time.deltaTime;
                return;
            }

            _flightTime += Time.deltaTime;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, _speed, homingAcceleration * Time.deltaTime);

            Vector3 target;
            Vector2 moveDirection;

            if (_pulledByBlackHole && _blackHoleTarget != null)
            {
                target = _blackHoleTarget.position;
                moveDirection = GetDirectionToward(target);
            }
            else
            {
                target = _isDna
                    ? _manager.GetDnaTargetWorldPosition()
                    : _manager.GetStardustTargetWorldPosition();
                moveDirection = GetScatterThenHomeDirection(target);
            }

            transform.position += (Vector3)(moveDirection * (_currentSpeed * Time.deltaTime));

            if (Vector3.Distance(transform.position, target) <= arrivalDistance)
            {
                if (_pulledByBlackHole && _blackHoleTarget != null)
                    _manager.OnParticleConsumedByBlackHole(this, _blackHoleTarget);
                else if (_isDna)
                    _manager.OnDnaCollected();
                else
                    _manager.OnStardustCollected(_value, transform.position);

                Destroy(gameObject);
            }
        }

        private Vector2 GetScatterThenHomeDirection(Vector3 target)
        {
            var toTarget = (Vector2)(target - transform.position);
            var homingDirection = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : _burstDirection;

            if (_flightTime < scatterBurstDuration)
            {
                var burstT = _flightTime / scatterBurstDuration;
                return Vector2.Lerp(_burstDirection, homingDirection, burstT * 0.45f).normalized;
            }

            var homingT = Mathf.Clamp01((_flightTime - scatterBurstDuration) / homingBlendDuration);
            return Vector2.Lerp(_burstDirection, homingDirection, homingT).normalized;
        }

        private Vector2 GetDirectionToward(Vector3 target)
        {
            var toTarget = (Vector2)(target - transform.position);
            return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero;
        }
    }
}
