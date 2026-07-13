#if UNITY_EDITOR
using Universes.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Universes.Editor
{
    public static class PrototypeSfxEditor
    {
        [MenuItem("Universes/SFX/Add Or Select Prototype SFX Manager")]
        public static void AddOrSelectPrototypeSfxManager()
        {
            var controller = Object.FindAnyObjectByType<PrototypeGameController>();
            if (controller == null)
            {
                Debug.LogWarning("PrototypeGameController not found in the open scene.");
                return;
            }

            var sfxManager = EnsureSfxManager(controller);
            Selection.activeObject = sfxManager;
            EditorGUIUtility.PingObject(sfxManager);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("Prototype SFX Manager is ready. Assign clips on the selected component.");
        }

        public static PrototypeSfxManager EnsureSfxManager(PrototypeGameController controller)
        {
            var sfxManager = controller.GetComponent<PrototypeSfxManager>();
            if (sfxManager == null)
                sfxManager = controller.gameObject.AddComponent<PrototypeSfxManager>();

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
