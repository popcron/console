using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        }

        private static void OnUpdate()
        {
            if (!DoesConsoleWindowExist())
            {
                if (!Settings.Current)
                {
                    CreateSettings();
                }

                ConsoleWindow consoleWindow = ConsoleWindow.CreateConsoleWindow();
                consoleWindow.Initialize();
            }
        }

        private static void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            ClearAllConsoleWindows();
            CreateSettings();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredEditMode)
            {
                ClearAllConsoleWindows();
            }

            CreateSettings();
        }

        private static bool DoesConsoleWindowExist()
        {
            ConsoleWindow[] consoleWindows = Resources.FindObjectsOfTypeAll<ConsoleWindow>();
            for (int i = 0; i < consoleWindows.Length; i++)
            {
                ref ConsoleWindow consoleWindow = ref consoleWindows[i];
                if (consoleWindow)
                {
                    if (ConsoleWindow.All.Contains(consoleWindow))
                    {
                        return true;
                    }
                    else
                    {
                        Object.DestroyImmediate(consoleWindow);
                    }
                }
            }

            return false;
        }

        private static void ClearAllConsoleWindows()
        {
            ConsoleWindow[] consoleWindows = Resources.FindObjectsOfTypeAll<ConsoleWindow>();
            for (int i = consoleWindows.Length - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(consoleWindows[i].gameObject);
                }
                else
                {
                    Object.DestroyImmediate(consoleWindows[i].gameObject);
                }
            }
        }

        /// <summary>
        /// Puts a console object into the currently open scene.
        /// </summary>
        private static void CreateSettings()
        {
            if (!SettingsArePreloaded())
            {
                //make a file here
                string path = "Assets/Console Settings.asset";

                Settings settings = ScriptableObject.CreateInstance<Settings>();
                settings.name = "Console Settings";
                settings.consoleStyle = GetDefaultStyle();
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.Refresh();
                PreloadSettings(settings);
            }
        }

        /// <summary>
        /// Returns the default console style.
        /// </summary>
        private static GUIStyle GetDefaultStyle()
        {
            Font font = Resources.Load<Font>("CascadiaMono");
            GUIStyle consoleStyle = new GUIStyle
            {
                name = "Console",
                richText = true,
                alignment = TextAnchor.UpperLeft,
                font = font,
                fontSize = 14
            };

            consoleStyle.normal.textColor = Color.white;
            consoleStyle.hover.textColor = Color.white;
            consoleStyle.active.textColor = Color.white;
            return consoleStyle;
        }

        private static bool SettingsArePreloaded()
        {
            Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();
            foreach (Object preloadedAsset in preloadedAssets)
            {
                if (preloadedAsset && preloadedAsset is Settings)
                {
                    return true;
                }
            }

            return false;
        }

        private static void PreloadSettings(Settings settings)
        {
            List<Object> preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();
            bool preloaded = false;
            for (int i = 0; i < preloadedAssets.Count; i++)
            {
                Object preloadedAsset = preloadedAssets[i];
                if (!preloadedAsset || preloadedAsset is Settings)
                {
                    preloadedAssets[i] = settings;
                    preloaded = true;
                    break;
                }
            }

            if (!preloaded)
            {
                preloadedAssets.Add(settings);
            }

            PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
        }
    }
}
