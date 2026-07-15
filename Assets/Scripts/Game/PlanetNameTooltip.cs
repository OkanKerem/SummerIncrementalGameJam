using UnityEngine;

namespace Universes.Game
{
    [RequireComponent(typeof(PlanetView))]
    public class PlanetNameTooltip : MonoBehaviour
    {
        [SerializeField] private TextMesh label;
        [SerializeField] private Vector3 labelOffset = new(0f, 0.42f, 0f);

        private PlanetView _view;

        private void Awake()
        {
            _view = GetComponent<PlanetView>();
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (label == null || _view?.Planet == null)
                return;

            label.transform.position = transform.position + labelOffset;
            label.text = GetPlanetName();
        }

        private void OnMouseEnter() => SetVisible(true);

        private void OnMouseExit() => SetVisible(false);

        private void SetVisible(bool visible)
        {
            if (label != null)
                label.gameObject.SetActive(visible);
        }

        private string GetPlanetName()
        {
            var planet = _view.Planet;
            if (planet == null)
                return "Planet";

            return !string.IsNullOrWhiteSpace(planet.PlanetName)
                ? planet.PlanetName
                : PlanetTypeUtility.GetLabel(planet.Definition);
        }
    }
}
