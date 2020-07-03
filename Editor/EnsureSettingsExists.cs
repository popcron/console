using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Popcron.Console
{
    [InitializeOnLoad]
    public class EnsureSettingsExist
    {
        static EnsureSettingsExist()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        }

        private static void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            CreateSettings();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            CreateSettings();
        }

        /// <summary>
        /// Puts a console object into the currently open scene.
        /// </summary>
        public static void CreateSettings()
        {
            Settings.GetOrCreate();
        }
    }
}
