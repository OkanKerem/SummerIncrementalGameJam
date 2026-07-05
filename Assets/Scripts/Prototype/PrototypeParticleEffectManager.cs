using UnityEngine;

namespace Universes.Prototype
{
    public class PrototypeParticleEffectManager : MonoBehaviour
    {
        [SerializeField] private ParticleSystem clickEffectPrefab;
        [SerializeField] private ParticleSystem stardustEmitEffectPrefab;
        [SerializeField] private ParticleSystem supernovaEffectPrefab;

        public void PlayClickEffect(Vector3 worldPosition, PrototypeStarStage stage)
        {
            PlayTintedEffect(clickEffectPrefab, worldPosition, PrototypeStarColors.GetStageColor(stage));
        }

        public void PlayStardustEmitEffect(Vector3 worldPosition, PrototypeStarStage stage)
        {
            PlayTintedEffect(stardustEmitEffectPrefab, worldPosition, PrototypeStarColors.GetStageColor(stage));
        }

        public void PlaySupernovaEffect(Vector3 worldPosition)
        {
            PlayTintedEffect(supernovaEffectPrefab, worldPosition,
                PrototypeStarColors.GetStageColor(PrototypeStarStage.Supernova));
        }

        public void PlayCollectAreaEffect(Vector3 worldPosition, float radius, PrototypeStarStage stage)
        {
            PlayTintedEffect(clickEffectPrefab, worldPosition, PrototypeStarColors.GetStageColor(stage));

            if (stardustEmitEffectPrefab == null)
                return;

            var instance = Instantiate(stardustEmitEffectPrefab, worldPosition, Quaternion.identity, transform);
            var shape = instance.shape;
            shape.radius = Mathf.Max(shape.radius, radius * 0.5f);
            var main = instance.main;
            main.startColor = PrototypeStarColors.GetStageColor(stage);
            instance.Play();
            Destroy(instance.gameObject, main.duration + main.startLifetime.constantMax + 0.35f);
        }

        private void PlayTintedEffect(ParticleSystem prefab, Vector3 worldPosition, Color color)
        {
            if (prefab == null)
                return;

            var instance = Instantiate(prefab, worldPosition, Quaternion.identity, transform);
            var main = instance.main;
            main.startColor = color;
            instance.Play();

            var lifetime = main.duration + main.startLifetime.constantMax + 0.35f;
            Destroy(instance.gameObject, lifetime);
        }
    }
}
