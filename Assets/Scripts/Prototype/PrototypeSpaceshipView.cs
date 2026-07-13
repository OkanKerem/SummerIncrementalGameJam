using System;
using UnityEngine;

namespace Universes.Prototype
{
    public class PrototypeSpaceshipView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer shipRenderer;
        [SerializeField] private Transform portraitRoot;
        [SerializeField] private float portraitOffsetY = 0.32f;
        [SerializeField] private float portraitScale = 0.18f;

        private Vector3 _start;
        private Vector3 _end;
        private float _speed;
        private float _duration;
        private float _elapsed;
        private Func<Vector3> _getEndPosition;
        private Action<Vector3> _onArrived;
        private bool _preservePrefabVisuals;
        private static Sprite _fallbackShipSprite;

        public void Initialize(PrototypePlanet source, Sprite[] shipSprites, GameObject alienPortraitPrefab,
            Vector3 start, Vector3 end, Func<Vector3> getEndPosition, float speed, bool preservePrefabVisuals,
            Action<Vector3> onArrived)
        {
            _start = start;
            _end = end;
            _getEndPosition = getEndPosition;
            _speed = Mathf.Max(0.1f, speed);
            _duration = Mathf.Max(0.1f, Vector3.Distance(start, end) / _speed);
            _elapsed = 0f;
            _onArrived = onArrived;
            _preservePrefabVisuals = preservePrefabVisuals;

            if (_preservePrefabVisuals)
                ResolveExistingVisuals();
            else
                EnsureVisuals();

            transform.position = _start;

            if (shipRenderer != null)
            {
                shipRenderer.sprite = SelectSprite(shipSprites);

                if (!_preservePrefabVisuals)
                {
                    shipRenderer.color = source != null && source.HasSpecies
                        ? PrototypeSpeciesPortraitPool.GetSpeciesColor(source)
                        : Color.white;
                    shipRenderer.sortingOrder = 12;
                }
            }

            if (!_preservePrefabVisuals)
                ConfigurePortrait(source, alienPortraitPrefab);
            else
                ConfigureExistingPrefabPortrait(source);
            FaceTravelDirection();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_getEndPosition != null)
                _end = _getEndPosition();

            var t = Mathf.Clamp01(_elapsed / _duration);
            var easedT = EaseInOut(t);
            transform.position = Vector3.Lerp(_start, _end, easedT);
            FaceTravelDirection();

            if (t < 1f)
                return;

            transform.position = _end;
            _onArrived?.Invoke(_end);
            Destroy(gameObject);
        }

        private void EnsureVisuals()
        {
            if (shipRenderer == null)
            {
                var shipGo = new GameObject("ShipSprite");
                shipGo.transform.SetParent(transform, false);
                shipRenderer = shipGo.AddComponent<SpriteRenderer>();
            }

            if (portraitRoot == null)
            {
                var portraitGo = new GameObject("AlienPortrait");
                portraitGo.transform.SetParent(transform, false);
                portraitGo.transform.localPosition = Vector3.up * portraitOffsetY;
                portraitGo.transform.localScale = Vector3.one * portraitScale;
                portraitRoot = portraitGo.transform;
            }
        }

        private void ResolveExistingVisuals()
        {
            if (shipRenderer != null)
                return;

            shipRenderer = GetComponent<SpriteRenderer>();
            if (shipRenderer != null)
                return;

            shipRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        private void ConfigureExistingPrefabPortrait(PrototypePlanet source)
        {
            if (source == null)
                return;

            var portrait = FindExistingPortraitRoot();
            if (portrait == null)
                return;

            portrait.gameObject.SetActive(true);
            var faces = portrait.Find("Faces");
            var eyes = portrait.Find("Eyes");
            if (faces != null)
                faces.gameObject.SetActive(true);
            if (eyes != null)
                eyes.gameObject.SetActive(true);

            PrototypeSpeciesPortraitPool.ConfigurePortrait(portrait.gameObject, source);
        }

        private Transform FindExistingPortraitRoot()
        {
            var searchRoot = portraitRoot != null ? portraitRoot : transform;
            var namedAlien = FindChildRecursive(searchRoot, "Alien");
            if (namedAlien != null)
                return namedAlien;

            return FindPortraitByParts(searchRoot);
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

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;

            foreach (Transform child in root)
            {
                if (child.name == childName)
                    return child;

                var found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void ConfigurePortrait(PrototypePlanet source, GameObject alienPortraitPrefab)
        {
            if (portraitRoot == null || source == null)
                return;

            foreach (Transform child in portraitRoot)
                Destroy(child.gameObject);

            if (alienPortraitPrefab != null)
            {
                var instance = Instantiate(alienPortraitPrefab, portraitRoot);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                PrototypeSpeciesPortraitPool.ConfigurePortrait(instance, source);
                return;
            }

            var fallback = new GameObject("FallbackPortrait");
            fallback.transform.SetParent(portraitRoot, false);
            var renderer = fallback.AddComponent<SpriteRenderer>();
            renderer.sprite = GetFallbackShipSprite();
            renderer.color = PrototypeSpeciesPortraitPool.GetSpeciesColor(source);
            renderer.sortingOrder = 13;
            fallback.transform.localScale = Vector3.one * 0.45f;
        }

        private void FaceTravelDirection()
        {
            var direction = _end - _start;
            if (direction.sqrMagnitude < 0.001f)
                return;

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        private static Sprite SelectSprite(Sprite[] sprites)
        {
            if (sprites != null && sprites.Length > 0)
            {
                var sprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
                if (sprite != null)
                    return sprite;
            }

            return GetFallbackShipSprite();
        }

        private static Sprite GetFallbackShipSprite()
        {
            if (_fallbackShipSprite != null)
                return _fallbackShipSprite;

            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    var centerDistance = Mathf.Abs(x - 7.5f);
                    var inShip = y > 2 && y < 14 && centerDistance < (y * 0.35f + 1f);
                    texture.SetPixel(x, y, inShip ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            _fallbackShipSprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
            return _fallbackShipSprite;
        }

        private static float EaseInOut(float t) => t * t * t * (t * (6f * t - 15f) + 10f);
    }
}
