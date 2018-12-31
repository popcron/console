using System;
using System.Collections.Generic;
using System.Reflection;

namespace Popcron.Console
{
    [Serializable]
    public class Owner
    {
        public class OwnerMember
        {
            public string name;
            public string description;
            public Type type;
            public bool canGetProperty;
            public bool canSetProperty;

            private object owner;
            private MemberInfo member;
            private object value;

            public object Value
            {
                get
                {
                    if (value == null)
                    {
                        if (member is FieldInfo field)
                        {
                            value = field.GetValue(owner);
                        }
                        else
                        {
                            return null;
                        }
                    }

                    return value;
                }
            }

            public OwnerMember(object owner, MemberInfo member)
            {
                this.owner = owner;
                this.member = member;
            }
        }

        public string id;
        public object owner;
        public List<OwnerMember> methods;
        public List<OwnerMember> properties;
        public List<OwnerMember> fields;

        internal void FindMethods()
        {
            //reset
            if (methods == null)
            {
                methods = new List<OwnerMember>();
            }
            else
            {
                methods.Clear();
            }

            //try to add all of its instance methods
            MethodInfo[] allMethods = owner.GetType().GetMethods();
            for (int i = 0; i < allMethods.Length; i++)
            {
                //short circuit if static
                if (allMethods[i].IsStatic) continue;

                CommandAttribute attribute = allMethods[i].GetCommand();
                if (attribute != null)
                {
                    OwnerMember method = new OwnerMember(owner, allMethods[i])
                    {
                        name = attribute.name,
                        description = attribute.description,
                        type = allMethods[i].ReturnType
                    };
                    methods.Add(method);
                }
            }
        }

        internal void FindProperties()
        {
            //reset
            if (properties == null)
            {
                properties = new List<OwnerMember>();
            }
            else
            {
                properties.Clear();
            }

            //try to add all of its instance properties
            PropertyInfo[] allProperties = owner.GetType().GetProperties();
            for (int i = 0; i < allProperties.Length; i++)
            {
                //short circuit if static
                MethodInfo get = allProperties[i].GetGetMethod();
                MethodInfo set = null;
                if (get != null && get.IsStatic) continue;
                else if (get == null)
                {
                    set = allProperties[i].GetSetMethod();
                    if (set != null && set.IsStatic) continue;
                }

                CommandAttribute attribute = allProperties[i].GetCommand();
                if (attribute != null)
                {
                    OwnerMember method = new OwnerMember(owner, allProperties[i])
                    {
                        name = attribute.name,
                        description = attribute.description,
                        type = get?.ReturnType,
                        canGetProperty = get != null,
                        canSetProperty = set != null
                    };
                    properties.Add(method);
                }
            }
        }

        internal void FindFields()
        {
            //reset methods
            if (fields == null)
            {
                fields = new List<OwnerMember>();
            }
            else
            {
                fields.Clear();
            }

            //try to add all of its instance methods
            FieldInfo[] allFields = owner.GetType().GetFields();
            for (int i = 0; i < allFields.Length; i++)
            {
                //short circuit if static
                if (allFields[i].IsStatic) continue;

                CommandAttribute attribute = allFields[i].GetCommand();
                if (attribute != null)
                {
                    OwnerMember method = new OwnerMember(owner, allFields[i])
                    {
                        name = attribute.name,
                        description = attribute.description,
                        type = allFields[i].FieldType
                    };
                    fields.Add(method);
                }
            }
        }
    }
}
