#pragma warning disable CS0162 //unreachable code detected

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using C = global::Console;

namespace Popcron.Console
{
    [InitializeOnLoad]
    public class EnsureSettingsExist
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            ClearAllConsoleWindows();
            CreateConsoleWindow();
        }

        private static bool DoesConsoleWindowExist()
        {
            bool exists = false;
            ConsoleWindow[] consoleWindows = Resources.FindObjectsOfTypeAll<ConsoleWindow>();
            for (int i = 0; i < consoleWindows.Length; i++)
            {
                ref ConsoleWindow consoleWindow = ref consoleWindows[i];
                if (consoleWindow)
                {
                    if (!exists && ConsoleWindow.All.Contains(consoleWindow))
                    {
                        exists = true;
                    }
                    else
                    {
                        Object.DestroyImmediate(consoleWindow);
                    }
                }
            }

            return exists;
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

        /// <summary>
        /// Creates a new console window instance without initializing it.
        /// </summary>
        public static ConsoleWindow CreateConsoleWindow()
        {
            if (!C.IsIncluded)
            {
                return null;
            }

            //is this scene blacklisted?
            if (Settings.Current.IsSceneBlacklisted())
            {
                Scene currentScene = SceneManager.GetActiveScene();
                Debug.LogWarning($"Console window will not be created in blacklisted scene {currentScene.name}");
                return null;
            }

            ConsoleWindow consoleWindow = new GameObject(nameof(ConsoleWindow)).AddComponent<ConsoleWindow>();
            const HideFlags Flags = HideFlags.DontSaveInBuild | HideFlags.NotEditable;
            consoleWindow.gameObject.hideFlags = Flags;
            consoleWindow.Initialize();
            Object.DontDestroyOnLoad(consoleWindow.gameObject);
            return consoleWindow;
        }
    }
}
