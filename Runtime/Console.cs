using Popcron.Console;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

[AddComponentMenu("")]
public class Console : MonoBehaviour
{
    private const int HistorySize = 400;

    public delegate void OnPrint(string text, LogType type);

    /// <summary>
    /// Gets executed after a message is added to the console window.
    /// </summary>
    public static OnPrint onPrint;

    private const string ConsoleControlName = "ControlField";
    private const string PrintColor = "white";
    private const string WarningColor = "orange";
    private const string ErrorColor = "red";
    private const string UserColor = "lime";

    private static Console instance;

    private static Texture2D Pixel
    {
        get
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.8f));
            texture.Apply();
            return texture;
        }
    }

    private static int MaxLines
    {
        get
        {
            int lines = Mathf.RoundToInt(Screen.height * 0.45f / 16);
            return Mathf.Clamp(lines, 4, 32);
        }
    }

    private static Console Instance
    {
        get
        {
            if (!instance)
            {
                GameObject consoleGameObject = GameObject.Find("Console");
                if (!consoleGameObject)
                {
                    consoleGameObject = new GameObject("Console");
                }

                Console console = consoleGameObject.GetComponent<Console>();
                if (!console)
                {
                    console = consoleGameObject.AddComponent<Console>();
                    console.UpdateText();
                }

                instance = console;
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
        set => Instance.textInput = value;
    }

    /// <summary>
    /// Is the console window currently open?
    /// </summary>
    public static bool IsOpen
    {
        get => Instance.open;
        set => Instance.open = value;
    }

    [Obsolete("This is obsolete, use Console.IsOpen instead.")]
    public static bool Open
    {
        get => IsOpen;
        set => IsOpen = value;
    }

    /// <summary>
    /// The key that should be used to open the console window.
    /// </summary>
    public static KeyCode Key
    {
        get => Instance.key;
        set => Instance.key = value;
    }

    /// <summary>
    /// The amount of lines that a single scroll should perform.
    /// </summary>
    public static int Scroll
    {
        get => Instance.scroll;
        set
        {
            if (value < 0)
            {
                value = 0;
            }

            if (value >= HistorySize)
            {
                value = HistorySize - 1;
            }

            Instance.scroll = value;
        }
    }

    private string textInput;
    private float deltaTime;
    private bool open;
    private KeyCode key = KeyCode.BackQuote;
    private int scroll;
    private int index;
    private int lastMaxLines;
    private Type keyboardType;
    private bool hasInputSystem = true;
    private int framePressedOn;
    private List<string> text = new List<string>();
    private List<string> history = new List<string>();
    private List<SearchResult> searchResults = new List<SearchResult>();
    private string linesString;
    private bool typedSomething;
    private GUIStyle consoleStyle;
    private GUIStyle fpsCounterStyle;

    private void Awake()
    {
        instance = this;
        Parser.Initialize();
    }

    private void OnEnable()
    {
        instance = this;
        Parser.Initialize();

        //adds a listener for debug prints in editor
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private Font GetFont(string fontName)
    {
        Font[] fonts = Resources.LoadAll<Font>("");
        for (int i = 0; i < fonts.Length; i++)
        {
            if (fonts[i].name == fontName)
            {
                return fonts[i];
            }
        }

        return null;
    }

    //creates a style to be used in the gui calls
    private void CreateStyle()
    {
        Font font = GetFont("Hack-Regular");

        Texture2D pixel = Pixel;
        consoleStyle = new GUIStyle
        {
            richText = true,
            alignment = TextAnchor.UpperLeft,
            font = font,
            fontSize = 12
        };

        consoleStyle.normal.background = pixel;
        consoleStyle.normal.textColor = Color.white;

        consoleStyle.hover.background = pixel;
        consoleStyle.hover.textColor = Color.white;

        consoleStyle.active.background = pixel;
        consoleStyle.active.textColor = Color.white;

        //fps counter style
        fpsCounterStyle = new GUIStyle(consoleStyle)
        {
            alignment = TextAnchor.UpperRight
        };
    }

    private void HandleLog(string message, string stack, LogType logType)
    {
        if (logType == LogType.Warning)
        {
            //dont print this, its spam
            return;
        }
        else if (logType == LogType.Log)
        {
            WriteLine(message, logType);
        }
        else
        {
            //if any kind of error, print the stack as well
            WriteLine(message + "\n" + stack, logType);
        }
    }

    public static void Initialize()
    {
        //dumb
        IsOpen = false;
        Run("info");
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
    /// Prints a log message.
    /// </summary>
    public static void Print(object message)
    {
        string txt = GetStringFromObject(message);
        if (txt == null)
        {
            return;
        }

        Instance.Add(txt, PrintColor);
    }

    /// <summary>
    /// Prints a warning message.
    /// </summary>
    public static void Warn(object message)
    {
        string txt = GetStringFromObject(message);
        if (txt == null)
        {
            return;
        }

        Instance.Add(txt, WarningColor);
    }

    /// <summary>
    /// Prints an error message.
    /// </summary>
    public static void Error(object message)
    {
        string txt = GetStringFromObject(message);
        if (txt == null)
        {
            return;
        }

        Instance.Add(txt, ErrorColor);
    }

    /// <summary>
    /// Clears the console.
    /// </summary>
    public static void Clear()
    {
        Scroll = 0;
        TextInput = "";
        Instance.text.Clear();
        Instance.history.Clear();
        Instance.UpdateText();
    }

    /// <summary>
    /// Submit generic text to the console.
    /// </summary>
    public static void WriteLine(object text, LogType type = LogType.Log)
    {
        string stringText = GetStringFromObject(text);
        if (stringText == null)
        {
            return;
        }

        if (type == LogType.Log)
        {
            Print(stringText);
        }
        else if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
        {
            Error(stringText);
        }
        else if (type == LogType.Warning)
        {
            Warn(stringText);
        }

        //invoke the event
        onPrint?.Invoke(stringText, type);
    }

    /// <summary>
    /// Runs a single command.
    /// </summary>
    public static async void Run(string command)
    {
        //run
        object result = await Parser.Run(command);
        if (result == null)
        {
            return;
        }

        if (result is Exception exception)
        {
            Exception inner = exception.InnerException;
            if (inner != null)
            {
                Error(exception.Message + "\n" + exception.Source + "\n" + inner.Message + "\n" + inner.StackTrace);
            }
            else
            {
                Error(exception.Message + "\n" + exception.StackTrace);
            }
        }
        else
        {
            Print(result.ToString());
        }
    }

    /// <summary>
    /// Runs a list of commands.
    /// </summary>
    public static void Run(List<string> commands)
    {
        if (commands == null)
        {
            return;
        }

        for (int i = 0; i < commands.Count; i++)
        {
            Run(commands[i]);
        }
    }

    //adds text to console text
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

        for (int i = 0; i < lines.Count; i++)
        {
            text.Add("<color=" + color + ">" + lines[i] + "</color>");
            if (text.Count > HistorySize)
            {
                text.RemoveAt(0);
            }
        }

        int newScroll = text.Count - MaxLines;
        if (newScroll > Scroll)
        {
            //set scroll to bottom
            Scroll = newScroll;
        }

        //update the lines string
        UpdateText();
    }

    //creates a single text to use when display the console
    private void UpdateText()
    {
        string[] lines = new string[MaxLines];
        int lineIndex = 0;
        for (int i = 0; i < text.Count; i++)
        {
            int index = i + Scroll;
            if (index < 0)
            {
                continue;
            }
            else if (index >= text.Count)
            {
                continue;
            }
            else if (string.IsNullOrEmpty(text[index]))
            {
                break;
            }

            lines[lineIndex] = (text[index]);

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
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        //max lines amount changed
        if (lastMaxLines != MaxLines)
        {
            lastMaxLines = MaxLines;
            UpdateText();
        }
    }

    private void ShowFPS()
    {
        if (CommandsBuiltin.ShowFPS)
        {
            //style doesnt exist, ensure one exists
            if (fpsCounterStyle == null)
            {
                CreateStyle();
            }

            float msec = deltaTime * 1000f;
            float fps = 1f / deltaTime;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            Rect rect = new Rect(Screen.width - 100, 0, 100, 0);

            int oldDepth = GUI.depth;
            GUI.depth = -4000;
            GUI.Label(rect, text, fpsCounterStyle);
            GUI.depth = oldDepth;
        }
    }

    private bool IsConsoleKey(KeyCode keyCode)
    {
        if (keyCode == key)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool IsConsoleChar(char character)
    {
        if (character == '`')
        {
            return true;
        }
        else if (character == '~')
        {
            return true;
        }
        else if (character == '§')
        {
            return true;
        }
        else
        {
            return false;
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
                        textBuilder.Append("@id ");
                    }

                    textBuilder.Append(string.Join("/", command.Names));
                    if (command.Member is MethodInfo)
                    {
                        foreach (string parameter in command.Parameters)
                        {
                            textBuilder.Append(" <" + parameter + ">");
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
                        textBuilder.Append(" = " + command.Description);
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

    /// <summary>
    /// Returns true if enter key was pressed.
    /// </summary>
    private bool CheckForEnter()
    {
        //try with input system if it exists
        if (hasInputSystem)
        {
            if (keyboardType == null)
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
                            break;
                        }
                    }

                    if (keyboardType != null)
                    {
                        break;
                    }
                }

                //still null, so no input system
                if (keyboardType == null)
                {
                    hasInputSystem = false;
                    return false;
                }
            }

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

    private void OnGUI()
    {
        //just in case this is null, regen
        if (consoleStyle == null)
        {
            CreateStyle();
        }

        //show fps
        ShowFPS();
        bool moveToEnd = false;

        if (Event.current.type == EventType.KeyDown)
        {
            if (IsConsoleKey(Event.current.keyCode))
            {
                TextInput = "";
                IsOpen = !IsOpen;
                if (IsOpen)
                {
                    typedSomething = false;
                    index = history.Count;
                    Search(TextInput);
                }

                CreateStyle();
                Event.current.Use();
            }

            if (IsConsoleChar(Event.current.character))
            {
                return;
            }
        }

        //dont show the console if it shouldnt be open
        //duh
        if (!IsOpen)
        {
            return;
        }

        //view scrolling
        if (Event.current.type == EventType.ScrollWheel)
        {
            int scrollDirection = (int)Mathf.Sign(Event.current.delta.y) * 3;
            Scroll += scrollDirection;
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
                        TextInput = "";
                        moveToEnd = true;
                    }
                    else
                    {
                        if (index >= 0 && index < history.Count)
                        {
                            TextInput = history[index];
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
                        TextInput = "";
                        moveToEnd = true;
                    }
                    else
                    {
                        if (index >= 0 && index < searchResults.Count)
                        {
                            TextInput = searchResults[index].command;
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
                        TextInput = "";
                        moveToEnd = true;
                    }
                    else
                    {
                        if (index >= 0 && index < history.Count)
                        {
                            TextInput = history[index];
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
                        TextInput = "";
                        moveToEnd = true;
                    }
                    else
                    {
                        if (index >= 0 && index < searchResults.Count)
                        {
                            TextInput = searchResults[index].command;
                            moveToEnd = true;
                        }
                    }
                }
            }
        }

        //draw elements
        Color oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.depth = -5;
        int lineHeight = consoleStyle.fontSize + 4;

        GUILayout.Box(linesString, consoleStyle, GUILayout.Width(Screen.width));
        Rect lastControl = GUILayoutUtility.GetLastRect();

        //draw the typing field
        GUI.Box(new Rect(0, lastControl.y + lastControl.height, Screen.width, 2), "", consoleStyle);

        GUI.SetNextControlName(ConsoleControlName);
        string text = GUI.TextField(new Rect(0, lastControl.y + lastControl.height + 1, Screen.width, lineHeight), TextInput, consoleStyle);
        GUI.FocusControl(ConsoleControlName);

        if (moveToEnd)
        {
            MoveToEnd();
        }

        //text changed, search
        if (TextInput != text)
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

            TextInput = text;
            Search(text);
        }

        //display the search suggestions
        GUI.color = new Color(1f, 1f, 1f, 0.4f);
        StringBuilder suggestionsText = new StringBuilder();
        for (int i = 0; i < searchResults.Count; i++)
        {
            if (i == searchResults.Count - 1)
            {
                suggestionsText.Append(searchResults[i].text);
            }
            else
            {
                suggestionsText.AppendLine(searchResults[i].text);
            }
        }

        GUI.Box(new Rect(0, lastControl.y + lastControl.height + 1 + lineHeight, Screen.width, searchResults.Count * lineHeight), suggestionsText.ToString(), consoleStyle);
        GUI.color = oldColor;

        //pressing enter to run command
        if (CheckForEnter())
        {
            Add(TextInput, UserColor);

            history.Add(TextInput);
            index = history.Count;

            Search(null);
            Run(TextInput);
            Event.current.Use();

            TextInput = "";
            typedSomething = false;
        }
    }

    [Serializable]
    public class SearchResult
    {
        public string text;
        public string command;

        public SearchResult(string text, string command)
        {
            this.text = text;
            this.command = command;
        }
    }
}
