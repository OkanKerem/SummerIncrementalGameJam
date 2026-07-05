using UnityEngine;

namespace Universes.Prototype
{
    public class PrototypePrestigePanel : MonoBehaviour
    {
        [SerializeField] private PrototypeGameController controller;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private PrototypePrestigeUpgradeRow[] upgradeRows;

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<PrototypeGameController>();

            if (panelRoot == null)
                panelRoot = gameObject;
        }

        private void Start()
        {
            if (upgradeRows == null || upgradeRows.Length == 0)
                upgradeRows = GetComponentsInChildren<PrototypePrestigeUpgradeRow>(true);

            foreach (var row in upgradeRows)
            {
                if (row != null)
                    row.Initialize(controller);
            }

            if (controller != null)
            {
                controller.Prestige.OnChanged += RefreshAll;
                controller.OnUniverseCollapsed += OnUniverseCollapsed;
                controller.OnStateChanged += OnStateChanged;
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (controller == null)
                return;

            controller.Prestige.OnChanged -= RefreshAll;
            controller.OnUniverseCollapsed -= OnUniverseCollapsed;
            controller.OnStateChanged -= OnStateChanged;
        }

        private void OnUniverseCollapsed(bool _) => Show();

        private void OnStateChanged()
        {
            if (controller != null && !controller.IsCollapsed)
                Hide();
        }

        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            RefreshAll();
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
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
