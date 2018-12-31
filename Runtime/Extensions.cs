using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Popcron.Console
{
    internal static class Extensions
    {
        public static CommandAttribute GetCommand(this MemberInfo member)
        {
            CommandAttribute attribute = null;
            object[] attributes = member.GetCustomAttributes(typeof(CommandAttribute), false);
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

        public static AliasAttribute[] GetAliases(this MemberInfo member)
        {
            List<AliasAttribute> aliases = new List<AliasAttribute>();
            object[] attributes = member.GetCustomAttributes(typeof(AliasAttribute), false);
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

        public static CategoryAttribute GetCategoryAttribute(this Type type)
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

        public static char GetCharFromKeyCode(this KeyCode keyCode)
        {
            int index = (int)keyCode;
            return (char)index;
        }
    }
}
