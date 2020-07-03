using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Popcron.Console
{
    [AddComponentMenu("")]
    public class ConsoleWindow : MonoBehaviour
    {
        private static ConsoleWindow instance;
        private const string ConsoleControlName = "ControlField";

        public delegate bool OnAboutToPrint(object obj, string text, LogType type);
        public delegate void OnPrinted(string text, LogType type);

        /// <summary>
        /// Gets executed after before a message is printed to the console.
        /// </summary>
        public static OnAboutToPrint onAboutToPrint;

        /// <summary>
        /// Gets executed after a message is added to the console window.
        /// </summary>
        public static OnPrinted onPrinted;

        private static StringBuilder suggestionsText = new StringBuilder();

        private static int MaxLines
        {
            get
            {
                int lines = Mathf.RoundToInt(Screen.height * 0.45f / 16);
                return Mathf.Clamp(lines, 4, 32);
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
                    instance = GetOrCreate();
                }

                return instance;
            }
        }

        /// <summary>
        /// The text that is currently given to the console window?
        /// </summary>
        public static string TextInput
        {
            get => Instance.textInput;
            set => Instance.textInput = Parser.Sanitize(value);
        }

        /// <summary>
        /// Is the console window currently open?
        /// </summary>
        public static bool IsOpen
        {
            get => Instance.isOpen;
            set
            {
                Instance.textInput = null;
                Instance.isOpen = value;
                if (value)
                {
                    Instance.typedSomething = false;
                    Instance.index = Instance.history.Count;
                    Instance.Search(null);
                }
            }
        }

        [Obsolete("This is obsolete, use Console.IsOpen instead.")]
        public static bool Open
        {
            get => Instance.isOpen;
            set => Instance.isOpen = value;
        }

        /// <summary>
        /// The amount of lines that a single scroll should perform.
        /// </summary>
        public static int ScrollPosition
        {
            get => Instance.scrollPosition;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                int historySize = Settings.Current.historySize;
                if (value >= historySize)
                {
                    value = historySize - 1;
                }

                Instance.scrollPosition = value;
            }
        }

        /// <summary>
        /// Read only list of commands that were submitted to the console.
        /// </summary>
        public static ReadOnlyCollection<string> History
        {
            get
            {
                //recreate if different
                if (Instance.historyReadOnly == null || Instance.historyReadOnly.Count != Instance.history.Count)
                {
                    Instance.historyReadOnly = Instance.history.AsReadOnly();
                }

                return Instance.historyReadOnly;
            }
        }

        /// <summary>
        /// Read only list of the text that was submitted to the console (without the rich text tags).
        /// </summary>
        public static ReadOnlyCollection<string> Text
        {
            get
            {
                //recreate if different
                if (Instance.textReadOnly == null || Instance.textReadOnly.Count != Instance.text.Count)
                {
                    Instance.textReadOnly = Instance.text.AsReadOnly();
                }

                return Instance.textReadOnly;
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

        private void Awake()
        {
            text.Clear();
            history.Clear();
            textInput = null;
            isOpen = false;
            index = 0;
            linesString = null;
            historyReadOnly = null;
            textReadOnly = null;
        }

        private void Start()
        {
            ClearLogFile();

            //run the init commands
            for (int i = 0; i < Settings.Current.startupCommands.Count; i++)
            {
                string command = Settings.Current.startupCommands[i];
                global::Console.Run(command);
            }
        }

        private void OnEnable()
        {
            Library.FindCategories();
            Library.FindCommands();
            Converter.FindConverters();

            //adds a listener for debug prints in editor
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string message, string stack, LogType logType)
        {
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
                StringBuilder builder = new StringBuilder(listOfBytes.Count * 5);
                int spacing = 25;
                for (int i = 0; i < listOfBytes.Count; i++)
                {
                    builder.Append(listOfBytes[i].ToString("000"));
                    builder.Append(" ");
                    if (i % spacing == 0 && i >= spacing)
                    {
                        builder.AppendLine();
                    }
                }

                return builder.ToString();
            }
            else if (message is byte[] arrayOfBytes)
            {
                StringBuilder builder = new StringBuilder(arrayOfBytes.Length * 5);
                int spacing = 25;
                for (int i = 0; i < arrayOfBytes.Length; i++)
                {
                    builder.Append(arrayOfBytes[i].ToString("000"));
                    builder.Append(" ");
                    if (i % spacing == 0 && i >= spacing)
                    {
                        builder.AppendLine();
                    }
                }

                return builder.ToString();
            }

            return message.ToString();
        }

        /// <summary>
        /// Clears the console.
        /// </summary>
        public static void Clear()
        {
            ScrollPosition = 0;
            TextInput = "";

            //clear text
            Instance.rawText.Clear();
            Instance.text.Clear();
            Instance.UpdateText();
        }

        /// <summary>
        /// Submit generic text to the console with an optional log type.
        /// </summary>
        public static void WriteLine(object obj, LogType type = LogType.Log)
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

            string color = Settings.Current.GetColor(type);
            Instance.Add(stringText, color);
            LogToFile(stringText, type.ToString());

            //invoke the printed event
            onPrinted?.Invoke(stringText, type);
        }

        private void ClearLogFile()
        {
            string path = Settings.Current.LogFilePath;
            if (File.Exists(path))
            {
                File.WriteAllText(path, "");
            }

            LogToFile("Hello World!");
        }

        private static void LogToFile(string text, string logType = null)
        {
            if (Settings.Current.logToFile)
            {
                string path = Settings.Current.LogFilePath;
                if (path == "./")
                {
                    //if its just the 2 chars, then its not valid!
                    Settings.Current.logToFile = false;
                    Debug.LogError($"Log file path is invalid, it must be a path to a file. Given path is {path}");
                    return;
                }

                //check if it has an extension, indicating a file
                if (!Path.HasExtension(path))
                {
                    Settings.Current.logToFile = false;
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
                using (StreamWriter stream = File.AppendText(path))
                {
                    stream.Write('[');
                    stream.Write(DateTime.Now.ToString());

                    //put the log type as long as its not null
                    if (!string.IsNullOrEmpty(logType))
                    {
                        stream.Write(' ');
                        stream.Write(logType.ToString());
                    }

                    stream.Write("] ");
                    stream.WriteLine(text);
                }
            }
        }

        private string RemoveRichText(string input)
        {
            //remove bold start tag
            int index = input.IndexOf("<b>");
            if (index != -1)
            {
                input = input.Replace("<b>", "");
            }

            //remove bold end tag
            index = input.IndexOf("</b>");
            if (index != -1)
            {
                input = input.Replace("</b>", "");
            }

            //remove italics start tag
            index = input.IndexOf("<i>");
            if (index != -1)
            {
                input = input.Replace("<i>", "");
            }

            //remove italics end tag
            index = input.IndexOf("</i>");
            if (index != -1)
            {
                input = input.Replace("</i>", "");
            }

            //remove all color end tags
            index = input.IndexOf("</color>");
            if (index != -1)
            {
                input = input.Replace("</color>", "");
            }

            while (true) //safe
            {
                //remove all color start tags
                index = input.IndexOf("<color=");
                if (index != -1)
                {
                    int end = -1;
                    for (int i = index; i < input.Length; i++)
                    {
                        if (input[i] == '>')
                        {
                            end = i;
                            break;
                        }
                    }

                    if (end != -1)
                    {
                        input = input.Remove(index, end - index);
                    }
                    else
                    {
                        //somehow there is no end to this color tag? so kys
                        break;
                    }
                }
                else
                {
                    //no more color start tags, so we can break
                    break;
                }
            }

            return input;
        }

        private void Add(string input, string color)
        {
            List<string> lines = new List<string>();
            string str = input.ToString();
            if (str.Contains("\n"))
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
                string line = $"<color={color}>{lines[i]}</color>";
                rawText.Add(line);

                string newLine = RemoveRichText(lines[i]);
                text.Add(newLine);

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

        //creates a single text to use when display the console
        private void UpdateText()
        {
            string[] lines = new string[MaxLines];
            int lineIndex = 0;
            for (int i = 0; i < rawText.Count; i++)
            {
                int index = i + scrollPosition;
                if (index < 0)
                {
                    continue;
                }
                else if (index >= rawText.Count)
                {
                    continue;
                }
                else if (string.IsNullOrEmpty(rawText[index]))
                {
                    break;
                }

                lines[lineIndex] = rawText[index];

                //replace all \t with 4 spaces
                lines[lineIndex] = lines[lineIndex].Replace("\t", "    ");

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
            if (text.Length > 1 && text.StartsWith(Parser.IDPrefix + " "))
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
            StringBuilder textBuilder = new StringBuilder();
            foreach (Category category in Library.Categories)
            {
                foreach (Command command in category.Commands)
                {
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

                        textBuilder.Append(string.Join("/", command.Names));
                        if (command.Member is MethodInfo)
                        {
                            foreach (string parameter in command.Parameters)
                            {
                                textBuilder.Append(" <");
                                textBuilder.Append(parameter);
                                textBuilder.Append(">");
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
        private void MoveToEnd()
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
            if (CheckForOpen())
            {
                IsOpen = !IsOpen;
                Event.current.Use();
                return;
            }

            //dont show the console if it shouldnt be open
            //duh
            if (!isOpen)
            {
                return;
            }

            //view scrolling
            if (Event.current.type == EventType.ScrollWheel)
            {
                int scrollDirection = (int)Mathf.Sign(Event.current.delta.y) * Settings.Current.scrollAmount;
                ScrollPosition += scrollDirection;
                UpdateText();
            }

            //history scolling
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.UpArrow)
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
                else if (Event.current.keyCode == KeyCode.DownArrow)
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
                MoveToEnd();
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
            suggestionsText.Clear();
            for (int i = 0; i < searchResults.Count; i++)
            {
                string searchResultText = Parser.Sanitize(searchResults[i].Text);
                if (i == searchResults.Count - 1)
                {
                    suggestionsText.Append(searchResultText);
                }
                else
                {
                    suggestionsText.AppendLine(searchResultText);
                }
            }

            string suggestions = suggestionsText.ToString();
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
                global::Console.Run(textInput);
                Event.current.Use();

                textInput = "";
                typedSomething = false;
            }
        }

        /// <summary>
        /// Returns an existing or creates a new console window instance with these settings.
        /// </summary>
        public static ConsoleWindow GetOrCreate()
        {
            //find a console window component in this scene
            ConsoleWindow consoleWindow = FindObjectOfType<ConsoleWindow>();
            if (!consoleWindow)
            {
                //none present, create a new hidden one
                consoleWindow = new GameObject("Console").AddComponent<ConsoleWindow>();
            }

            return consoleWindow;
        }
    }
}