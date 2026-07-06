using UnityEngine;

namespace Universes.Prototype
{
    public static class PrototypeExpansionSave
    {
        private const string ExpandedKey = "prototype_universe_expanded";

        public static bool IsExpanded() => PlayerPrefs.GetInt(ExpandedKey, 0) == 1;

        public static void SetExpanded(bool expanded)
        {
            PlayerPrefs.SetInt(ExpandedKey, expanded ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
