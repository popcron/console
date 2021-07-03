using System;
using static Popcron.Console.Parser;

namespace Popcron.Console
{
    public class CategoryAttribute : Attribute
    {
        public string Name { get; }

        public CategoryAttribute(string name)
        {
            Name = Sanitize(name);
        }
    }
}
