using Universes.Stars;
using TMPro;
using UnityEngine;

namespace Universes.Presentation
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class StarView : MonoBehaviour
    {
        private static readonly Color Yellow = new(1f, 0.92f, 0.3f);
        private static readonly Color Orange = new(1f, 0.55f, 0.2f);
        private static readonly Color Red = new(0.95f, 0.25f, 0.2f);
        private static readonly Color Supernova = new(1f, 1f, 1f);
        private static readonly Color Dead = new(0.3f, 0.3f, 0.35f);

        [SerializeField] private TMP_Text nameLabel;

        private Star _star;
        private StarManager _manager;
        private SpriteRenderer _sprite;
        private float _pulse;

        public void Bind(Star star, StarManager manager)
        {
            _star = star;
            _manager = manager;
            _sprite = GetComponent<SpriteRenderer>();
            RefreshVisual();
            RefreshNameLabel();
        }

        private void Update()
        {
            if (_star == null || _star.Stage != StarStage.Supernova)
                return;

            _pulse += Time.deltaTime * 8f;
            var scale = 1f + Mathf.Sin(_pulse) * 0.15f;
            transform.localScale = Vector3.one * scale;
        }

        public void RefreshVisual()
        {
            if (_star == null || _sprite == null)
                return;

            _sprite.color = _star.Stage switch
            {
                StarStage.Yellow => Yellow,
                StarStage.Orange => Orange,
                StarStage.RedGiant => Red,
                StarStage.Supernova => Supernova,
                _ => Dead
            };

            transform.localScale = _star.Stage switch
            {
                StarStage.RedGiant => Vector3.one * 1.4f,
                StarStage.Supernova => Vector3.one * 1.6f,
                _ => Vector3.one
            };

            RefreshNameLabel();
        }

        private void RefreshNameLabel()
        {
            ResolveNameLabel();

            if (nameLabel == null || _star == null)
                return;

            var hasName = !string.IsNullOrWhiteSpace(_star.Name);
            nameLabel.enabled = hasName;
            if (hasName)
                nameLabel.text = _star.Name;
        }

        private void ResolveNameLabel()
        {
            if (nameLabel != null)
                return;

            var existing = transform.Find("StarNameLabel");
            if (existing != null)
                nameLabel = existing.GetComponent<TMP_Text>();

            if (nameLabel == null)
                nameLabel = GetComponentInChildren<TMP_Text>(true);
        }

        public void PlayDeathEffect()
        {
            if (_sprite != null)
                _sprite.color = Dead;
        }

        private void OnMouseDown()
        {
            if (_manager != null && _star != null)
                _manager.OnStarClicked(_star);
        }
    }
}
