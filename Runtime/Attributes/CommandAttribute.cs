using System;
using static Popcron.Console.Parser;

namespace Popcron.Console
{
    [Serializable]
    public class CommandAttribute : Attribute
    {
        public string name = "";
        public string description = "";

        public CommandAttribute(string name, string description = "")
        {
            this.name = Sanitize(name);
            this.description = description;
        }
    }
}