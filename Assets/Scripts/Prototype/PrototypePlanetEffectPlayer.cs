using UnityEngine;

namespace Universes.Prototype
{
    public static class PrototypePlanetEffectPlayer
    {
        public const float DefaultReferenceVisualScale = 0.22f;

        public static void Play(PrototypePlanetTypeDefinition definition, ParticleSystem prefab,
            Vector3 worldPosition, Transform parent)
        {
            if (definition == null || prefab == null)
                return;

            var prefabRoot = prefab.transform;
            var instance = Object.Instantiate(prefab.gameObject, worldPosition, Quaternion.identity, parent);

            var referenceScale = definition.effectReferenceVisualScale > 0f
                ? definition.effectReferenceVisualScale
                : DefaultReferenceVisualScale;
            var scaleFactor = definition.visualScale / referenceScale * definition.effectScaleMultiplier;
            instance.transform.localScale = prefabRoot.localScale * scaleFactor;

            var tintTarget = FindTintTarget(instance.transform, definition.effectTintChildName);
            if (tintTarget != null)
                ApplyTint(tintTarget, definition.effectTint);

            foreach (var system in instance.GetComponentsInChildren<ParticleSystem>(true))
                system.Play();

            Object.Destroy(instance, GetMaxLifetime(instance) + 0.35f);
        }

        private static ParticleSystem FindTintTarget(Transform instanceRoot, string childName)
        {
            if (!string.IsNullOrWhiteSpace(childName))
            {
                foreach (var transform in instanceRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name != childName)
                        continue;

                    var system = transform.GetComponent<ParticleSystem>();
                    if (system != null)
                        return system;
                }
            }

            var marker = instanceRoot.GetComponentInChildren<PrototypePlanetEffectTintTarget>(true);
            if (marker != null)
                return marker.GetComponent<ParticleSystem>();

            return instanceRoot.GetComponent<ParticleSystem>();
        }

        private static void ApplyTint(ParticleSystem system, Color color)
        {
            var main = system.main;
            main.startColor = color;
        }

        private static float GetMaxLifetime(GameObject instance)
        {
            var max = 0f;
            foreach (var system in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = system.main;
                max = Mathf.Max(max, main.duration + main.startLifetime.constantMax);
            }

            return max;
        }
    }
}
