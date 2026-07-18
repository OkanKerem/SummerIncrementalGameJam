using UnityEngine;

namespace Universes.Game
{
    public static class GameSession
    {
        private const string LastGameSceneKey = "Universes_LastGameScene";

        public static bool HasContinueSave => RunSave.HasSave;

        public static string LastGameScene =>
            PlayerPrefs.GetString(LastGameSceneKey, "Step1");

        public static void MarkGameStarted(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            PlayerPrefs.SetString(LastGameSceneKey, sceneName);
            PlayerPrefs.Save();
        }

        public static void ClearContinue()
        {
            RunSave.Clear();
        }
    }
}
