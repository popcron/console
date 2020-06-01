using System;
using static Popcron.Console.Parser;

namespace Popcron.Console
{
    [Serializable]
    public class AliasAttribute : Attribute
    {
        public string name = "";

        public AliasAttribute(string name)
        {
            this.name = Sanitize(name);
        }
    }
}
