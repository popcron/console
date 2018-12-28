using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using System.Text.RegularExpressions;
using Popcron.Console;

public class Console : MonoBehaviour
{
    private const int HistorySize = 200;
    private const int MaxLines = 30;
    private const int LineHeight = 16;
    private const string ConsoleControlName = "ControlField";
    private const string PrintColor = "white";
    private const string WarningColor = "orange";
    private const string ErrorColor = "red";
    private const string UserColor = "gray";

    private static Console instance;

    private static Texture2D Pixel
    {
        get
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.5f));
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
            return PlayerPrefs.GetString("consoleText") + "";
        }
        set
        {
            PlayerPrefs.SetString("consoleText", value);
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

    private bool open;
    private int scroll;
    private int historyIndex;
    private List<string> text = new List<string>();
    private List<string> history = new List<string>();
    private GUIStyle style;

    private void Awake()
    {
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

    //creates a style to be used in the gui calls
    private void CreateStyle()
    {
        Texture2D pixel = Pixel;
        style = new GUIStyle();
        style.richText = true;

        //box
        style.alignment = TextAnchor.UpperLeft;
        style.normal.background = pixel;
        style.normal.textColor = Color.white;

        style.hover.background = pixel;
        style.hover.textColor = Color.white;

        style.active.background = pixel;
        style.active.textColor = Color.white;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string message, string stack, LogType logType)
    {
        WriteLine(message, logType);
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
    }

    private static void WriteLine(object text, LogType type = LogType.Log)
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

        if (result is Exception)
        {
            Exception exception = result as Exception;
            Error(exception.Message + "\n" + exception.InnerException.Message + "\n" + exception.InnerException.StackTrace);
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
            lines.Add("<color=" + color + ">" + str + "</color>");
        }

        foreach (var line in lines)
        {
            text.Add("<color=" + color + ">" + line + "</color>");
            if (text.Count > HistorySize)
            {
                text.RemoveAt(0);
            }
        }

        Scroll = text.Count - MaxLines;
    }

    private void OnGUI()
    {
        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.BackQuote)
            {
                Text = "";
                Open = !Open;
                Event.current.Use();
            }

            if (Event.current.character == '`') return;
        }

        //dont show the console if it shouldnt be open
        //duh
        if (!Open) return;

        //view scrolling
        if (Event.current.type == EventType.ScrollWheel)
        {
            Scroll += (int)Event.current.delta.y;
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

        //draw lines
        string[] lines = new string[MaxLines];
        int lineIndex = 0;
        for (int i = 0; i < text.Count; i++)
        {
            int index = i + Scroll;
            if (index < 0) continue;
            if (index >= text.Count) continue;
            if (string.IsNullOrEmpty(text[index])) break;

            lines[lineIndex] = (text[index]);

            lineIndex++;
            if (lineIndex == MaxLines) break;
        }

        //draw elements
        GUI.depth = -5;
        GUILayout.Box(string.Join("\n", lines), style, GUILayout.Width(Screen.width));
        Rect lastControl = GUILayoutUtility.GetLastRect();

        //draw the typing field
        GUI.depth = -5;
        GUI.Box(new Rect(0, lastControl.y + lastControl.height, Screen.width, 2), "", style);

        GUI.depth = -5;
        GUI.SetNextControlName(ConsoleControlName);
        Text = GUI.TextField(new Rect(0, lastControl.y + lastControl.height + 1, Screen.width, 16), Text, style);
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
