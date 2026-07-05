using Universes.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Universes.Presentation
{
    public class CollapsePanelController : MonoBehaviour
    {
        [SerializeField] private UniverseRunController runController;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text breakdownText;
        [SerializeField] private Button continueButton;

        private void Start()
        {
            if (runController == null)
                runController = FindAnyObjectByType<UniverseRunController>();

            if (panel != null)
                panel.SetActive(false);

            continueButton?.onClick.AddListener(OnContinue);
            runController.OnRunCollapsed += ShowCollapse;
        }

        private void ShowCollapse(CollapseBreakdown breakdown)
        {
            if (panel == null)
                return;

            panel.SetActive(true);

            if (breakdownText != null)
            {
                breakdownText.supportRichText = true;
                breakdownText.text =
                    "Universe Collapsed!\n\n" +
                    $"Stardust: +{NumberFormatHelper.Format(breakdown.stardustComponent)}\n" +
                    $"Stars: +{NumberFormatHelper.Format(breakdown.starsComponent)}\n" +
                    $"Planets: +{NumberFormatHelper.Format(breakdown.planetsComponent)}\n" +
                    $"Life: +{NumberFormatHelper.Format(breakdown.lifeComponent)}\n" +
                    $"Supernovas: +{NumberFormatHelper.Format(breakdown.supernovaComponent)}\n" +
                    $"Survival: +{NumberFormatHelper.Format(breakdown.survivalComponent)}\n" +
                    $"Black Hole Seeds: +{NumberFormatHelper.Format(breakdown.blackHoleComponent)}\n\n" +
                    $"Multiplier: x{breakdown.multiplier:0.##}" +
                    (breakdown.earlyCollapse ? " (early collapse penalty)" : "") + "\n" +
                    $"\n<b>Universe DNA Gained: {NumberFormatHelper.Format(breakdown.dnaGained)}</b>";
            }
        }

        private void OnContinue()
        {
            if (panel != null)
                panel.SetActive(false);

            var picker = FindAnyObjectByType<VariantPickerController>();
            picker?.Show();
        }
    }
}
