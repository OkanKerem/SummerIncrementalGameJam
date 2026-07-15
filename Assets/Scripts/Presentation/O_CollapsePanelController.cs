using Universes.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Universes.Presentation
{
    public class O_CollapsePanelController : MonoBehaviour
    {
        [SerializeField] private O_UniverseRunController runController;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text breakdownText;
        [SerializeField] private Button continueButton;

        private void Start()
        {
            if (runController == null)
                runController = FindAnyObjectByType<O_UniverseRunController>();

            if (panel != null)
                panel.SetActive(false);

            continueButton?.onClick.AddListener(OnContinue);
            runController.OnRunCollapsed += ShowCollapse;
        }

        private void ShowCollapse(O_CollapseBreakdown breakdown)
        {
            if (panel == null)
                return;

            panel.SetActive(true);

            if (breakdownText != null)
            {
                breakdownText.supportRichText = true;
                breakdownText.text =
                    "Universe Collapsed!\n\n" +
                    $"Stardust: +{O_NumberFormatHelper.Format(breakdown.stardustComponent)}\n" +
                    $"Stars: +{O_NumberFormatHelper.Format(breakdown.starsComponent)}\n" +
                    $"Planets: +{O_NumberFormatHelper.Format(breakdown.planetsComponent)}\n" +
                    $"Life: +{O_NumberFormatHelper.Format(breakdown.lifeComponent)}\n" +
                    $"Supernovas: +{O_NumberFormatHelper.Format(breakdown.supernovaComponent)}\n" +
                    $"Survival: +{O_NumberFormatHelper.Format(breakdown.survivalComponent)}\n" +
                    $"Black Hole Seeds: +{O_NumberFormatHelper.Format(breakdown.blackHoleComponent)}\n\n" +
                    $"Multiplier: x{breakdown.multiplier:0.##}" +
                    (breakdown.earlyCollapse ? " (early collapse penalty)" : "") + "\n" +
                    $"\n<b>Universe DNA Gained: {O_NumberFormatHelper.Format(breakdown.dnaGained)}</b>";
            }
        }

        private void OnContinue()
        {
            if (panel != null)
                panel.SetActive(false);

            var picker = FindAnyObjectByType<O_VariantPickerController>();
            picker?.Show();
        }
    }
}
