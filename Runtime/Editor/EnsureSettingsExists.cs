#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Popcron.Console
{
    [InitializeOnLoad]
    public class EnsureSettingsExist
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RuntimeInitialize()
        {
            ClearAllConsoleWindows();
            ConsoleWindow.CreateConsoleWindow();
            CreateSettingsIfMissing();
        }

        static EnsureSettingsExist()
        {
            CreateSettingsIfMissing();
        }

        /// <summary>
        /// Clears all console windows found in all scenes.
        /// </summary>
        public static void ClearAllConsoleWindows()
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
        private static void CreateSettingsIfMissing()
        {
            if (!FindSettings())
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

        public static Settings FindSettings()
        {
            Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();
            foreach (Object preloadedAsset in preloadedAssets)
            {
                if (preloadedAsset && preloadedAsset is Settings)
                {
                    return preloadedAsset as Settings;
                }
            }

            string[] guids = AssetDatabase.FindAssets($"t:{typeof(Settings).FullName}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Settings settings = AssetDatabase.LoadAssetAtPath<Settings>(path);
                PreloadSettings(settings);
                return settings;
            }

            return null;
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
#endif