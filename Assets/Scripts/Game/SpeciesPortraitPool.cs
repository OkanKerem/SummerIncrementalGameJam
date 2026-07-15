using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Universes.Game
{
    public class SpeciesPortraitPool : MonoBehaviour
    {
        [SerializeField] private GameObject portraitPrefab;
        [SerializeField] private int preloadCount = 8;

        private readonly Stack<GameObject> _pool = new();
        private bool _preloaded;

        public bool HasPrefab => portraitPrefab != null;
        public GameObject PortraitPrefab => portraitPrefab;

        public void Configure(GameObject prefab, int preload)
        {
            portraitPrefab = prefab;
            preloadCount = Mathf.Max(0, preload);
            Preload();
        }

        public GameObject Acquire(Transform parent, Planet planet)
        {
            if (portraitPrefab == null || parent == null)
                return null;

            Preload();

            var instance = _pool.Count > 0
                ? _pool.Pop()
                : Instantiate(portraitPrefab, transform);

            instance.transform.SetParent(parent, false);
            instance.SetActive(true);
            FitToParent(instance);
            ConfigurePortrait(instance, planet);
            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null || this == null || !gameObject.scene.IsValid())
                return;

            instance.transform.SetParent(transform, false);
            instance.SetActive(false);
            _pool.Push(instance);
        }

        private void Preload()
        {
            if (_preloaded || portraitPrefab == null)
                return;

            _preloaded = true;
            for (var i = 0; i < preloadCount; i++)
            {
                var instance = Instantiate(portraitPrefab, transform);
                instance.SetActive(false);
                _pool.Push(instance);
            }
        }

        private static void FitToParent(GameObject instance)
        {
            if (instance.TryGetComponent<RectTransform>(out var rect))
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                return;
            }

            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        public static void ConfigurePortrait(GameObject instance, Planet planet)
        {
            if (instance == null || planet == null)
                return;

            var seed = GetStableSeed(planet);
            var color = GetSpeciesColor(planet);
            var facesRoot = instance.transform.Find("Faces");
            var eyesRoot = instance.transform.Find("Eyes");

            var face = SelectOneChild(facesRoot, seed);
            SelectOneChild(eyesRoot, seed / 7);

            if (face != null)
                ApplyColor(face, color);
        }

        private static Transform SelectOneChild(Transform root, int seed)
        {
            if (root == null || root.childCount == 0)
                return null;

            var selected = PositiveModulo(seed, root.childCount);
            Transform activeChild = null;
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var active = i == selected;
                child.gameObject.SetActive(active);
                if (active)
                    activeChild = child;
            }

            return activeChild;
        }

        private static void ApplyColor(Transform root, Color color)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                image.color = color;

            foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
                sprite.color = color;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is SpriteRenderer)
                    continue;

                if (renderer.material != null && renderer.material.HasProperty("_Color"))
                    renderer.material.color = color;
            }
        }

        public static Color GetSpeciesColor(Planet planet)
        {
            var hue = Mathf.Repeat((planet.Intelligence * 0.007f) + (planet.Aggression * 0.013f), 1f);
            var saturation = Mathf.Lerp(0.45f, 0.85f, planet.Aggression / 100f);
            var value = Mathf.Lerp(0.65f, 1f, planet.Intelligence / 100f);
            return Color.HSVToRGB(hue, saturation, value);
        }

        private static int GetStableSeed(Planet planet)
        {
            unchecked
            {
                var hash = 17;
                var name = planet.SpeciesName ?? string.Empty;
                for (var i = 0; i < name.Length; i++)
                    hash = hash * 31 + name[i];

                hash = hash * 31 + planet.Intelligence;
                hash = hash * 31 + planet.Aggression;
                return hash;
            }
        }

        private static int PositiveModulo(int value, int divisor)
        {
            if (divisor <= 0)
                return 0;

            var result = value % divisor;
            return result < 0 ? result + divisor : result;
        }
    }
}
