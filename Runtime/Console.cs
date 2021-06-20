#pragma warning disable CS0162 //unreachable code detected

using Popcron.Console;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UnityEngine;
using C = Popcron.Console.ConsoleWindow;

public struct Console
{
    /// <summary>
    /// Returns true if the console is meant to be functional.
    /// </summary>
#if DISABLE_CONSOLE && !UNITY_EDITOR
    public const bool IsIncluded = false;
#else
    public const bool IsIncluded = true;
#endif

    /// <summary>
    /// Gets executed after a message is added to the console window.
    /// </summary>
    public static C.OnPrinted OnPrinted
    {
        get => C.onPrinted;
        set => C.onPrinted = value;
    }

    /// <summary>
    /// Gets executed after before a message is printed to the console.
    /// </summary>
    public static C.OnAboutToPrint OnAboutToPrint
    {
        get => C.onAboutToPrint;
        set => C.onAboutToPrint = value;
    }

    /// <summary>
    /// Read only list of commands that were submitted to the console.
    /// </summary>
    public static ReadOnlyCollection<string> History => C.History;

    /// <summary>
    /// Read only list of the text that was submitted to the console (without the rich text tags).
    /// </summary>
    public static ReadOnlyCollection<string> Text => C.Text;

    /// <summary>
    /// The text that is currently given to the console window?
    /// </summary>
    public static string TextInput
    {
        get => C.TextInput;
        set => C.TextInput = value;
    }

    /// <summary>
    /// Is the console window currently open?
    /// </summary>
    public static bool IsOpen
    {
        get => C.IsOpen;
        set => C.IsOpen = value;
    }

    [Obsolete("This is obsolete, use Console.IsOpen instead.")]
    public static bool Open
    {
        get => IsOpen;
        set => IsOpen = value;
    }

    /// <summary>
    /// The amount of lines that a single scroll should perform.
    /// </summary>
    public static int ScrollPosition
    {
        get => C.ScrollPosition;
        set => C.ScrollPosition = value;
    }

    /// <summary>
    /// Clears the console window.
    /// </summary>
    public static void Clear() => C.Clear();

    /// <summary>
    /// Runs a single command synchronously.
    /// </summary>
    public static object Run(string command) => AsyncHelpers.RunSync(() => RunAsyncTask(command));

    /// <summary>
    /// Runs a single command asynchronously.
    /// </summary>
    public static async void RunAsync(string command) => await RunAsyncTask(command);

    /// <summary>
    /// Runs a single command asynchronously.
    /// </summary>
    public static async Task<object> RunAsyncTask(string command)
    {
        if (!IsIncluded)
        {
            return null;
        }

        //run
        char newLine = '\n';
        object result = await Parser.Run(command);
        if (result != null)
        {
            if (result is Exception exception)
            {
                Exception inner = exception.InnerException;
                if (inner != null)
                {
                    WriteLine(exception.Message + newLine + exception.Source + newLine + inner.Message + newLine + inner.StackTrace, LogType.Exception);
                }
                else
                {
                    WriteLine(exception.Message + newLine + exception.StackTrace, LogType.Exception);
                }
            }
            else
            {
                WriteLine(result.ToString(), LogType.Log);
            }
        }

        return result;
    }

    /// <summary>
    /// Runs a list of commands synchronously.
    /// </summary>
    public static void Run(List<string> commands)
    {
        if (!IsIncluded)
        {
            return;
        }

        if (commands == null || commands.Count == 0)
        {
            return;
        }

        for (int i = 0; i < commands.Count; i++)
        {
            Run(commands[i]);
        }
    }

    /// <summary>
    /// Runs a list of commands asynchronously.
    /// </summary>
    public static async void RunAsync(List<string> commands)
    {
        if (!IsIncluded)
        {
            return;
        }

        if (commands == null || commands.Count == 0)
        {
            return;
        }

        for (int i = 0; i < commands.Count; i++)
        {
            await RunAsyncTask(commands[i]);
        }
    }

    /// <summary>
    /// Runs a list of commands asynchronously.
    /// </summary>
    public static async Task RunAsyncTask(List<string> commands)
    {
        if (!IsIncluded)
        {
            return;
        }

        if (commands == null || commands.Count == 0)
        {
            return;
        }

        for (int i = 0; i < commands.Count; i++)
        {
            await RunAsyncTask(commands[i]);
        }
    }

    /// <summary>
    /// Prints this object to the console as a log.
    /// </summary>
    public static void Print(object message) => C.WriteLine(message, LogType.Log);

    /// <summary>
    /// Prints this object to the console as a warning.
    /// </summary>
    public static void Warn(object message) => C.WriteLine(message, LogType.Warning);

    /// <summary>
    /// Prints this object to the console with a specific type.
    /// </summary>
    public static void WriteLine(object message, LogType type = LogType.Log) => C.WriteLine(message, type);

    /// <summary>
    /// Prints this object to the console as a log and a specific hex color.
    /// </summary>
    public static void WriteLine(object message, string hexColor) => C.WriteLine(message, hexColor);

    /// <summary>
    /// Prints an error to the console window.
    /// </summary>
    public static void Error(object message) => C.WriteLine(message, LogType.Error);

    /// <summary>
    /// Prints an exception to the console window.
    /// </summary>
    public static void Exception(object message) => C.WriteLine(message, LogType.Exception);
}
