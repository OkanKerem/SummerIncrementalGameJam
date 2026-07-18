using UnityEngine;

namespace Universes.Game
{
    public class BlackHoleView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer coreRenderer;
        [SerializeField] private float spinSpeed = 90f;
        [SerializeField] private float baseScale = 0.55f;

        private GameController _controller;
        private float _lifetime;
        private float _dnaTimer;
        private float _age;

        public float DnaPotential { get; private set; }
        public float Age => _age;

        public void Init(GameController controller, Vector3 position)
        {
            _controller = controller;
            transform.position = position;
            var balance = _controller.Phase3Balance;
            _lifetime = balance.blackHoleLifetimeSeconds;
            _dnaTimer = balance.blackHoleDnaIntervalSeconds *
                        balance.blackHoleInitialDnaTimerMultiplier;
            _age = 0f;
            DnaPotential = 0f;

            EnsureRenderer();
            transform.localScale = Vector3.one * baseScale;
        }

        private void EnsureRenderer()
        {
            if (coreRenderer == null)
                coreRenderer = GetComponent<SpriteRenderer>();

            if (coreRenderer == null)
                coreRenderer = gameObject.AddComponent<SpriteRenderer>();

            if (coreRenderer.sprite == null)
                coreRenderer.sprite = CreateDiscSprite();

            if (coreRenderer.color.a < 0.01f)
                coreRenderer.color = new Color(0.15f, 0.05f, 0.25f, 0.95f);

            coreRenderer.sortingOrder = 15;
        }

        private void Update()
        {
            if (_controller == null || _controller.IsCollapsed || _controller.IsUniverseCollapsing)
                return;

            _age += Time.deltaTime;
            _lifetime -= Time.deltaTime;
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

            var pulse = 1f + Mathf.Sin(_age * 4f) * 0.06f;
            transform.localScale = Vector3.one * (baseScale * pulse);

            var balance = _controller.Phase3Balance;
            _controller.AddBlackHoleEntropy(balance.blackHoleEntropyPerSecond * Time.deltaTime);
            _controller.TickBlackHoleThreat(this, Time.deltaTime);

            _dnaTimer -= Time.deltaTime;
            if (_dnaTimer <= 0f)
            {
                _dnaTimer = balance.blackHoleDnaIntervalSeconds;
                DnaPotential += balance.blackHoleDnaPotentialPerInterval;
                _controller.AddBlackHoleDnaPotential(balance.blackHoleDnaPotentialPerInterval);

                if (UnityEngine.Random.value < balance.blackHoleDnaFragmentChance +
                    _controller.GetCosmicEventDnaChanceBonus())
                    _controller.TrySpawnDnaFragment(transform.position);
            }

            if (_lifetime <= 0f)
                _controller.RemoveBlackHole(this);
        }

        private static Sprite CreateDiscSprite()
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
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
