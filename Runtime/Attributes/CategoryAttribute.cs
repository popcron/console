using System;

namespace Popcron.Console
{
    [Serializable]
    public class CategoryAttribute : Attribute
    {
        public string name = "";

        public CategoryAttribute(string name)
        {
            this.name = name;
        }
    }
}
