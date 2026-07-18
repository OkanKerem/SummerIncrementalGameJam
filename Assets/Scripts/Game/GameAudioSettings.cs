using UnityEngine;

namespace Universes.Game
{
    public static class GameAudioSettings
    {
        private const string MusicVolumeKey = "Universes_MusicVolume";
        private const string EffectsVolumeKey = "Universes_EffectsVolume";
        private const string LegacyMusicEnabledKey = "Universes_MusicEnabled";
        private const string LegacyEffectsEnabledKey = "Universes_EffectsEnabled";

        public static float MusicVolume
        {
            get => GetVolume(MusicVolumeKey, LegacyMusicEnabledKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static float EffectsVolume
        {
            get => GetVolume(EffectsVolumeKey, LegacyEffectsEnabledKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(EffectsVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static bool MusicEnabled => MusicVolume > 0.001f;
        public static bool EffectsEnabled => EffectsVolume > 0.001f;

        public static event System.Action Changed;

        private static float GetVolume(string volumeKey, string legacyEnabledKey, float defaultValue)
        {
            if (PlayerPrefs.HasKey(volumeKey))
                return Mathf.Clamp01(PlayerPrefs.GetFloat(volumeKey, defaultValue));

            if (PlayerPrefs.HasKey(legacyEnabledKey))
                return PlayerPrefs.GetInt(legacyEnabledKey, 1) == 1 ? defaultValue : 0f;

            return defaultValue;
        }
    }
}
