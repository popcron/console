![Woah console woah!!!](https://cdn.discordapp.com/attachments/377316629220032523/529091513104465920/unknown.png)

# Console
A command line parser for use in Unity.

## Requirements
- .NET Framework 4.5

## Installing
To install for use in Unity, copy everything from this repository to `<YourNewUnityProject>/Packages/Popcron.Console` folder.

If using 2018.3.x, you can add a new entry to the manifest.json file in your Packages folder:
```json
"com.popcron.console": "https://github.com/popcron/console.git"
```

## Calling commands
```cs
string command = "help";
object result = await Parser.Run(command);
```

or just `help` from the console window.

## Instance commands
Command methods that are declared as static can be executed from any context. Instance methods however cannot be called in the same way. These commands require an object that owns the method. The parser class provides methods that allow you to register and unregister objects with a unique ID, which can allow instance commands to be called on an object.

```cs
using UnityEngine;
using Popcron.Console;

public class PlayerAmmo : MonoBehaviour
{
    [Command("ammo")]
    public int ammo = 100;
    
    //when the object gets created, register it
    private void OnEnable()
    {
        Parser.Register(this, "player");
    }
    
    //when the object gets destroyed, unregister it
    private void OnDisable()
    {
        Parser.Unregister(this);
    }
}
```

To call this command from the console, you call the method exactly as its writen, with the addition of the `@id` prefix.
`@player ammo 100`

### Troubleshooting
To debug the instance methods, the parser contains a list of all registered owners in the `Parser.Owners` list.

## Examples
To create a simple command, add a `Command` attribute to your method.

```cs
using Popcron.Console;

public class Commands
{
    [Command("add")]
    public static int Add(int a, int b)
    {
        return a + b;
    }
}
```
`add 2 3` will return `5`

<details>
    <summary>Setting up categories</summary>
    
Categories arent necessary, but they allow you to categorize commands into a list which can be retrieved using `Parser.Categories`. To add categories, add a `Category` attribute to the class itself. This is primarely useful when listing all of the commands using `help`.
```cs
using Popcron.Console;

[Category("Default commands")]
public class Commands
{
    [Command("add")]
    public static int Add(int a, int b)
    {
        return a + b;
    }
}
```
</details>

<details>
    <summary>Command aliases</summary>
    
Commands can have multiple aliases. To give a command another calling name, add the `Alias` attribute
```cs
using Popcron.Console;

[Category("Default commands")]
public class Commands
{
    [Alias("+")]
    [Command("add")]
    public static int Add(int a, int b)
    {
        return a + b;
    }
}
```
`+ 2 3` will return `5`

`add 7 -2` will return `5`
</details>

## Supported types
- string
- char
- bool
- byte and sbyte
- short and ushort
- int and uint
- long and ulong
- float
- double
- object
- Type
- DateTime

You can also add your own type converters by inheriting from the abstract `Converter` class.

## Built in commands
- `info` Prints the system information (OS, Device, CPU and GPU).
- `show fps` Boolean toggle for whether the fps counter should show up or not.
- `clear` Clears the console.
- `owners` Prints all registered owners, along with their instance methods, properties and fields.
- `converters` Lists all registered converters.
- `help` Prints all commands found as static methods, properties and fields.
- `echo` Dumb echo command.

## FAQ
- **How do I add this to my unity project?**
Add `"com.popcron.console": "https://github.com/popcron/console.git"` to your `manifest.json` file in the packages folder.
- **It doesn't show up.**
Press ~.
- **I pressed it, it still doesn't show up!**
Invoke the `Console.Initialize()` method, or any of the static methods/properties.
- **How do I change the key to open the console?**
Change the `Console.Key` property.
- **I want to use the original console that's included.**
If you want to reference the "original console", you can do so by referencing the namespace: `System.Console.WriteLine("wee")`.
- **Can I use properties as commands?**
Yes.
- **Can I use fields as commands?**
Yes.
- **Static?**
Yes, as long as you register and unregister the instance owner of the command.
- **How do I categorize a command?**
Add a `Category` attribute to the class type.
- **How do I make my own converter?**
Create a new class, and inherit from the `Converter` class.
