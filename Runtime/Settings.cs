using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Popcron.Console
{
    public class Settings : ScriptableObject
    {
        private static StringBuilder stringBuilder = new StringBuilder();
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
        public static Settings Current => current;

        /// <summary>
        /// The hex value that represents the user typed text color.
        /// </summary>
        public string UserColor
        {
            get
            {
                if (string.IsNullOrEmpty(userColorHex))
                {
                    stringBuilder.Clear();
                    stringBuilder.Append("#");
                    stringBuilder.Append(ColorUtility.ToHtmlStringRGBA(userColor));
                    userColorHex = stringBuilder.ToString();
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
                if (string.IsNullOrEmpty(cachedLogFilePath))
                {
                    cachedLogFilePath = logFilePathPlayer;
#if UNITY_EDITOR
                    cachedLogFilePath = logFilePathEditor;
#endif

                    string root = Directory.GetParent(Application.dataPath).FullName;
                    if (cachedLogFilePath == "./")
                    {
                        //if its just the 2 chars, then its not valid!
                    }
                    else if (cachedLogFilePath.StartsWith("./"))
                    {
                        //replace those 2 chars with the root
                        cachedLogFilePath = cachedLogFilePath.Replace("./", $"{root}/");
                    }
                }

                return cachedLogFilePath;
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

#if UNITY_EDITOR
        public List<AssemblyDefinitionAsset> assemblyDefinitions = new List<AssemblyDefinitionAsset>();
        public List<SceneAsset> blacklistedScenes = new List<SceneAsset>();
#endif

        public List<string> assemblies = new List<string>();
        public List<string> blacklistedSceneNames = new List<string>();
        public List<string> startupCommands = new List<string>() { "info" };
        public GUIStyle consoleStyle = new GUIStyle();
        public int scrollAmount = 3;

        [SerializeField]
        private string formatting = "[{time} {type}] {text}";

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

        [NonSerialized]
        private string cachedLogFilePath;

        private void OnEnable()
        {
            current = this;
        }

        /// <summary>
        /// Is the current scene blacklisted and disallowed.
        /// </summary>
        public bool IsSceneBlacklisted()
        {
            Scene scene = SceneManager.GetActiveScene();
            string sceneName = scene.name;
            int sceneCount = blacklistedSceneNames.Count;
            for (int i = sceneCount - 1; i >= 0; i--)
            {
                if (blacklistedSceneNames[i].Equals(sceneName))
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
                    stringBuilder.Clear();
                    stringBuilder.Append("#");
                    stringBuilder.Append(ColorUtility.ToHtmlStringRGBA(logColor));
                    logColorHex = stringBuilder.ToString();
                }

                return logColorHex;
            }
            else if (type == LogType.Error)
            {
                if (string.IsNullOrEmpty(errorColorHex))
                {
                    stringBuilder.Clear();
                    stringBuilder.Append("#");
                    stringBuilder.Append(ColorUtility.ToHtmlStringRGBA(errorColor));
                    errorColorHex = stringBuilder.ToString();
                }

                return errorColorHex;
            }
            else if (type == LogType.Warning)
            {
                if (string.IsNullOrEmpty(warnColorHex))
                {
                    stringBuilder.Clear();
                    stringBuilder.Append("#");
                    stringBuilder.Append(ColorUtility.ToHtmlStringRGBA(warnColor));
                    warnColorHex = stringBuilder.ToString();
                }

                return warnColorHex;
            }
            else if (type == LogType.Exception)
            {
                if (string.IsNullOrEmpty(exceptionColorHex))
                {
                    stringBuilder.Clear();
                    stringBuilder.Append("#");
                    stringBuilder.Append(ColorUtility.ToHtmlStringRGBA(exceptionColor));
                    exceptionColorHex = stringBuilder.ToString();
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
                int length = consoleChararacters.Length;
                for (int i = length - 1; i >= 0; i--)
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
        /// Formats the text that is used to log to disk with.
        /// </summary>
        public string FormatText(string text, string logType)
        {
            if (text != null)
            {
                stringBuilder.Clear();
                stringBuilder.Append(formatting);
                stringBuilder.Replace("{time}", DateTime.Now.ToString());
                stringBuilder.Replace("{text}", text);
                stringBuilder.Replace("{type}", logType);
                return stringBuilder.ToString();
            }

            return null;
        }
    }
}
