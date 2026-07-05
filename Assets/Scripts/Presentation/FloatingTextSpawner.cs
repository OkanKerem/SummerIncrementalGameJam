using UnityEngine;
using UnityEngine.UI;

namespace Universes.Presentation
{
    public class FloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Font font;

        public void Spawn(Vector3 worldPosition, string text, Color color)
        {
            if (canvas == null)
                return;

            var go = new GameObject("FloatingText");
            go.transform.SetParent(canvas.transform, false);

            var rect = go.AddComponent<RectTransform>();
            var textComp = go.AddComponent<Text>();
            textComp.text = text;
            textComp.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = 18;
            textComp.color = color;
            textComp.alignment = TextAnchor.MiddleCenter;

            var cam = Camera.main;
            if (cam != null)
            {
                var screenPos = cam.WorldToScreenPoint(worldPosition);
                rect.position = screenPos;
            }

            go.AddComponent<FloatingTextBehaviour>();
        }
    }

    public class FloatingTextBehaviour : MonoBehaviour
    {
        private float _lifetime = 1.2f;
        private Text _text;
        private Vector3 _velocity = new(0, 40f, 0);

        private void Awake()
        {
            _text = GetComponent<Text>();
        }

        private void Update()
        {
            _lifetime -= Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;

            if (_text != null)
            {
                var c = _text.color;
                c.a = _lifetime;
                _text.color = c;
            }

            if (_lifetime <= 0)
                Destroy(gameObject);
        }
    }
}
