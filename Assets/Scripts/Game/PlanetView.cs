using TMPro;
using UnityEngine;

namespace Universes.Game
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlanetView : MonoBehaviour
    {
        [SerializeField] private float popScalePeak = 1.25f;
        [SerializeField] private float popDuration = 0.18f;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Vector2 fallbackOrbitEllipseScale = new(1.25f, 0.58f);

        private Planet _planet;
        private PlanetManager _manager;
        private Transform _orbitCenter;
        private SpriteRenderer _sprite;
        private CircleCollider2D _collider;
        private LineRenderer _orbitLine;
        private Vector3 _baseScale;
        private Coroutine _popRoutine;
        private static Material _orbitLineMaterial;
        private bool _blackHolePullActive;

        public Planet Planet => _planet;
        public bool IsBlackHolePullActive => _blackHolePullActive;

        public void Bind(Planet planet, PlanetManager manager, Transform orbitCenter)
        {
            _planet = planet;
            _manager = manager;
            _orbitCenter = orbitCenter;
            _sprite = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CircleCollider2D>();
            _baseScale = transform.localScale;

            ApplyDefinitionVisuals();
            RefreshVisual();
            UpdatePosition();
            RefreshOrbitLine();
        }

        public void TickOrbit(float deltaTime)
        {
            if (_planet == null || _orbitCenter == null || !_planet.IsAlive)
                return;

            if (_blackHolePullActive)
                return;

            _planet.OrbitAngle += _manager.GetOrbitSpeed() * deltaTime;
            UpdatePosition();
            RefreshOrbitLine();
        }

        public float PullToward(Vector3 target, float pullRadius, float pullSpeed, float deltaTime)
        {
            if (_planet == null || !_planet.IsAlive)
                return float.MaxValue;

            var position = transform.position;
            var distance = Vector3.Distance(position, target);
            if (pullRadius <= 0f || pullSpeed <= 0f || deltaTime <= 0f || distance > pullRadius)
            {
                if (_blackHolePullActive)
                {
                    _blackHolePullActive = false;
                    UpdatePosition();
                    RefreshOrbitLine();
                }

                return distance;
            }

            _blackHolePullActive = true;
            if (distance > 0.001f)
            {
                var proximity = 1f - Mathf.Clamp01(distance / pullRadius);
                var speed = pullSpeed * (0.35f + proximity * 1.65f);
                transform.position = Vector3.MoveTowards(position, target, speed * deltaTime);
                RefreshOrbitLine();
            }

            return Vector3.Distance(transform.position, target);
        }

        private void ApplyDefinitionVisuals()
        {
            var definition = _planet?.Definition;
            if (definition == null)
                return;

            if (definition.planetSprite != null && _sprite != null)
                _sprite.sprite = definition.planetSprite;

            _baseScale = Vector3.one * definition.visualScale;

            if (_sprite != null)
                _sprite.sortingOrder = definition.sortingOrder;
        }

        public Color GetDisplayColor()
        {
            if (_planet == null)
                return Color.gray;

            var definition = _planet.Definition;
            var color = definition != null ? definition.planetColor : Color.gray;

            if (_planet.HasLife)
            {
                var lifeTint = definition != null ? definition.lifeTintColor : new Color(0.35f, 0.95f, 0.55f);
                color = Color.Lerp(color, lifeTint, 0.45f);
            }

            if (_planet.CivilizationStage >= CivilizationStage.CivilizationPhase)
            {
                var civTint = definition != null ? definition.civilizationTintColor : new Color(1f, 0.85f, 0.35f);
                color = Color.Lerp(color, civTint, 0.25f);
            }

            color.a = 1f;
            return color;
        }

        public void RefreshVisual()
        {
            if (_sprite == null || _planet == null)
                return;

            var definition = _planet.Definition;
            var color = GetDisplayColor();
            var durabilityT = _planet.MaxDurability > 0f ? _planet.Durability / _planet.MaxDurability : 0f;
            color.a = 0.55f + durabilityT * 0.45f;
            _sprite.color = color;

            var civScale = 1f + (int)_planet.CivilizationStage * 0.04f;
            transform.localScale = _baseScale * civScale;

            if (_collider != null)
                _collider.enabled = _planet.IsAlive;

            if (_orbitLine != null)
                _orbitLine.enabled = _planet.IsAlive && (_manager == null || _manager.ShowOrbitLines);

            RefreshNameLabel();
            RefreshSpeciesPortrait();
        }

        private GameObject _speciesPortraitRoot;
        private Canvas _speciesPortraitCanvas;

        private void RefreshSpeciesPortrait()
        {
            if (_planet == null)
                return;

            var show = _planet.HasSpecies;
            var canvas = ResolveSpeciesPortraitCanvas();
            if (canvas != null)
                canvas.gameObject.SetActive(show);

            if (!show)
                return;

            var portrait = ResolveSpeciesPortraitRoot();
            if (portrait == null)
                return;

            portrait.SetActive(true);
            var faces = portrait.transform.Find("Faces");
            var eyes = portrait.transform.Find("Eyes");
            if (faces != null)
                faces.gameObject.SetActive(true);
            if (eyes != null)
                eyes.gameObject.SetActive(true);

            SpeciesPortraitPool.ConfigurePortrait(portrait, _planet);
        }

        private Canvas ResolveSpeciesPortraitCanvas()
        {
            if (_speciesPortraitCanvas != null)
                return _speciesPortraitCanvas;

            _speciesPortraitCanvas = GetComponentInChildren<Canvas>(true);
            return _speciesPortraitCanvas;
        }

        private GameObject ResolveSpeciesPortraitRoot()
        {
            if (_speciesPortraitRoot != null)
                return _speciesPortraitRoot;

            var canvas = ResolveSpeciesPortraitCanvas();
            if (canvas != null)
            {
                var foundInCanvas = FindPortraitByParts(canvas.transform);
                if (foundInCanvas != null)
                {
                    _speciesPortraitRoot = foundInCanvas.gameObject;
                    return _speciesPortraitRoot;
                }
            }

            var namedAlien = transform.Find("Alien");
            if (namedAlien == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.StartsWith("Alien"))
                    {
                        namedAlien = child;
                        break;
                    }
                }
            }

            if (namedAlien != null)
            {
                _speciesPortraitRoot = namedAlien.gameObject;
                return _speciesPortraitRoot;
            }

            var found = FindPortraitByParts(transform);
            if (found != null)
                _speciesPortraitRoot = found.gameObject;

            return _speciesPortraitRoot;
        }

        private static Transform FindPortraitByParts(Transform root)
        {
            if (root == null)
                return null;

            if (root.Find("Faces") != null && root.Find("Eyes") != null)
                return root;

            foreach (Transform child in root)
            {
                var found = FindPortraitByParts(child);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void UpdatePosition()
        {
            if (_orbitCenter == null || _planet == null)
                return;

            var rad = _planet.OrbitAngle * Mathf.Deg2Rad;
            var offset = _manager != null
                ? _manager.GetOrbitOffset(_planet)
                : new Vector3(
                    Mathf.Cos(rad) * _planet.OrbitRadius * fallbackOrbitEllipseScale.x,
                    Mathf.Sin(rad) * _planet.OrbitRadius * fallbackOrbitEllipseScale.y,
                    0f);
            transform.position = _orbitCenter.position + offset;
            RefreshNameLabel();
        }

        public void HideOrbitLine()
        {
            if (_orbitLine != null)
                _orbitLine.enabled = false;
        }

        private void RefreshOrbitLine()
        {
            if (_planet == null || _orbitCenter == null || _manager == null || !_manager.ShowOrbitLines)
            {
                HideOrbitLine();
                return;
            }

            EnsureOrbitLine();
            if (_orbitLine == null)
                return;

            var segments = Mathf.Max(16, _manager.OrbitLineSegments);
            _orbitLine.enabled = _planet.IsAlive;
            _orbitLine.positionCount = segments + 1;
            _orbitLine.startWidth = _manager.OrbitLineWidth;
            _orbitLine.endWidth = _manager.OrbitLineWidth;
            _orbitLine.startColor = _manager.OrbitLineColor;
            _orbitLine.endColor = _manager.OrbitLineColor;
            _orbitLine.sortingOrder = _manager.OrbitLineSortingOrder;

            for (var i = 0; i <= segments; i++)
            {
                var angle = (i / (float)segments) * 360f;
                _orbitLine.SetPosition(i, _orbitCenter.position + _manager.GetOrbitOffset(_planet.OrbitRadius, angle));
            }
        }

        private void EnsureOrbitLine()
        {
            if (_orbitLine != null)
                return;

            var go = new GameObject("OrbitLine");
            go.transform.SetParent(transform, false);
            _orbitLine = go.AddComponent<LineRenderer>();
            _orbitLine.useWorldSpace = true;
            _orbitLine.loop = false;
            _orbitLine.textureMode = LineTextureMode.Stretch;
            _orbitLine.numCornerVertices = 4;
            _orbitLine.numCapVertices = 4;
            _orbitLine.material = GetOrbitLineMaterial();
        }

        private static Material GetOrbitLineMaterial()
        {
            if (_orbitLineMaterial != null)
                return _orbitLineMaterial;

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            _orbitLineMaterial = new Material(shader);
            return _orbitLineMaterial;
        }

        private void RefreshNameLabel()
        {
            ResolveNameLabel();

            if (nameLabel == null || _planet == null)
                return;

            var hasRealName = !string.IsNullOrWhiteSpace(_planet.PlanetName);
            nameLabel.enabled = hasRealName;
            if (!hasRealName)
                return;

            nameLabel.text = _planet.PlanetName;
        }

        private void ResolveNameLabel()
        {
            if (nameLabel != null)
                return;

            var existing = transform.Find("PlanetNameLabel_TMP");
            if (existing != null)
                nameLabel = existing.GetComponent<TMP_Text>();

            if (nameLabel == null)
                nameLabel = GetComponentInChildren<TMP_Text>(true);
        }

        private void OnMouseDown()
        {
            if (_manager == null || _planet == null || !_planet.IsAlive)
                return;

            _manager.OnPlanetClicked(this);
            PlayClickPop();
        }

        private void PlayClickPop()
        {
            if (_popRoutine != null)
                StopCoroutine(_popRoutine);

            _popRoutine = StartCoroutine(PopRoutine());
        }

        private System.Collections.IEnumerator PopRoutine()
        {
            var start = _baseScale;
            var peak = _baseScale * popScalePeak;
            var elapsed = 0f;

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / popDuration;
                transform.localScale = Vector3.Lerp(start, peak, t < 0.4f ? t / 0.4f : 1f - (t - 0.4f) / 0.6f);
                yield return null;
            }

            RefreshVisual();
            _popRoutine = null;
        }
    }
}
