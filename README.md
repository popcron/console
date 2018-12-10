# Console
A command line parser for use in developer consoles for games

## Requirements
- .NET Framework 4.5

## Unity
To install for use unity, download the contents of the unity branch.
A Console class is provided as a small wrapper for the parser. Use the `~` key to open the console.

If using 2018.3.x, you can add a new entry to the manifest.json file in your Packages folder:
```json
"com.popcron.console": "https://github.com/popcron/console.git#unity"
```

## Calling commands
```cs
string command = "help";
object result = await Parser.Run(command);
```

The `Run` command returns an awaited object, this allows commands to be awaitable and use delaying.

## Instance commands
Command methods that are declared as static can be executed from any context. Instance methods however cannot be called in the same way. These commands require an object that owns the method. The parser class provides methods that allow you to register and unregister objects with a unique ID, which can allow instance commands to be called on an object. This is useful in the case of games, where the instance commands for adding ammo are now marked as commands with the commnd attribute.

```cs
using Popcron.Console;

public class PlayerAmmo : MonoBehaviour
{
    [SerializeField]
    private int ammo = 100;
    
    //when the object gets created, register it
    private void OnEnable()
    {
        Parser.Register("player", this);
    }
    
    //when the object gets destroyed, unregister it
    private void OnDisable()
    {
        Parser.Unregister("player");
    }
    
    [Command("add ammo")]
    public void AddAmmo(int amount)
    {
        ammo += amount;
    }
    
    [Command("get ammo")]
    public int GetAmmo()
    {
        return ammo;
    }
}
```

To call this command from the console, you call the method exactly as its writen, with the addition of the `@id` prefix.
`@player add ammo 100`

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
