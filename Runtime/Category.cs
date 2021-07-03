using System;
using System.Collections.Generic;

namespace Popcron.Console
{
    [Serializable]
    public class Category
    {
        private string name = "";
        private List<Command> commands = new List<Command>();

        public string Name => name;
        public List<Command> Commands => commands;

        private Category(string name)
        {
            this.name = name;
        }

        public static Category Create(string name)
        {
            return new Category(name);
        }

        public static Category Create(Type type)
        {
            CategoryAttribute attribute = type.GetCategoryAttribute();
            if (attribute == null)
            {
                return null;
            }

            Category category = new Category(attribute.Name);
            List<Command> commands = Library.Commands;
            int commandsCount = commands.Count;
            for (int i = 0; i < commandsCount; i++)
            {
                Command command = commands[i];
                if (command.OwnerClass == type)
                {
                    category.commands.Add(command);
                }
            }

            return category;
        }
    }
}