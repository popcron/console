using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Popcron.Console
{
    public class Settings : ScriptableObject
    {
        public enum DefineSymbolsMode
        {
            EnableIfContains,
            EnableIfAllExist,
            AlwaysInclude
        }

        private static Settings current;
        private static Texture2D pixel;

        /// <summary>
        /// Returns a single 1x1 pixel that has some alpha.
        /// </summary>
        public static Texture2D Pixel
        {
            get
            {
                if (!pixel)
                {
                    pixel = new Texture2D(1, 1);
                    pixel.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
                    pixel.Apply();
                }

                return pixel;
            }
        }

        /// <summary>
        /// The current settings data being used.
        /// </summary>
        public static Settings Current
        {
            get
            {
                if (!current)
                {
                    current = GetOrCreate();
                }

                return current;
            }
        }

        /// <summary>
        /// The hex value that represents the user typed text color.
        /// </summary>
        public string UserColor
        {
            get
            {
                if (string.IsNullOrEmpty(userColorHex))
                {
                    userColorHex = $"#{ColorUtility.ToHtmlStringRGBA(userColor)}";
                }

                return userColorHex;
            }
        }

        /// <summary>
        /// Returns the correct path to the log file.
        /// </summary>
        public string LogFilePath
        {
            get
            {
                string path = logFilePathPlayer;
#if UNITY_EDITOR
                path = logFilePathEditor;
#endif

                string root = Directory.GetParent(Application.dataPath).FullName;
                if (path == "./")
                {
                    //if its just the 2 chars, then its not valid!
                    return path;
                }
                else if (path.StartsWith("./"))
                {
                    //replace those 2 chars with the root
                    path = path.Replace("./", $"{root}/");
                }

                return path;
            }
        }

        [SerializeField]
        private Color userColor = Color.green;

        [SerializeField]
        private Color errorColor = Color.red;

        [SerializeField]
        private Color exceptionColor = new Color(0.7f, 0f, 0f, 1f);

        [SerializeField]
        private Color warnColor = Color.Lerp(Color.red, Color.yellow, 0.8f);

        [SerializeField]
        private Color logColor = Color.white;

        [SerializeField]
        private List<string> blacklistedScenes = new List<string>();

        public List<string> startupCommands = new List<string>() { "info" };
        public GUIStyle consoleStyle = new GUIStyle();
        public DefineSymbolsMode defineSymbolsMode = DefineSymbolsMode.AlwaysInclude;
        public List<string> defineSymbols = new List<string>();
        public bool defineSymbolsInvert = false;
        public int scrollAmount = 3;
        public int historySize = 1024;
        public bool logToFile = true;
        public bool checkForOpenInput = true;
        public bool reportUnknownCommand = true;

        [SerializeField]
        private string consoleChararacters = "`!~*^#\\Ё§";

        /*
        ` = common/english qwerty
        ! = common/english dvorak
        ~ = khmer
        * = turkish
        ^ = germany
        # = french dvorak/bepo standard
        \ = albanian qwertz
        Ё = russian
        § = ukranian
        */

        [SerializeField]
        private bool allowWarnings = false;

        [SerializeField]
        private bool allowLogs = true;

        [SerializeField]
        private bool allowExceptions = true;

        [SerializeField]
        private bool allowErrors = true;

        [SerializeField]
        private bool allowAsserts = true;

        [SerializeField]
        private string logFilePathPlayer = "./Log.txt";

        [SerializeField]
        private string logFilePathEditor = "./Assets/Log.txt";

        [NonSerialized]
        private string userColorHex;

        [NonSerialized]
        private string logColorHex;

        [NonSerialized]
        private string warnColorHex;

        [NonSerialized]
        private string errorColorHex;

        [NonSerialized]
        private string exceptionColorHex;

        /// <summary>
        /// Is the current scene blacklisted and disallowed.
        /// </summary>
        /// <returns></returns>
        public bool IsSceneBlacklisted()
        {
            Scene scene = SceneManager.GetActiveScene();
            for (int i = 0; i < blacklistedScenes.Count; i++)
            {
                if (blacklistedScenes[i].Equals(scene.path))
                {
                    return true;
                }
            }

            return false;
        }

        public string GetColor(LogType type)
        {
            if (type == LogType.Log)
            {
                if (string.IsNullOrEmpty(logColorHex))
                {
                    logColorHex = $"#{ColorUtility.ToHtmlStringRGBA(logColor)}";
                }

                return logColorHex;
            }
            else if (type == LogType.Error)
            {
                if (string.IsNullOrEmpty(errorColorHex))
                {
                    errorColorHex = $"#{ColorUtility.ToHtmlStringRGBA(errorColor)}";
                }

                return errorColorHex;
            }
            else if (type == LogType.Warning)
            {
                if (string.IsNullOrEmpty(warnColorHex))
                {
                    warnColorHex = $"#{ColorUtility.ToHtmlStringRGBA(warnColor)}";
                }

                return warnColorHex;
            }
            else if (type == LogType.Exception)
            {
                if (string.IsNullOrEmpty(exceptionColorHex))
                {
                    exceptionColorHex = $"#{ColorUtility.ToHtmlStringRGBA(exceptionColor)}";
                }

                return exceptionColorHex;
            }
            else
            {
                return "gray";
            }
        }

        /// <summary>
        /// Returns true if this character is for opening the console.
        /// </summary>
        public bool IsConsoleOpenChar(char c)
        {
            if (!string.IsNullOrEmpty(consoleChararacters))
            {
                for (int i = 0; i < consoleChararacters.Length; i++)
                {
                    if (consoleChararacters[i] == c)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Is this type of log allowed?
        /// </summary>
        public bool IsAllowed(LogType logType)
        {
            if (logType == LogType.Assert)
            {
                return allowAsserts;
            }
            else if (logType == LogType.Error)
            {
                return allowErrors;
            }
            else if (logType == LogType.Exception)
            {
                return allowExceptions;
            }
            else if (logType == LogType.Log)
            {
                return allowLogs;
            }
            else if (logType == LogType.Warning)
            {
                return allowWarnings;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns an existing console settings asset, or creates a new one if none exist.
        /// </summary>
        public static Settings GetOrCreate()
        {
            //find from resources
            Settings settings = Resources.Load<Settings>("Console Settings");
            bool exists = settings;
            if (!exists)
            {
                //no console settings asset exists yet, so create one
                settings = CreateInstance<Settings>();
                settings.name = "Console Settings";
                settings.consoleStyle = GetDefaultStyle();
            }

#if UNITY_EDITOR
            if (!exists)
            {
                //ensure the resources folder exists
                if (!Directory.Exists("Assets/Resources"))
                {
                    Directory.CreateDirectory("Assets/Resources");
                }

                //make a file here
                string path = "Assets/Resources/Console Settings.asset";
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.Refresh();
            }
#endif

            return settings;
        }

        /// <summary>
        /// Returns the default console style.
        /// </summary>
        public static GUIStyle GetDefaultStyle()
        {
            Font font = Resources.Load<Font>("Hack-Regular");
            GUIStyle consoleStyle = new GUIStyle
            {
                name = "Console",
                richText = true,
                alignment = TextAnchor.UpperLeft,
                font = font,
                fontSize = 12
            };

            consoleStyle.normal.textColor = Color.white;
            consoleStyle.hover.textColor = Color.white;
            consoleStyle.active.textColor = Color.white;
            return consoleStyle;
        }
    }
}
