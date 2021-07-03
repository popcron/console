using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Popcron.Console
{
    public sealed class Library
    {
        private static List<(Assembly assembly, Type[] types)> assemblies = null;
        private static List<Command> commands = null;
        private static List<Category> categories = null;

        /// <summary>
        /// List of all commands found.
        /// </summary>
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

        public static Category Uncategorized
        {
            get
            {
                if (!TryGetCategory("Uncategorized", out Category category))
                {
                    category = Category.Create("Uncategorized");
                    categories.Add(category);
                }

                return category;
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

        private static List<(Assembly assembly, Type[] types)> Assemblies
        {
            get
            {
                if (assemblies == null || assemblies.Count == 0)
                {
                    //possible in some cases
                    if (!Settings.Current)
                    {
                        assemblies = new List<(Assembly assembly, Type[] types)>();
                        return assemblies;
                    }

                    FindAllAssemblies();
                }

                return assemblies;
            }
        }

        private static void FindAllAssemblies()
        {
            List<string> allAssemblies = Settings.Current.assemblies.ToList();
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            Assembly consoleAssembly = typeof(ConsoleWindow).Assembly;

            //ensure the last 4 assemblies exist in the list
            if (executingAssembly != null && !allAssemblies.Contains(executingAssembly.FullName))
            {
                allAssemblies.Add(executingAssembly.FullName);
            }

            if (callingAssembly != null && !allAssemblies.Contains(callingAssembly.FullName))
            {
                allAssemblies.Add(callingAssembly.FullName);
            }

            if (entryAssembly != null && !allAssemblies.Contains(entryAssembly.FullName))
            {
                allAssemblies.Add(entryAssembly.FullName);
            }

            if (consoleAssembly != null && !allAssemblies.Contains(consoleAssembly.FullName))
            {
                allAssemblies.Add(consoleAssembly.FullName);
            }

            assemblies = new List<(Assembly assembly, Type[] types)>();
            for (int a = 0; a < allAssemblies.Count; a++)
            {
                AddAssembly(allAssemblies[a]);

            }

            if (!ContainsAssembly("Assembly-CSharp"))
            {
                AddAssembly("Assembly-CSharp", true);
            }

#if UNITY_EDITOR
            if (!ContainsAssembly("Assembly-CSharp-Editor"))
            {
                AddAssembly("Assembly-CSharp-Editor", true);
            }
#endif
        }

        private static bool ContainsAssembly(string name)
        {
            for (int i = 0; i < assemblies.Count; i++)
            {
                if (assemblies[i].assembly.GetName().Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddAssembly(string name, bool searchAppDomain = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            try
            {
                Assembly assemblyToLoad = null;
                if (searchAppDomain)
                {
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    int length = assemblies.Length;
                    for (int i = 0; i < length; i++)
                    {
                        Assembly assembly = assemblies[i];
                        if (assembly.GetName().Name == name)
                        {
                            assemblyToLoad = assembly;
                            break;
                        }
                    }
                }
                else
                {
                    assemblyToLoad = Assembly.Load(name);
                }

                if (assemblyToLoad != null)
                {
                    Type[] types = assemblyToLoad.GetTypes();
                    assemblies.Add((assemblyToLoad, types));
                }
            }
            catch
            {

            }
        }

        public static void FindCategories()
        {
            if (categories == null)
            {
                categories = new List<Category>();
                HashSet<Type> classTypesWithoutCategories = new HashSet<Type>();
                for (int a = 0; a < Assemblies.Count; a++)
                {
                    Type[] types = Assemblies[a].types;
                    int typesLength = types.Length;
                    for (int t = 0; t < typesLength; t++)
                    {
                        Type classType = types[t];
                        Category category = Category.Create(classType);
                        if (category != null)
                        {
                            if (TryGetCategory(category.Name, out Category existingCategory))
                            {
                                existingCategory.Commands.AddRange(category.Commands);
                            }
                            else
                            {
                                categories.Add(category);
                            }
                        }
                        else
                        {
                            classTypesWithoutCategories.Add(classType);
                        }
                    }
                }

                //put any commands that arent defined inside classes with a Category attribute
                //into the uncategorized category
                if (classTypesWithoutCategories.Count > 0)
                {
                    Category uncat = Uncategorized;
                    List<Command> commands = Commands;
                    foreach (Command command in commands)
                    {
                        if (classTypesWithoutCategories.Contains(command.OwnerClass))
                        {
                            uncat.Commands.Add(command);
                        }
                    }
                }

                //sort alphabetically
                categories = categories.OrderBy(x => x.Name).ToList();
            }
        }

        public static bool TryGetCategory(string name, out Category category)
        {
            category = null;
            if (categories == null)
            {
                return false;
            }

            for (int i = 0; i < categories.Count; i++)
            {
                if (categories[i].Name == name)
                {
                    category = categories[i];
                    return true;
                }
            }

            return false;
        }

        public static void AddCommand(Command command, string category = null)
        {
            if (commands == null)
            {
                FindCommands();
            }

            Category categoryToUse = Uncategorized;
            if (!string.IsNullOrEmpty(category))
            {
                if (TryGetCategory(category, out Category existingCategory))
                {
                    categoryToUse = existingCategory;
                }
            }

            foreach (Command existingCommand in categoryToUse.Commands)
            {
                if (existingCommand.GetHashCode() == command.GetHashCode())
                {
                    //already exists!
                    return;
                }
            }

            categoryToUse.Commands.Add(command);
            commands.Add(command);
        }

        public static void FindCommands()
        {
            if (commands == null)
            {
                commands = new List<Command>();
                for (int a = 0; a < Assemblies.Count; a++)
                {
                    Type[] types = Assemblies[a].types;
                    for (int t = 0; t < types.Length; t++)
                    {
                        Type type = types[t];
                        MethodInfo[] methods = type.GetMethods();
                        for (int m = 0; m < methods.Length; m++)
                        {
                            MethodInfo method = methods[m];
                            Command command = Command.Create(method, type);
                            if (command != null)
                            {
                                commands.Add(command);
                            }
                        }

                        PropertyInfo[] properties = type.GetProperties();
                        for (int p = 0; p < properties.Length; p++)
                        {
                            PropertyInfo property = properties[p];
                            Command command = Command.Create(property, type);
                            if (command != null)
                            {
                                commands.Add(command);
                            }
                        }

                        FieldInfo[] fields = type.GetFields();
                        for (int f = 0; f < fields.Length; f++)
                        {
                            FieldInfo field = fields[f];
                            Command command = Command.Create(field, type);
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
