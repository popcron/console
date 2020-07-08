using Popcron.Console;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[Category("Built in commands")]
public class CommandsBuiltin
{
    private const string Indent = "    ";

    [Command("info", "Prints system information.")]
    public static string PrintSystemInfo()
    {
        string deviceName = SystemInfo.deviceName;
        string deviceModel = SystemInfo.deviceModel;
        DeviceType deviceType = SystemInfo.deviceType;
        string id = SystemInfo.deviceUniqueIdentifier;

        string os = SystemInfo.operatingSystem;
        OperatingSystemFamily osFamily = SystemInfo.operatingSystemFamily;
        string ram = (SystemInfo.systemMemorySize / 1000f) + " Gb";

        string cpuName = SystemInfo.processorType;
        int cpuCount = SystemInfo.processorCount;
        int cpuFrequency = SystemInfo.processorFrequency;

        GraphicsDeviceType gpuType = SystemInfo.graphicsDeviceType;
        string gpuName = SystemInfo.graphicsDeviceName;
        string gpuVendor = SystemInfo.graphicsDeviceVendor;
        string gpuRam = (SystemInfo.graphicsMemorySize / 1000f) + " Gb";

        int padding = 23;
        StringBuilder text = new StringBuilder();
        text.AppendLine("<b>Device</b>");
        text.AppendLine(Indent + "<b>OS</b>".PadRight(padding) + os + "(" + osFamily + ")");
        text.AppendLine(Indent + "<b>RAM</b>".PadRight(padding) + ram);
        text.AppendLine(Indent + "<b>Name</b>".PadRight(padding) + deviceName);
        text.AppendLine(Indent + "<b>Model</b>".PadRight(padding) + deviceModel);
        text.AppendLine(Indent + "<b>Type</b>".PadRight(padding) + deviceType);
        text.AppendLine(Indent + "<b>Unique ID</b>".PadRight(padding) + id);

        text.AppendLine("<b>CPU</b>");
        text.AppendLine(Indent + "<b>Name</b>".PadRight(padding) + cpuName);
        text.AppendLine(Indent + "<b>Processors</b>".PadRight(padding) + cpuCount);
        text.AppendLine(Indent + "<b>Frequency</b>".PadRight(padding) + cpuFrequency);

        text.AppendLine("<b>GPU</b>");
        text.AppendLine(Indent + "<b>Name</b>".PadRight(padding) + gpuName);
        text.AppendLine(Indent + "<b>Type</b>".PadRight(padding) + gpuType);
        text.AppendLine(Indent + "<b>Vendor</b>".PadRight(padding) + gpuVendor);
        text.AppendLine(Indent + "<b>Memory</b>".PadRight(padding) + gpuRam);
        return text.ToString();
    }

    [Command("clear", "Clears the console window.")]
    public static void ClearConsole()
    {
        Console.Clear();
        Debug.ClearDeveloperConsole();
    }

    [Command("owners", "Lists all instance command owners.")]
    public static string Owners()
    {
        StringBuilder text = new StringBuilder();
        foreach (Owner owner in Parser.Owners)
        {
            Console.Print(Indent + owner.id + " = " + owner.owner);
            foreach (Owner.OwnerMember method in owner.methods)
            {
                text.AppendLine(Indent + Indent + method.name);
            }

            foreach (Owner.OwnerMember property in owner.properties)
            {
                if (property.canSetProperty)
                {
                    text.Append(Indent + Indent + property.name);
                    text.AppendLine(" [value]");
                }
                else
                {
                    text.AppendLine(Indent + Indent + property.name);
                }
            }

            foreach (Owner.OwnerMember field in owner.fields)
            {
                text.AppendLine(Indent + Indent + field.name);
                text.AppendLine(" = ");
                text.AppendLine(field.Value?.ToString());
            }
        }
        return text.ToString();
    }

    [Command("converters", "Lists all type converters.")]
    public static string Converters()
    {
        StringBuilder text = new StringBuilder();
        foreach (Converter converter in Converter.Converters)
        {
            text.AppendLine(Indent + converter.GetType() + " (" + converter.Type + ")");
        }

        return text.ToString();
    }

    [Command("help", "Outputs a list of all commands")]
    public static string Help()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("All commands registered: ");
        foreach (Category category in Library.Categories)
        {
            builder.AppendLine(Indent + category.Name);
            foreach (Command command in category.Commands)
            {
                string namesText = string.Join("/", command.Names);
                builder.Append(Indent + Indent);

                //not static, so an instance method with ID requirement
                if (!command.IsStatic)
                {
                    builder.Append(Parser.IDPrefix);
                    builder.Append(" ");
                }

                builder.Append(namesText);
                if (command.Member is MethodInfo method)
                {
                    foreach (string parameter in command.Parameters)
                    {
                        builder.Append(" ");
                        builder.Append(Parser.LeftAngleBracket);
                        builder.Append(parameter);
                        builder.Append(Parser.RightAngleBracket);
                    }
                }
                else if (command.Member is PropertyInfo property)
                {
                    MethodInfo set = property.GetSetMethod();
                    if (set != null)
                    {
                        builder.Append(" [");
                        builder.Append(property.PropertyType.Name);
                        builder.Append("]");
                    }
                }
                else if (command.Member is FieldInfo field)
                {
                    builder.Append(" [");
                    builder.Append(field.FieldType.Name);
                    builder.Append("]");
                }

                //add description if its present
                if (!string.IsNullOrEmpty(command.Description))
                {
                    builder.Append(" = ");
                    builder.Append(command.Description);
                }

                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    [Command("echo")]
    public static string Echo(string text)
    {
        return text;
    }

    [Command("scene", "Prints out the entire scene hierarchy.")]
    public static void Scene()
    {
        string indent = Indent;
        GameObject[] root = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < root.Length; i++)
        {
            PrintObjectAndKids(indent, root[i].transform);
        }
    }

    private static void PrintObjectAndKids(string indent, Transform transform)
    {
        if (transform.gameObject.activeInHierarchy && transform.gameObject.activeSelf)
        {
            Console.WriteLine(indent + transform.name);
        }
        else
        {
            Console.WriteLine(indent + "<color=gray>" + transform.name + "</color>");
        }

        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform kid = transform.GetChild(i);
            PrintObjectAndKids(indent + Indent, kid);
        }
    }
}
