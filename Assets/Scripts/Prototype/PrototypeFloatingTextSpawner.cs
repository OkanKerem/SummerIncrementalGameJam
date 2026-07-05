using UnityEngine;
using UnityEngine.UI;

namespace Universes.Prototype
{
    public class PrototypeFloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Font font;
        [SerializeField] private int fontSize = 26;

        public void Spawn(Vector3 worldPosition, int amount, PrototypeStarStage stage)
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
                return;

            var go = new GameObject("FloatingStardust");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);

            var text = go.AddComponent<Text>();
            text.text = $"+{amount}";
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            var color = PrototypeStarColors.GetStageColor(stage);
            color.a = 1f;
            text.color = color;

            var floater = go.AddComponent<PrototypeFloatingText>();
            floater.Init(canvas, Camera.main, worldPosition + Vector3.up * 0.35f, $"+{amount}", color);
        }
    }
}
