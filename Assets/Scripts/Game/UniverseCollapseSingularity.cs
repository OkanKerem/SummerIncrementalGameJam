using UnityEngine;

namespace Universes.Game
{
    public class UniverseCollapseSingularity : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer coreRenderer;
        [SerializeField] private float spinSpeed = 120f;

        private float _maxScale = 3f;

        public static UniverseCollapseSingularity Spawn(UniverseCollapseSingularity prefab, Transform parent,
            Vector3 position, float maxScale)
        {
            if (prefab == null)
            {
                Debug.LogError("Collapse singularity prefab is not assigned on GameController.");
                return null;
            }

            var instance = Object.Instantiate(prefab, position, Quaternion.identity, parent);
            instance.Initialize(maxScale);
            return instance;
        }

        public void Initialize(float maxScale)
        {
            _maxScale = Mathf.Max(0.5f, maxScale);
            transform.localScale = Vector3.one * 0.15f;
        }

        public void SetCollapseProgress(float normalized)
        {
            var t = Mathf.Clamp01(normalized);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            var pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.04f;
            transform.localScale = Vector3.one * (_maxScale * eased * pulse);
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }
    }
}
