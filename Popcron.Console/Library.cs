using System;
using System.Collections.Generic;
using System.Reflection;

namespace Popcron.Console
{
    [Serializable]
    public sealed class Library
    {
        private static List<Command> commands = null;
        private static List<Category> categories = null;

        public static List<Command> Commands
        {
            get
            {
                if (commands == null)
                {
                    FindCommands();
                }

                return commands;
            }
        }

        public static List<Category> Categories
        {
            get
            {
                if (categories == null)
                {
                    FindCategories();
                }

                return categories;
            }
        }

        public static void FindCategories()
        {
            if (categories == null)
            {
                categories = new List<Category>();
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types)
                    {
                        Category category = Category.Create(type);
                        if (category != null)
                        {
                            categories.Add(category);
                        }
                    }
                }
            }
        }

        public static void FindCommands()
        {
            if (commands == null)
            {
                commands = new List<Command>();
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types)
                    {
                        MethodInfo[] methods = type.GetMethods();
                        foreach (MethodInfo method in methods)
                        {
                            Command command = Command.Create(method, type);
                            if (command != null)
                            {
                                commands.Add(command);
                            }
                        }
                    }
                }
            }
        }
    }
}
