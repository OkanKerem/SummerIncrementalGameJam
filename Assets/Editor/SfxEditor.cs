#if UNITY_EDITOR
using Universes.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Universes.Editor
{
    public static class SfxEditor
    {
        [MenuItem("Universes/SFX/Add Or Select SFX Manager")]
        public static void AddOrSelectSfxManager()
        {
            var controller = Object.FindAnyObjectByType<GameController>();
            if (controller == null)
            {
                Debug.LogWarning("GameController not found in the open scene.");
                return;
            }

            var sfxManager = EnsureSfxManager(controller);
            Selection.activeObject = sfxManager;
            EditorGUIUtility.PingObject(sfxManager);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("SFX Manager is ready. Assign clips on the selected component.");
        }

        public static SfxManager EnsureSfxManager(GameController controller)
        {
            var sfxManager = controller.GetComponent<SfxManager>();
            if (sfxManager == null)
                sfxManager = controller.gameObject.AddComponent<SfxManager>();

            var audioSources = controller.GetComponents<AudioSource>();
            var sfxSource = audioSources.Length > 0
                ? audioSources[0]
                : controller.gameObject.AddComponent<AudioSource>();
            var musicSource = audioSources.Length > 1
                ? audioSources[1]
                : controller.gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            musicSource.playOnAwake = false;
            musicSource.loop = true;

            SetRef(sfxManager, "audioSource", sfxSource);
            SetRef(sfxManager, "musicSource", musicSource);
            SetRef(controller, "sfxManager", sfxManager);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(sfxManager);
            EditorUtility.SetDirty(sfxSource);
            EditorUtility.SetDirty(musicSource);

            return sfxManager;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
                return;

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
