#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Popcron.Console
{
    [InitializeOnLoad]
    public static class Generator
    {
        private static readonly StringBuilder contents = new StringBuilder();
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static Func<string> PathToFile { get; } = GetPathToFile;

        static Generator()
        {
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private static string GetPathToFile()
        {
            string typeName = "CommandLoader";
            string folder = Path.Combine(Application.dataPath, "Code", "Generated");
            string path = Path.Combine(folder, $"{typeName}.generated.cs");
            return path;
        }

        private static (List<Type>, List<MemberInfo>, List<int>) GetAll()
        {
            List<Type> categoriesFound = new List<Type>();
            List<MemberInfo> membersFound = new List<MemberInfo>();
            List<int> indices = new List<int>();

            AppDomain domain = AppDomain.CurrentDomain;
            Assembly[] assemblies = domain.GetAssemblies();
            int assemblyCount = assemblies.Length;
            for (int a = 0; a < assemblyCount; a++)
            {
                Assembly assembly = assemblies[a];
                Type[] types = assembly.GetTypes();
                int typeCount = types.Length;
                for (int t = 0; t < typeCount; t++)
                {
                    Type type = types[t];
                    if (type.GetCustomAttribute<CategoryAttribute>() != null)
                    {
                        categoriesFound.Add(type);
                    }

                    MemberInfo[] members = type.GetMembers(Flags);
                    int memberCount = members.Length;
                    for (int m = 0; m < memberCount; m++)
                    {
                        MemberInfo member = members[m];
                        try
                        {
                            if (member.GetCustomAttribute<CommandAttribute>() != null)
                            {
                                membersFound.Add(member);
                                indices.Add(m);
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }

            return (categoriesFound, membersFound, indices);
        }

        private static void OnAfterAssemblyReload()
        {
            const string Indent = "    ";
            List<string> namespaces = new List<string>();
            namespaces.Add("UnityEngine");
            namespaces.Add("UnityEngine.Scripting");
            namespaces.Add("System");
            namespaces.Add("System.Reflection");
            namespaces.Add("System.Collections.Generic");

            (List<Type> categories, List<MemberInfo> members, List<int> indices) = GetAll();
            string myNamespace = "Popcron.Console";
            string path = PathToFile.Invoke();
            string folder = Directory.GetParent(path).FullName;
            string typeName = Path.GetFileName(path);
            if (typeName.Contains('.'))
            {
                typeName = typeName.Substring(0, typeName.IndexOf('.'));
            }

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            contents.Clear();
            foreach (string ns in namespaces)
            {
                contents.Append("using ");
                contents.Append(ns);
                contents.AppendLine(";");
            }

            contents.AppendLine();
            contents.Append("namespace ");
            contents.AppendLine(myNamespace);
            contents.AppendLine("{");

            contents.Append(Indent);
            contents.AppendLine("using Type = System.Type;");
            contents.AppendLine();

            contents.Append(Indent);
            contents.AppendLine("[Preserve]");

            contents.Append(Indent);
            contents.Append("public static class ");
            contents.AppendLine(typeName);

            contents.Append(Indent);
            contents.AppendLine("{");

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("private const BindingFlags Flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("private static Dictionary<Type, MemberInfo[]> typeToMembers = new Dictionary<Type, MemberInfo[]>();");
            contents.AppendLine();

            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("#if UNITY_EDITOR");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("[UnityEditor.Callbacks.DidReloadScripts]");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("private static void LoadBecauseRecompile()");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("{");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("Load();");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("}");

            contents.AppendLine();
            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("#endif");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("private static MemberInfo GetMemberOf(Type type, int index)");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("{");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("if (!typeToMembers.TryGetValue(type, out MemberInfo[] members))");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("{");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("members = type.GetMembers(Flags);");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("typeToMembers[type] = members;");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("}");
            contents.AppendLine();

            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("return members[index];");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("}");
            contents.AppendLine();

            contents.AppendLine();
            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("[RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType.AfterAssembliesLoaded), Preserve]");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append("private static void Load()");
            contents.AppendLine();

            contents.Append(Indent);
            contents.Append(Indent);
            contents.AppendLine("{");

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.AppendLine("Command command;");

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.AppendLine("Category category;");

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.AppendLine("Type type;");

            contents.Append(Indent);
            contents.Append(Indent);
            contents.Append(Indent);
            contents.AppendLine("MemberInfo member;");

            contents.AppendLine();

            foreach (Type category in categories)
            {
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append("type = Type.GetType(\"");
                contents.Append(category.FullName);
                contents.Append("\");");
                contents.AppendLine();

                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append("category = Category.Create(type);");
                contents.AppendLine();

                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append("Library.AddCategory(category);");
                contents.AppendLine();

                contents.AppendLine();
            }

            int memberCount = members.Count;
            Type lastOwningType = null;
            for (int i = 0; i < memberCount; i++)
            {
                MemberInfo member = members[i];
                int index = indices[i];
                CommandAttribute command = member.GetCustomAttribute<CommandAttribute>();
                string name = command.name;
                string description = command.description;
                Type owningType = member.DeclaringType;
                CategoryAttribute category = owningType.GetCustomAttribute<CategoryAttribute>();

                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append("try");
                contents.AppendLine();

                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append("{");
                contents.AppendLine();

                if (lastOwningType != owningType)
                {
                    lastOwningType = owningType;
                    contents.Append(Indent);
                    contents.Append(Indent);
                    contents.Append(Indent);
                    contents.Append(Indent);
                    contents.Append("type = Type.GetType(\"");
                    contents.Append(owningType.FullName);
                    contents.Append("\");");
                    contents.AppendLine();
                }

                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append("member = GetMemberOf(type, ");
                contents.Append(index);
                contents.Append(");");
                contents.AppendLine();

                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append("command = Command.Create(\"");
                contents.Append(name);
                contents.Append("\", \"");
                contents.Append(description);
                contents.Append("\", member, type);");
                contents.AppendLine();

                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                if (category == null)
                {
                    contents.Append("Library.AddCommand(command);");
                    contents.AppendLine();
                }
                else
                {
                    contents.Append("Library.AddCommand(command, \"");
                    contents.Append(category.Name);
                    contents.Append("\");");
                    contents.AppendLine();
                }

                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append("}");
                contents.AppendLine();

                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append(Indent);
                contents.Append("catch { }");
                contents.AppendLine();

                contents.AppendLine();
            }

            contents.Append(Indent);
            contents.Append(Indent);
            contents.AppendLine("}");

            contents.Append(Indent);
            contents.AppendLine("}");
            contents.AppendLine("}");

            string fileContent = contents.ToString();
            if (!File.Exists(path) || File.ReadAllText(path) != fileContent)
            {
                File.WriteAllText(path, fileContent);
            }
        }
    }
}
#endif