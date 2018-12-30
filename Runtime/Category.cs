using System;
using System.Collections.Generic;

namespace Popcron.Console
{
    [Serializable]
    public class Category
    {
        private string name = "";
        private List<Command> commands = new List<Command>();

        public string Name
        {
            get
            {
                return name;
            }
        }

        public List<Command> Commands
        {
            get
            {
                return commands;
            }
        }

        private Category(string name)
        {
            this.name = name;
        }

        public static Category CreateUncategorized()
        {
            Category category = new Category("Uncategorized");
            return category;
        }

        public static Category Create(Type type)
        {
            CategoryAttribute attribute = type.GetCategoryAttribute();
            if (attribute == null) return null;

            Category category = new Category(attribute.name);
            List<Command> commands = Library.Commands;
            foreach (Command command in commands)
            {
                if (command.Owner == type)
                {
                    category.commands.Add(command);
                }
            }

            return category;
        }
    }
}