using UnityEngine;

namespace Universes.Game
{
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private UpgradeRow[] upgradeRows;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private bool hideMaxedUpgrades;

        private void Start()
        {
            if (controller == null)
                controller = FindAnyObjectByType<GameController>();

            if (panelRoot == null)
                panelRoot = gameObject;

            if (upgradeRows == null || upgradeRows.Length == 0)
                upgradeRows = GetComponentsInChildren<UpgradeRow>(true);

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

            SetVisible(controller == null || !controller.IsRunEnded);
            RefreshAll();
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

            if (!controller.IsRunEnded)
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
                    row.Refresh(hideMaxedUpgrades);
            }
        }

        public void SetHideMaxedUpgrades(bool hide)
        {
            hideMaxedUpgrades = hide;
            RefreshAll();
        }

        public void ToggleHideMaxedUpgrades()
        {
            hideMaxedUpgrades = !hideMaxedUpgrades;
            RefreshAll();
        }
    }
}
