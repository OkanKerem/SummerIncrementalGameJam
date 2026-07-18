using UnityEngine;
using UnityEngine.UI;

namespace Universes.Game
{
    public class SpeciesEntryView : MonoBehaviour
    {
        [SerializeField] private Button portraitButton;
        [SerializeField] private Image portraitImage;
        [SerializeField] private SpeciesPortraitMount portraitMount;
        [SerializeField] private GameObject alienPortraitRoot;
        [SerializeField] private Text speciesNameText;

        private Planet _planet;
        private SpeciesPanel _panel;

        public void Bind(Planet planet, SpeciesPanel panel, int colonyCount = 1)
        {
            _planet = planet;
            _panel = panel;

            if (planet == null)
                return;

            if (speciesNameText != null)
            {
                speciesNameText.text = planet.HasSpecies
                    ? colonyCount > 1
                        ? $"{planet.SpeciesName} ({colonyCount} worlds)"
                        : planet.SpeciesName
                    : "No species yet";
            }

            var directAlien = GetDirectAlienPortraitRoot();
            if (directAlien != null)
                SpeciesPortraitPool.ConfigurePortrait(directAlien, planet);

            if (portraitImage != null)
                portraitImage.color = SpeciesPortraitPool.GetSpeciesColor(planet);

            if (portraitMount != null && directAlien == null)
                portraitMount.Bind(planet, panel != null ? panel.PortraitPool : null);

            if (portraitButton == null)
                portraitButton = GetPortraitButton(directAlien);

            if (portraitButton != null)
            {
                portraitButton.onClick.RemoveListener(OnPortraitClicked);
                portraitButton.onClick.AddListener(OnPortraitClicked);
            }
        }

        private void OnDestroy()
        {
            if (portraitButton != null)
                portraitButton.onClick.RemoveListener(OnPortraitClicked);
        }

        private void OnPortraitClicked() => _panel?.ShowDetails(_planet);

        public void ReleasePortrait()
        {
            if (portraitMount != null)
                portraitMount.Release();
        }

        private GameObject GetDirectAlienPortraitRoot()
        {
            if (alienPortraitRoot != null)
                return alienPortraitRoot;

            var alien = transform.Find("Alien");
            if (alien != null)
            {
                alienPortraitRoot = alien.gameObject;
                return alienPortraitRoot;
            }

            foreach (Transform child in transform)
            {
                if (child.Find("Faces") != null && child.Find("Eyes") != null)
                {
                    alienPortraitRoot = child.gameObject;
                    return alienPortraitRoot;
                }
            }

            return null;
        }

        private Button GetPortraitButton(GameObject directAlien)
        {
            if (directAlien != null && directAlien.TryGetComponent<Button>(out var directButton))
                return directButton;

            return directAlien != null
                ? directAlien.GetComponentInChildren<Button>(true)
                : GetComponentInChildren<Button>(true);
        }
    }
}
