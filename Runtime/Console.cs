using System;
using System.Collections.Generic;
using UnityEngine;

using Popcron.Console;

[AddComponentMenu("")]
public class Console : MonoBehaviour
{
    private const int HistorySize = 200;

    private const int MaxLines = 30;
    private const int LineHeight = 16;
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

    private static string Text
    {
        get
        {
            return PlayerPrefs.GetString(ID + "_Console_Text") + "";
        }
        set
        {
            PlayerPrefs.SetString(ID + "_Console_Text", value);
        }
    }

    public static int ID
    {
        get
        {
            return Instance.currentlyUniqueId;
        }
    }

    public static KeyCode Key
    {
        get
        {
            int defaultValue = (int)KeyCode.BackQuote;
            int savedValue = PlayerPrefs.GetInt(ID + "_Console_Key", defaultValue);
            return (KeyCode)defaultValue;
        }
        set
        {
            int newValue = (int)value;
            PlayerPrefs.SetInt(ID + "_Console_Key", newValue);
        }
    }

    public static bool Open
    {
        get
        {
            return Instance.open;
        }
        set
        {
            Instance.open = value;
        }
    }

    private static int Scroll
    {
        get
        {
            return Instance.scroll;
        }
        set
        {
            if (value < 0) value = 0;
            if (value >= HistorySize) value = HistorySize - 1;
            Instance.scroll = value;
        }
    }

    private float deltaTime;
    private int currentlyUniqueId;
    private bool open;
    private int scroll;
    private int historyIndex;
    private List<string> text = new List<string>();
    private List<string> history = new List<string>();
    private string linesString;
    private GUIStyle consoleStyle;
    private GUIStyle fpsCounterStyle;

    private void Awake()
    {
        currentlyUniqueId = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        instance = this;
        Parser.Initialize();
    }

    private void OnEnable()
    {
        instance = this;
        Parser.Initialize();

        CreateStyle();

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
        Font font = GetFont("MonospaceTypewriter");

        Texture2D pixel = Pixel;
        consoleStyle = new GUIStyle
        {
            richText = true,
            alignment = TextAnchor.UpperLeft,
            font = font
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
        WriteLine(message, logType);
    }

    public static void Initialize()
    {
        //dumb
        Open = false;
        Run("info");
    }

    /// <summary>
    /// Prints a log message
    /// </summary>
    /// <param name="message"></param>
    public static void Print(object message)
    {
        if (message == null) return;
        Instance.Add(message.ToString(), PrintColor);
    }

    /// <summary>
    /// Prints a warning message
    /// </summary>
    /// <param name="message"></param>
    public static void Warn(object message)
    {
        if (message == null) return;
        Instance.Add(message.ToString(), WarningColor);
    }

    /// <summary>
    /// Prints an error message
    /// </summary>
    /// <param name="message"></param>
    public static void Error(object message)
    {
        if (message == null) return;
        Instance.Add(message.ToString(), ErrorColor);
    }

    /// <summary>
    /// Clears the console
    /// </summary>
    public static void Clear()
    {
        Scroll = 0;
        Text = "";
        Instance.text.Clear();
        Instance.history.Clear();
        Instance.UpdateText();
    }

    /// <summary>
    /// Submit generic text to the console
    /// </summary>
    /// <param name="text"></param>
    /// <param name="type"></param>
    public static void WriteLine(object text, LogType type = LogType.Log)
    {
        if (type == LogType.Log)
        {
            Print(text);
        }
        else if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
        {
            Error(text);
        }
        else if (type == LogType.Warning)
        {
            Warn(text);
        }
    }

    /// <summary>
    /// Runs a single command
    /// </summary>
    /// <param name="command"></param>
    /// <param name="gameBehaviour"></param>
    public static async void Run(string command)
    {
        //run
        object result = await Parser.Run(command);
        if (result == null) return;

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
    /// Runs a list of commands
    /// </summary>
    /// <param name="commands"></param>
    /// <param name="gameBehaviour"></param>
    public static void Run(List<string> commands)
    {
        if (commands == null) return;

        foreach (var command in commands)
        {
            Run(command);
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

        foreach (var line in lines)
        {
            text.Add("<color=" + color + ">" + line + "</color>");
            if (text.Count > HistorySize)
            {
                text.RemoveAt(0);
            }
        }

        int newScroll = text.Count - MaxLines;
        if (newScroll > Scroll + 3)
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
            if (index < 0) continue;
            if (index >= text.Count) continue;
            if (string.IsNullOrEmpty(text[index])) break;

            lines[lineIndex] = (text[index]);

            //replace all \t with 4 spaces
            lines[lineIndex] = lines[lineIndex].Replace("\t", "    ");

            lineIndex++;
            if (lineIndex == MaxLines) break;
        }

        linesString = string.Join("\n", lines);
    }

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    private void ShowFPS()
    {
        if (CommandsBuiltin.ShowFPS)
        {
            float msec = deltaTime * 1000f;
            float fps = 1f / deltaTime;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            Rect rect = new Rect(Screen.width - 100, 0, 100, 0);
            GUI.Label(rect, text, fpsCounterStyle);
        }
    }

    private void OnGUI()
    {
        //show fps
        ShowFPS();

        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == Key)
            {
                Text = "";
                Open = !Open;
                Event.current.Use();
            }

            char character = Key.GetCharFromKeyCode();
            if (Event.current.character == character) return;
        }

        //dont show the console if it shouldnt be open
        //duh
        if (!Open) return;

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
                historyIndex--;
                if (historyIndex < -1)
                {
                    historyIndex = -1;
                    Text = "";
                }
                else
                {
                    if (historyIndex >= 0 && historyIndex < history.Count)
                    {
                        Text = history[historyIndex];
                    }
                }
            }
            if (Event.current.keyCode == KeyCode.DownArrow)
            {
                historyIndex++;
                if (historyIndex > history.Count)
                {
                    historyIndex = history.Count;
                    Text = "";
                }
                else
                {
                    if (historyIndex >= 0 && historyIndex < history.Count)
                    {
                        Text = history[historyIndex];
                    }
                }
            }
        }

        //draw elements
        GUI.depth = -5;
        GUILayout.Box(linesString, consoleStyle, GUILayout.Width(Screen.width));
        Rect lastControl = GUILayoutUtility.GetLastRect();

        //draw the typing field
        GUI.depth = -5;
        GUI.Box(new Rect(0, lastControl.y + lastControl.height, Screen.width, 2), "", consoleStyle);

        GUI.depth = -5;
        GUI.SetNextControlName(ConsoleControlName);
        Text = GUI.TextField(new Rect(0, lastControl.y + lastControl.height + 1, Screen.width, 16), Text, consoleStyle);
        GUI.FocusControl(ConsoleControlName);

        //pressing enter to run command
        if (Event.current.type == EventType.KeyDown)
        {
            bool enter = Event.current.character == '\n' || Event.current.character == '\r';
            if (enter)
            {
                Add(Text, UserColor);

                history.Add(Text);
                historyIndex = history.Count;

                Run(Text);
                Event.current.Use();

                Text = "";
                return;
            }
        }
    }
}
