using UnityEngine;

namespace Universes.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private UniverseRunController runController;

        private void Awake()
        {
            if (FindAnyObjectByType<RuntimeGameSetup>() == null && FindAnyObjectByType<UniverseRunController>() == null)
            {
                var setupGo = new GameObject("RuntimeGameSetup");
                setupGo.AddComponent<RuntimeGameSetup>();
            }

            if (FindAnyObjectByType<SaveSystem>() == null)
            {
                var saveGo = new GameObject("SaveSystem");
                saveGo.AddComponent<SaveSystem>();
            }

            if (runController == null)
                runController = FindAnyObjectByType<UniverseRunController>();
        }
    }
}
