using UnityEngine;

namespace Universes.Prototype
{
    public class PrototypeUpgradePanel : MonoBehaviour
    {
        [SerializeField] private PrototypeGameController controller;
        [SerializeField] private PrototypeUpgradeRow[] upgradeRows;
        [SerializeField] private GameObject panelRoot;

        private void Start()
        {
            if (controller == null)
                controller = FindAnyObjectByType<PrototypeGameController>();

            if (panelRoot == null)
                panelRoot = gameObject;

            if (upgradeRows == null || upgradeRows.Length == 0)
                upgradeRows = GetComponentsInChildren<PrototypeUpgradeRow>(true);

            foreach (var row in upgradeRows)
            {
                if (row != null)
                    row.Initialize(controller);
            }

            if (controller != null)
            {
                controller.OnStateChanged += OnStateChanged;
                controller.OnUniverseCollapsed += OnUniverseCollapsed;
            }

            SetVisible(controller == null || !controller.IsCollapsed);
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.OnStateChanged -= OnStateChanged;
                controller.OnUniverseCollapsed -= OnUniverseCollapsed;
            }
        }

        private void OnStateChanged()
        {
            if (controller == null)
                return;

            if (!controller.IsCollapsed)
                SetVisible(true);

            RefreshAll();
        }

        private void OnUniverseCollapsed(bool _) => SetVisible(false);

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
                panelRoot.SetActive(visible);
        }

        private void RefreshAll()
        {
            if (upgradeRows == null)
                return;

            foreach (var row in upgradeRows)
            {
                if (row != null)
                    row.Refresh();
            }
        }
    }
}
