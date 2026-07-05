using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Universes.Prototype
{
    public class PrototypeFloatingText : MonoBehaviour
    {
        [SerializeField] private float riseSpeed = 110f;
        [SerializeField] private float duration = 0.9f;
        [SerializeField] private float startScale = 0.2f;
        [SerializeField] private float peakScale = 1.45f;

        private Text _label;
        private RectTransform _rect;
        private Camera _camera;
        private float _elapsed;
        private Vector3 _startScreenPos;
        private Color _baseColor;

        public void Init(Canvas canvas, Camera camera, Vector3 worldPosition, string text, Color color)
        {
            _camera = camera;
            _rect = GetComponent<RectTransform>();
            _label = GetComponent<Text>();
            _baseColor = color;

            if (_label != null)
            {
                _label.text = text;
                _label.color = color;
            }

            transform.SetParent(canvas.transform, false);
            _startScreenPos = WorldToScreen(worldPosition);
            _rect.position = _startScreenPos;
            _rect.localScale = Vector3.one * startScale;

            StartCoroutine(AnimatePopAndRise());
        }

        private IEnumerator AnimatePopAndRise()
        {
            while (_elapsed < duration)
            {
                _elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(_elapsed / duration);

                var pos = _startScreenPos;
                pos.y += riseSpeed * _elapsed;
                _rect.position = pos;

                float scale;
                if (t < 0.22f)
                {
                    var popT = t / 0.22f;
                    scale = Mathf.LerpUnclamped(startScale, peakScale, EaseOutBack(popT));
                }
                else if (t < 0.45f)
                {
                    var settleT = (t - 0.22f) / 0.23f;
                    scale = Mathf.Lerp(peakScale, 1f, EaseOutCubic(settleT));
                }
                else
                {
                    scale = 1f;
                }

                _rect.localScale = Vector3.one * scale;

                if (_label != null)
                {
                    var c = _baseColor;
                    c.a = t < 0.55f ? 1f : 1f - ((t - 0.55f) / 0.45f);
                    _label.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private Vector3 WorldToScreen(Vector3 world)
        {
            if (_camera == null)
                _camera = Camera.main;

            return _camera != null ? _camera.WorldToScreenPoint(world) : world;
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
