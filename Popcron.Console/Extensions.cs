using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Popcron.Console
{
    internal static class Extensions
    {
        public static CommandAttribute GetCommand(this MethodInfo method)
        {
            CommandAttribute attribute = null;
            object[] attributes = method.GetCustomAttributes(typeof(CommandAttribute), false);
            for (int a = 0; a < attributes.Length; a++)
            {
                if (attributes[a].GetType() == typeof(CommandAttribute))
                {
                    attribute = attributes[a] as CommandAttribute;
                    return attribute;
                }
            }

            return null;
        }

        public static AliasAttribute[] GetAliases(this MethodInfo method)
        {
            List<AliasAttribute> aliases = new List<AliasAttribute>();
            object[] attributes = method.GetCustomAttributes(typeof(AliasAttribute), false);
            for (int a = 0; a < attributes.Length; a++)
            {
                if (attributes[a].GetType() == typeof(AliasAttribute))
                {
                    AliasAttribute attribute = attributes[a] as AliasAttribute;
                    aliases.Add(attribute);
                }
            }

            return aliases.ToArray();
        }

        public static CategoryAttribute GetCategory(this Type type)
        {
            CategoryAttribute attribute = null;
            object[] attributes = type.GetCustomAttributes(typeof(CategoryAttribute), false);
            for (int a = 0; a < attributes.Length; a++)
            {
                if (attributes[a].GetType() == typeof(CategoryAttribute))
                {
                    attribute = attributes[a] as CategoryAttribute;
                    return attribute;
                }
            }

            return null;
        }
    }
}
