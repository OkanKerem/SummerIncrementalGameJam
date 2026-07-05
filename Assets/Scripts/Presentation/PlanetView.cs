using Universes.Planets;
using UnityEngine;

namespace Universes.Presentation
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlanetView : MonoBehaviour
    {
        private static readonly Color PlanetColor = new(0.4f, 0.6f, 0.95f);
        private static readonly Color LifeColor = new(0.3f, 0.9f, 0.5f);

        private Planet _planet;
        private Transform _orbitCenter;
        private SpriteRenderer _sprite;
        private float _orbitSpeed = 45f;

        public void Bind(Planet planet, Transform orbitCenter)
        {
            _planet = planet;
            _orbitCenter = orbitCenter;
            _sprite = GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.one * 0.25f;
            RefreshVisual();
            UpdatePosition();
        }

        public void TickOrbit(float deltaTime)
        {
            if (_planet == null || _orbitCenter == null)
                return;

            var angle = _planet.OrbitAngle + _orbitSpeed * deltaTime;
            SetOrbitAngle(angle);
            UpdatePosition();
        }

        private void SetOrbitAngle(float angle)
        {
            // Planet data is immutable for angle - we track visually
            _visualAngle = angle;
        }

        private float _visualAngle;

        private void UpdatePosition()
        {
            if (_orbitCenter == null || _planet == null)
                return;

            var rad = _visualAngle * Mathf.Deg2Rad;
            if (_visualAngle == 0)
                _visualAngle = _planet.OrbitAngle;

            rad = _visualAngle * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * _planet.OrbitRadius;
            transform.position = _orbitCenter.position + offset;
        }

        public void RefreshVisual()
        {
            if (_sprite == null || _planet == null)
                return;

            _sprite.color = _planet.HasLife ? LifeColor : PlanetColor;
            transform.localScale = _planet.HasLife ? Vector3.one * 0.32f : Vector3.one * 0.25f;
        }
    }
}
