using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;

using Popcron.Console;

[Category("Built in commands")]
public class CommandsBuiltin
{
    [Command("info", "Prints system information.")]
    public static void PrintSystemInfo()
    {
        string deviceName = SystemInfo.deviceName;
        string deviceModel = SystemInfo.deviceModel;
        DeviceType deviceType = SystemInfo.deviceType;
        string id = SystemInfo.deviceUniqueIdentifier;

        string os = SystemInfo.operatingSystem;
        OperatingSystemFamily osFamily = SystemInfo.operatingSystemFamily;
        string ram = (SystemInfo.systemMemorySize / 1000f) + " gb";

        string cpuName = SystemInfo.processorType;
        int cpuCount = SystemInfo.processorCount;
        int cpuFrequency = SystemInfo.processorFrequency;

        GraphicsDeviceType gpuType = SystemInfo.graphicsDeviceType;
        string gpuName = SystemInfo.graphicsDeviceName;
        string gpuVendor = SystemInfo.graphicsDeviceVendor;
        string gpuRam = (SystemInfo.graphicsMemorySize / 1000f) + " gb";

        Console.Print("<b>OS</b>: " + os + "(" + osFamily + ")");
        Console.Print("<b>RAM</b>: " + ram);

        Console.Print("<b>Device</b>");
        Console.Print("\t<b>Name</b>: " + deviceName);
        Console.Print("\t<b>Model</b>: " + deviceModel);
        Console.Print("\t<b>Type</b>: " + deviceType);
        Console.Print("\t<b>Unique ID</b>: " + id);

        Console.Print("<b>CPU</b>");
        Console.Print("\t<b>Name</b>: " + cpuName);
        Console.Print("\t<b>Processors</b>: " + cpuCount);
        Console.Print("\t<b>Frequency</b>: " + SystemInfo.processorFrequency);

        Console.Print("<b>GPU</b>");
        Console.Print("\t<b>Name</b>: " + gpuName);
        Console.Print("\t<b>Vendor</b>: " + gpuVendor);
        Console.Print("\t<b>Memory</b>: " + gpuRam);
    }

    [Command("show fps")]
    public static bool ShowFPS
    {
        get
        {
            return PlayerPrefs.GetInt(Console.ID + "_Console_ShowFPS", 0) == 1;
        }
        set
        {
            PlayerPrefs.SetInt(Console.ID + "_Console_ShowFPS", value ? 1 : 0);
        }
    }

    [Command("clear", "Clears the console window.")]
    public static void ClearConsole()
    {
        Console.Clear();
        Debug.ClearDeveloperConsole();
    }

    [Command("owners", "Lists all instance command owners.")]
    public static void Owners()
    {
        foreach (Owner owner in Parser.Owners)
        {
            Console.Print("\t" + owner.id + " = " + owner.owner);
            foreach (var method in owner.methods)
            {
                string parameters = "";
                Console.Print("\t\t" + method.name + parameters);
            }
            foreach (var property in owner.properties)
            {
                string extra = "";
                if (property.canSetProperty) extra += " [value]";
                Console.Print("\t\t" + property.name + extra);
            }
            foreach (var field in owner.fields)
            {
                Console.Print("\t\t" + field.name + " = " + field.Value);
            }
        }
    }

    [Command("converters", "Lists all type converters.")]
    public static void Converters()
    {
        foreach (Converter converter in Converter.Converters)
        {
            Console.Print("\t" + converter.GetType() + " (" + converter.Type + ")");
        }
    }

    [Command("help", "Outputs a list of all commands")]
    public static void Help()
    {
        Console.Print("All commands registered: ");
        foreach (var category in Library.Categories)
        {
            Console.Print("\t" + category.Name);
            foreach (var command in category.Commands)
            {
                string text = string.Join("/", command.Names);
                if (command.Member is MethodInfo method)
                {
                    foreach (var parameter in command.Parameters)
                    {
                        text += " <" + parameter + ">";
                    }
                }
                else if (command.Member is PropertyInfo property)
                {
                    MethodInfo set = property.GetSetMethod();
                    if (set != null)
                    {
                        text += " [value]";
                    }
                }
                else if (command.Member is FieldInfo field)
                {
                    text += " [value]";
                }

                if (command.Description != "")
                {
                    text += " = " + command.Description;
                }

                if (!command.IsStatic)
                {
                    text = "@id " + text;
                }

                Console.Print("\t\t" + text);
            }
        }
    }

    [Command("echo")]
    public static void Echo(string text)
    {
        Console.Print(text);
    }
}
