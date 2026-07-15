using UnityEngine;

namespace Universes.Core
{
    public class O_GameBootstrap : MonoBehaviour
    {
        [SerializeField] private O_UniverseRunController runController;

        private void Awake()
        {
            if (FindAnyObjectByType<O_RuntimeGameSetup>() == null && FindAnyObjectByType<O_UniverseRunController>() == null)
            {
                var setupGo = new GameObject("RuntimeGameSetup");
                setupGo.AddComponent<O_RuntimeGameSetup>();
            }

            if (FindAnyObjectByType<O_SaveSystem>() == null)
            {
                var saveGo = new GameObject("SaveSystem");
                saveGo.AddComponent<O_SaveSystem>();
            }

            if (runController == null)
                runController = FindAnyObjectByType<O_UniverseRunController>();
        }
    }
}
