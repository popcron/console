using System;

namespace Popcron.Console
{
    [Serializable]
    public class CommandAttribute : Attribute
    {
        public string name = "";
        public string description = "";

        public CommandAttribute(string name, string description = "")
        {
            this.name = name;
            this.description = description;
        }
    }
}