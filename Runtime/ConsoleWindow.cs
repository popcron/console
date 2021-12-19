#pragma warning disable CS0162 //unreachable code detected

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using C = Console;

namespace Popcron.Console
{
    [AddComponentMenu("")]
    public class ConsoleWindow : MonoBehaviour
    {
        /// <summary>
        /// All console windows in the scene.
        /// </summary>
        public static List<ConsoleWindow> All { get; private set; } = new List<ConsoleWindow>();

        private static ConsoleWindow instance;
        private const string ConsoleControlName = "ControlField";
        private static ReadOnlyCollection<string> emptyHistory;
        private static ReadOnlyCollection<string> emptyText;
        private static List<(object, LogType)> lazyWriteLineOperations = new List<(object, LogType)>();
        private static Dictionary<LogType, string> logTypeToString = new Dictionary<LogType, string>();
        private static StringBuilder textBuilder = new StringBuilder();

        public delegate bool OnAboutToPrint(object obj, string text, LogType type);
        public delegate void OnPrinted(string text, LogType type);

        public static OnAboutToPrint onAboutToPrint;
        public static OnPrinted onPrinted;

        /// <summary>
        /// The maximum amount of lines that can be shown on screen.
        /// </summary>
        private static int MaxLines
        {
            get
            {
                float fontSize = Settings.Current.FontSize;
                int lines = Mathf.RoundToInt(Screen.height * 0.45f / fontSize);
                return Mathf.Clamp(lines, 4, 64);
            }
        }

        /// <summary>
        /// The console instance.
        /// </summary>
        private static ConsoleWindow Instance
        {
            get
            {
                if (!instance)
                {
#if UNITY_EDITOR
                    ConsoleWindow[] consoleWindows = Resources.FindObjectsOfTypeAll<ConsoleWindow>();
                    if (consoleWindows.Length > 0)
                    {
                        instance = consoleWindows[0];
                    }
#else
                    CreateConsoleWindow();
#endif
                }

                return instance;
            }
        }

        /// <summary>
        /// The text that is currently given to the console window?
        /// </summary>
        public static string TextInput
        {
            get
            {
                if (!C.IsIncluded)
                {
                    return null;
                }

                ConsoleWindow instance = Instance;
                if (instance)
                {
                    return instance.textInput;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (!C.IsIncluded)
                {
                    return;
                }

                ConsoleWindow instance = Instance;
                if (instance)
                {
                    instance.textInput = Parser.Sanitize(value);
                }
            }
        }

        /// <summary>
        /// Is the console window currently open?
        /// </summary>
        public static bool IsOpen
        {
            get
            {
                if (!C.IsIncluded)
                {
                    return false;
                }

                ConsoleWindow instance = Instance;
                if (instance)
                {
                    return instance.isOpen;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (!C.IsIncluded)
                {
                    return;
                }

                ConsoleWindow instance = Instance;
                if (instance)
                {
                    instance.textInput = null;
                    instance.isOpen = value;
                    if (value)
                    {
                        instance.typedSomething = false;
                        instance.index = Instance.history.Count;
                        instance.Search(null);
                    }
                }
            }
        }

        [Obsolete("This is obsolete, use Console.IsOpen instead.")]
        public static bool Open
        {
            get => IsOpen;
            set => IsOpen = value;
        }

        /// <summary>
        /// The scroll position of the text in the window.
        /// </summary>
        public static int ScrollPosition
        {
            get
            {
                if (!C.IsIncluded)
                {
                    return 0;
                }

                ConsoleWindow instance = Instance;
                if (instance)
                {
                    return instance.scrollPosition;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (!C.IsIncluded)
                {
                    return;
                }

                if (value < 0)
                {
                    value = 0;
                }

                int historySize = Settings.Current.historySize;
                if (value >= historySize)
                {
                    value = historySize - 1;
                }

                ConsoleWindow instance = Instance;
                if (instance)
                {
                    instance.scrollPosition = value;
                }
            }
        }

        /// <summary>
        /// Read only list of commands that were submitted to the console.
        /// </summary>
        public static ReadOnlyCollection<string> History
        {
            get
            {
                if (!C.IsIncluded)
                {
                    if (emptyHistory == null)
                    {
                        emptyHistory = new List<string>().AsReadOnly();
                    }

                    return emptyHistory;
                }

                //recreate if different
                ConsoleWindow instance = Instance;
                if (instance)
                {
                    if (instance.historyReadOnly == null || instance.historyReadOnly.Count != instance.history.Count)
                    {
                        instance.historyReadOnly = instance.history.AsReadOnly();
                    }

                    return instance.historyReadOnly;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Read only list of the text that was submitted to the console (without the rich text tags).
        /// </summary>
        public static ReadOnlyCollection<string> Text
        {
            get
            {
                if (!C.IsIncluded)
                {
                    if (emptyText == null)
                    {
                        emptyText = new List<string>().AsReadOnly();
                    }

                    return emptyText;
                }

                //recreate if different
                ConsoleWindow instance = Instance;
                if (instance)
                {
                    if (instance.textReadOnly == null || instance.textReadOnly.Count != instance.text.Count)
                    {
                        instance.textReadOnly = instance.text.AsReadOnly();
                    }

                    return instance.textReadOnly;
                }
                else
                {
                    return null;
                }
            }
        }

        [SerializeField]
        private bool isOpen;

        [SerializeField]
        private List<string> text = new List<string>();

        [SerializeField]
        private List<string> history = new List<string>();

        private string textInput;
        private int scrollPosition;
        private int index;
        private int lastMaxLines;
        private Type keyboardType;
        private bool? hasKeyboardType;
        private int framePressedOn;
        private List<SearchResult> searchResults = new List<SearchResult>();
        private string linesString;
        private bool typedSomething;
        private List<string> rawText = new List<string>();
        private ReadOnlyCollection<string> historyReadOnly;
        private ReadOnlyCollection<string> textReadOnly;

        public void Initialize()
        {
            text.Clear();
            history.Clear();
            textInput = null;
            isOpen = false;
            index = 0;
            linesString = null;
            historyReadOnly = null;
            textReadOnly = null;

            if (Application.isPlaying)
            {
                ClearLogFile();
                WriteLine("Initialized");

                //run the init commands
                foreach (string command in Settings.Current.startupCommands)
                {
                    C.Run(command);
                }

                //print the lazy operations
                foreach ((object obj, LogType logType) operation in lazyWriteLineOperations)
                {
                    WriteLine(operation.obj, operation.logType);
                }

                lazyWriteLineOperations.Clear();
            }
        }

        private void OnEnable()
        {
            All.Add(this);
            Library.FindCategories();
            Library.FindCommands();
            Converter.FindConverters();

            //adds a listener for debug prints in editor
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            All.Remove(this);
            Application.logMessageReceived -= HandleLog;
        }

        private void OnApplicationQuit()
        {
            WriteLine("OnApplicationQuit");
        }

        private void HandleLog(string message, string stack, LogType logType)
        {
            if (!C.IsIncluded)
            {
                return;
            }

            //pass this log type through the filter first
            if (!Settings.Current.IsAllowed(logType))
            {
                return;
            }

            if (logType == LogType.Log)
            {
                //logs
                WriteLine(message, logType);
            }
            else if (logType == LogType.Warning)
            {
                //warnings
                WriteLine(message, logType);
            }
            else
            {
                //if any kind of error, print the stack as well
                WriteLine(message + "\n" + stack, logType);
            }
        }

        private static string GetStringFromObject(object message)
        {
            if (message == null)
            {
                return null;
            }

            if (message is string)
            {
                return message as string;
            }
            else if (message is List<byte> listOfBytes)
            {
                textBuilder.Clear();
                int spacing = 25;
                for (int i = 0; i < listOfBytes.Count; i++)
                {
                    textBuilder.Append(listOfBytes[i].ToString("000"));
                    textBuilder.Append(" ");
                    if (i % spacing == 0 && i >= spacing)
                    {
                        textBuilder.AppendLine();
                    }
                }

                return textBuilder.ToString();
            }
            else if (message is byte[] arrayOfBytes)
            {
                textBuilder.Clear();
                int spacing = 25;
                for (int i = 0; i < arrayOfBytes.Length; i++)
                {
                    textBuilder.Append(arrayOfBytes[i].ToString("000"));
                    textBuilder.Append(" ");
                    if (i % spacing == 0 && i >= spacing)
                    {
                        textBuilder.AppendLine();
                    }
                }

                return textBuilder.ToString();
            }

            return message.ToString();
        }

        /// <summary>
        /// Clears the console.
        /// </summary>
        public static void Clear()
        {
            if (!C.IsIncluded)
            {
                return;
            }

            ScrollPosition = 0;
            TextInput = "";

            //clear text
            ConsoleWindow instance = Instance;
            if (instance)
            {
                instance.rawText.Clear();
                instance.text.Clear();
                instance.UpdateText();
            }
        }

        private static void EnsureLogTypeToStringCacheExists()
        {
            if (logTypeToString.Count == 0)
            {
                logTypeToString[LogType.Assert] = "Assert";
                logTypeToString[LogType.Error] = "Error";
                logTypeToString[LogType.Exception] = "Exception";
                logTypeToString[LogType.Log] = "Log";
                logTypeToString[LogType.Warning] = "Warning";
            }
        }

        /// <summary>
        /// Submit generic text to the console with an optional log type.
        /// </summary>
        public static void WriteLine(object obj, LogType type = LogType.Log)
        {
            if (!C.IsIncluded)
            {
                return;
            }

            ConsoleWindow instance = Instance;
            if (instance)
            {
                string stringText = GetStringFromObject(obj);
                if (stringText == null)
                {
                    return;
                }

                //invoke the event
                if (onAboutToPrint?.Invoke(obj, stringText, type) == false)
                {
                    return;
                }

                EnsureLogTypeToStringCacheExists();
                string hexColor = Settings.Current.GetColor(type);
                instance.Add(stringText, hexColor);
                LogToFile(Parser.RemoveRichText(stringText), logTypeToString[type]);

                //invoke the printed event
                onPrinted?.Invoke(stringText, type);
            }
            else
            {
                lazyWriteLineOperations.Add((obj, type));
            }
        }

        /// <summary>
        /// Submit log text to the console with a specific hex color.
        /// </summary>
        public static void WriteLine(object obj, string hexColor)
        {
            if (!C.IsIncluded)
            {
                return;
            }

            ConsoleWindow instance = Instance;
            LogType type = LogType.Log;
            if (instance)
            {
                string stringText = GetStringFromObject(obj);
                if (stringText == null)
                {
                    return;
                }

                //invoke the event
                if (onAboutToPrint?.Invoke(obj, stringText, type) == false)
                {
                    return;
                }

                EnsureLogTypeToStringCacheExists();
                instance.Add(stringText, hexColor);
                LogToFile(Parser.RemoveRichText(stringText), logTypeToString[type]);

                //invoke the printed event
                onPrinted?.Invoke(stringText, type);
            }
            else
            {
                lazyWriteLineOperations.Add((obj, type));
            }
        }

        private void ClearLogFile()
        {
            string path = Settings.Current.LogFilePath;
            if (File.Exists(path))
            {
                File.WriteAllText(path, "");
            }
        }

        /// <summary>
        /// Writes this text with this log type to a file on disk.
        /// </summary>
        private static void LogToFile(string text, string logType = null)
        {
            Settings settings = Settings.Current;
            if (settings.logToFile)
            {
                string path = settings.LogFilePath;
                if (path == "./")
                {
                    //if its just the 2 chars, then its not valid!
                    settings.logToFile = false;
                    Debug.LogError($"Log file path is invalid, it must be a path to a file. Given path is {path}");
                    return;
                }

                //check if it has an extension, indicating a file
                if (!Path.HasExtension(path))
                {
                    settings.logToFile = false;
                    Debug.LogError($"Log file path is invalid, it must be a path to a file. Given path is {path}");
                    return;
                }

                //ensure dir exists
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    try
                    {
                        Directory.CreateDirectory(directory);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Tried to create dir at {directory}, but error with {e.GetType()}");
                    }
                }

                //append thy file
                text = settings.FormatText(text, logType);
                if (text != null)
                {
                    using (StreamWriter stream = File.AppendText(path))
                    {
                        stream.WriteLine(text);
                    }
                }
            }
        }

        private void Add(string input, string color)
        {
            List<string> lines = new List<string>();
            string str = input.ToString();
            if (str.IndexOf('\n') != -1)
            {
                lines.AddRange(str.Split('\n'));
            }
            else
            {
                lines.Add(str);
            }

            int historySize = Settings.Current.historySize;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                textBuilder.Clear();
                textBuilder.Append("<color=");
                textBuilder.Append(color);
                textBuilder.Append(">");
                textBuilder.Append(line);
                textBuilder.Append("</color>");

                rawText.Add(textBuilder.ToString());
                text.Add(Parser.RemoveRichText(line));

                //too many entries!
                if (rawText.Count > historySize)
                {
                    rawText.RemoveAt(0);
                    text.RemoveAt(0);
                }
            }

            int newScroll = rawText.Count - MaxLines;
            if (newScroll > scrollPosition)
            {
                //set scroll to bottom
                scrollPosition = newScroll;
            }

            //update the lines string
            UpdateText();
        }

        /// <summary>
        /// Creates a single text string to use when displaying the console.
        /// </summary>
        private void UpdateText()
        {
            string[] lines = new string[MaxLines];
            int lineIndex = 0;
            int rawTextCount = rawText.Count;
            for (int i = 0; i < rawTextCount; i++)
            {
                int index = i + scrollPosition;
                if (index < 0)
                {
                    continue;
                }
                else if (index >= rawTextCount)
                {
                    continue;
                }

                string rawTextLine = rawText[index];
                if (string.IsNullOrEmpty(rawTextLine))
                {
                    break;
                }

                //replace all \t with 4 spaces
                lines[lineIndex] = rawTextLine.Replace("\t", "    ");
                lineIndex++;
                if (lineIndex == MaxLines)
                {
                    break;
                }
            }

            linesString = string.Join("\n", lines);
        }

        private void Update()
        {
            //max lines amount changed
            if (lastMaxLines != MaxLines)
            {
                lastMaxLines = MaxLines;
                UpdateText();
            }
        }

        private bool Matches(string text, Command command)
        {
            if (command.Name.StartsWith(text))
            {
                return true;
            }
            else
            {
                //user typed in more than the command name, so check using the args
                List<string> args = Parser.GetParameters(text);
                int userArgs = args.Count;
                if (userArgs > 0)
                {
                    //check if the first arg is the command itself and that the args length is ok
                    if (args[0] == command.Name && userArgs - 1 <= command.Parameters.Count)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        private void Search(string text)
        {
            //search through all commands
            searchResults.Clear();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            //if it starts with a @, then concat
            bool instanceOnly = false;
            if (text.Length > 1 && text.StartsWith(Parser.IDPrefix))
            {
                int index = text.IndexOf(' ');
                if (index != -1)
                {
                    //remove everything before the space, which is what separates id from command
                    text = text.Substring(index + 1);
                    instanceOnly = true;
                    if (string.IsNullOrEmpty(text))
                    {
                        return;
                    }
                }
                else
                {
                    text = null;
                    return;
                }
            }

            //fill in the suggestion text
            int categoryCount = Library.Categories.Count;
            for (int y = 0; y < categoryCount; y++)
            {
                Category category = Library.Categories[y];
                int commandCount = category.Commands.Count;
                for (int c = 0; c < commandCount; c++)
                {
                    Command command = category.Commands[c];
                    if (Matches(text, command))
                    {
                        textBuilder.Clear();

                        //not static, so show that it needs an @id
                        if (!command.IsStatic)
                        {
                            textBuilder.Append(Parser.IDPrefix);
                            textBuilder.Append("id ");
                        }

                        if (instanceOnly && command.IsStatic)
                        {
                            continue;
                        }

                        //append the name
                        int nameCount = command.Names.Count;
                        for (int n = 0; n < nameCount; n++)
                        {
                            string name = command.Names[n];
                            textBuilder.Append(name);
                            if (n != nameCount - 1)
                            {
                                textBuilder.Append("/");
                            }
                        }

                        if (command.Member is MethodInfo)
                        {
                            foreach (string parameter in command.Parameters)
                            {
                                textBuilder.Append(" ");
                                textBuilder.Append(Parser.LeftAngleBracket);
                                textBuilder.Append(parameter);
                                textBuilder.Append(Parser.RightAngleBracket);
                            }
                        }
                        else if (command.Member is PropertyInfo property)
                        {
                            MethodInfo set = property.GetSetMethod();
                            if (set != null)
                            {
                                textBuilder.Append(" [value]");
                            }
                        }
                        else if (command.Member is FieldInfo)
                        {
                            textBuilder.Append(" [value]");
                        }

                        if (!string.IsNullOrEmpty(command.Description))
                        {
                            textBuilder.Append(" = ");
                            textBuilder.Append(command.Description);
                        }

                        SearchResult result = new SearchResult(textBuilder.ToString(), command.Name);
                        searchResults.Add(result);
                    }
                }
            }
        }

        /// <summary>
        /// Moves the caret to the end of the text field.
        /// </summary>
        private void MoveCaretToEnd()
        {
            TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (te != null)
            {
                te.MoveCursorToPosition(new Vector2(int.MaxValue, int.MaxValue));
            }
        }

        private Type GetKeyboardType()
        {
            if (keyboardType == null && hasKeyboardType != false)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int a = 0; a < assemblies.Length; a++)
                {
                    Type[] types = assemblies[a].GetTypes();
                    for (int t = 0; t < types.Length; t++)
                    {
                        if (types[t].FullName.Equals("UnityEngine.InputSystem.Keyboard"))
                        {
                            //found the keyboard type from the input system
                            keyboardType = types[t];
                            return keyboardType;
                        }
                    }
                }

                hasKeyboardType = false;
            }

            return null;
        }

        /// <summary>
        /// Returns true if the key to open the console window was opened.
        /// </summary>
        private bool CheckForOpen()
        {
            if (!Settings.Current.checkForOpenInput)
            {
                return false;
            }

            //try with input system if it exists
            Type keyboardType = GetKeyboardType();
            if (keyboardType != null)
            {
                //get the static `current` property and the `enterKey` property
                PropertyInfo current = keyboardType.GetProperty("current");
                PropertyInfo backquoteKey = keyboardType.GetProperty("backquoteKey");

                //get the value from it
                object keyboardValue = current.GetValue(null);
                if (keyboardValue != null)
                {
                    //get the `wasPressedThisFrame` property from the base type
                    //enter Key is a KeyControl, which derives from ButtonControl, which has this property
                    object enterKeyValue = backquoteKey.GetValue(keyboardValue);
                    PropertyInfo wasPressedThisFrame = enterKeyValue.GetType().BaseType.GetProperty("wasPressedThisFrame");

                    //get the value from it
                    bool yay = (bool)wasPressedThisFrame.GetValue(enterKeyValue);
                    if (yay)
                    {
                        if (framePressedOn != Time.frameCount)
                        {
                            framePressedOn = Time.frameCount;
                            return yay;
                        }
                    }
                }

                return false;
            }

            //try with the gui system
            if (Event.current.isKey && Event.current.type == EventType.KeyDown)
            {
                if (Settings.Current.IsConsoleOpenChar(Event.current.character))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if enter key was pressed.
        /// </summary>
        private bool CheckForEnter()
        {
            //try with input system if it exists
            Type keyboardType = GetKeyboardType();
            if (keyboardType != null)
            {
                //get the static `current` property and the `enterKey` property
                PropertyInfo current = keyboardType.GetProperty("current");
                PropertyInfo enterKey = keyboardType.GetProperty("enterKey");

                //get the value from it
                object keyboardValue = current.GetValue(null);
                if (keyboardValue != null)
                {
                    //get the `wasPressedThisFrame` property from the base type
                    //enter Key is a KeyControl, which derives from ButtonControl, which has this property
                    object enterKeyValue = enterKey.GetValue(keyboardValue);
                    PropertyInfo wasPressedThisFrame = enterKeyValue.GetType().BaseType.GetProperty("wasPressedThisFrame");

                    //get the value from it
                    bool yay = (bool)wasPressedThisFrame.GetValue(enterKeyValue);
                    if (yay)
                    {
                        if (framePressedOn != Time.frameCount)
                        {
                            framePressedOn = Time.frameCount;
                            return yay;
                        }
                    }
                }

                return false;
            }

            //try with the gui system
            if (Event.current.isKey && Event.current.type == EventType.KeyDown)
            {
                bool enter = Event.current.character == '\n' || Event.current.character == '\r' || Event.current.keyCode == KeyCode.Return;
                if (enter)
                {
                    return true;
                }
            }

            return false;
        }

        private GUIStyle GetStyle()
        {
            GUIStyle style = Settings.Current.consoleStyle;
            if (!style.normal.background)
            {
                style.normal.background = Settings.Pixel;
            }

            if (!style.hover.background)
            {
                style.hover.background = Settings.Pixel;
            }

            if (!style.active.background)
            {
                style.active.background = Settings.Pixel;
            }

            return style;
        }

        private void OnGUI()
        {
            bool moveToEnd = false;
            Event current = Event.current;
            if (CheckForOpen())
            {
                IsOpen = !IsOpen;
                current.Use();
                return;
            }

            //dont show the console if it shouldnt be open
            //duh
            if (!isOpen)
            {
                return;
            }

            //view scrolling
            if (current.type == EventType.ScrollWheel)
            {
                int scrollDirection = (int)Mathf.Sign(current.delta.y) * Settings.Current.scrollAmount;
                ScrollPosition += scrollDirection;
                UpdateText();
                current.Use();
                return;
            }

            //history scrolling
            if (current.type == EventType.KeyDown)
            {
                if (current.keyCode == KeyCode.UpArrow)
                {
                    if (!typedSomething)
                    {
                        index--;
                        if (index < -1)
                        {
                            index = -1;
                            textInput = "";
                            moveToEnd = true;
                        }
                        else
                        {
                            if (index >= 0 && index < history.Count)
                            {
                                textInput = history[index];
                                moveToEnd = true;
                            }
                        }
                    }
                    else
                    {
                        index--;
                        if (index <= -1)
                        {
                            index = -1;
                            textInput = "";
                            moveToEnd = true;
                        }
                        else
                        {
                            if (index >= 0 && index < searchResults.Count)
                            {
                                textInput = searchResults[index].Command;
                                moveToEnd = true;
                            }
                        }
                    }
                }
                else if (current.keyCode == KeyCode.DownArrow)
                {
                    if (!typedSomething)
                    {
                        index++;
                        if (index > history.Count)
                        {
                            index = history.Count;
                            textInput = "";
                            moveToEnd = true;
                        }
                        else
                        {
                            if (index >= 0 && index < history.Count)
                            {
                                textInput = history[index];
                                moveToEnd = true;
                            }
                        }
                    }
                    else
                    {
                        index++;
                        if (index >= searchResults.Count)
                        {
                            index = searchResults.Count;
                            textInput = "";
                            moveToEnd = true;
                        }
                        else
                        {
                            if (index >= 0 && index < searchResults.Count)
                            {
                                textInput = searchResults[index].Command;
                                moveToEnd = true;
                            }
                        }
                    }
                }
            }

            //go to home
            if (current.type == EventType.KeyDown)
            {
                if (current.keyCode == KeyCode.Home)
                {
                    ScrollPosition = 0;
                    UpdateText();
                    current.Use();
                    return;
                }
                else if (current.keyCode == KeyCode.End)
                {
                    ScrollPosition = rawText.Count - MaxLines;
                    UpdateText();
                    current.Use();
                    return;
                }
                else if (current.keyCode == KeyCode.PageUp)
                {
                    ScrollPosition -= MaxLines;
                    UpdateText();
                    current.Use();
                    return;
                }
                else if (current.keyCode == KeyCode.PageDown)
                {
                    ScrollPosition += MaxLines;
                    UpdateText();
                    current.Use();
                    return;
                }
            }

            //draw elements
            GUIStyle style = GetStyle();
            Color oldColor = GUI.color;
            GUI.color = Color.white;
            GUI.depth = -5;
            int lineHeight = style.fontSize + 4;

            GUILayout.Box(linesString, style, GUILayout.Width(Screen.width));
            Rect lastControl = GUILayoutUtility.GetLastRect();

            //draw the typing field
            GUI.Box(new Rect(0, lastControl.y + lastControl.height, Screen.width, 2), "", style);

            GUI.SetNextControlName(ConsoleControlName);
            string text = GUI.TextField(new Rect(0, lastControl.y + lastControl.height + 1, Screen.width, lineHeight), Parser.Sanitize(textInput), style);
            GUI.FocusControl(ConsoleControlName);

            if (moveToEnd)
            {
                MoveCaretToEnd();
            }

            //text changed, search
            if (textInput != text)
            {
                if (!typedSomething)
                {
                    typedSomething = true;
                    index = -1;
                }

                if (string.IsNullOrEmpty(text) && typedSomething)
                {
                    typedSomething = false;
                    index = history.Count;
                }

                textInput = text;
                Search(text);
            }

            //display the search suggestions
            GUI.color = new Color(1f, 1f, 1f, 0.4f);
            textBuilder.Clear();
            for (int i = 0; i < searchResults.Count; i++)
            {
                string searchResultText = Parser.Sanitize(searchResults[i].Text);
                if (i == searchResults.Count - 1)
                {
                    textBuilder.Append(searchResultText);
                }
                else
                {
                    textBuilder.AppendLine(searchResultText);
                }
            }

            string suggestions = textBuilder.ToString();
            GUI.Box(new Rect(0, lastControl.y + lastControl.height + 1 + lineHeight, Screen.width, searchResults.Count * lineHeight), suggestions, style);
            GUI.color = oldColor;

            //pressing enter to run command
            if (!string.IsNullOrEmpty(textInput) && CheckForEnter())
            {
                LogToFile(textInput, "Input");
                Add(textInput, Settings.Current.UserColor);

                //add to history
                history.Add(textInput);
                index = history.Count;

                Search(null);
                C.Run(textInput);
                Event.current.Use();

                textInput = "";
                typedSomething = false;
            }
        }

        /// <summary>
        /// Creates a new console window instance without initializing it.
        /// </summary>
        public static void CreateConsoleWindow()
        {
            if (!C.IsIncluded)
            {
                return;
            }

            //is this scene blacklisted?
            Settings settings = Settings.Current;
            if (settings && settings.IsSceneBlacklisted())
            {
                Scene currentScene = SceneManager.GetActiveScene();
                Debug.LogWarning($"Console window will not be created in blacklisted scene {currentScene.name}");
                return;
            }

            instance = new GameObject(nameof(ConsoleWindow)).AddComponent<ConsoleWindow>();
            const HideFlags Flags = HideFlags.DontSaveInBuild | HideFlags.NotEditable;
            instance.gameObject.hideFlags = Flags;
            instance.Initialize();
            DontDestroyOnLoad(instance.gameObject);
        }
    }
}