using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Popcron.Console
{
    [Serializable]
    public class Command
    {
        private MethodInfo method;
        private PropertyInfo property;
        private FieldInfo field;

        private MethodInfo get;
        private MethodInfo set;

        private CommandAttribute attribute;
        private List<Type> parameterTypes = new List<Type>();
        private List<string> parameterNames = new List<string>();
        private Type owner;
        private List<string> names = new List<string>();

        public string Name
        {
            get
            {
                return attribute.name;
            }
        }

        public List<string> Names
        {
            get
            {
                return names;
            }
        }

        public Type Owner
        {
            get
            {
                return owner;
            }
        }

        public string Description
        {
            get
            {
                return attribute.description;
            }
        }

        public List<string> Parameters
        {
            get
            {
                return parameterNames;
            }
        }

        public bool IsStatic
        {
            get
            {
                if (method != null) return method.IsStatic;
                if (field != null) return field.IsStatic;
                if (property != null)
                {
                    if (get != null) return get.IsStatic;
                    if (set != null) return get.IsStatic;
                }

                return false;
            }
        }

        public MemberInfo Member
        {
            get
            {
                if (method != null) return method;
                if (property != null) return property;
                if (field != null) return field;

                return null;
            }
        }

        private Command(MethodInfo method, CommandAttribute attribute, Type owner)
        {
            this.method = method;
            Initialize(attribute, owner);
        }

        private Command(PropertyInfo property, CommandAttribute attribute, Type owner)
        {
            this.property = property;
            Initialize(attribute, owner);
        }

        private Command(FieldInfo field, CommandAttribute attribute, Type owner)
        {
            this.field = field;
            Initialize(attribute, owner);
        }

        private void Initialize(CommandAttribute attribute, Type owner)
        {
            this.attribute = attribute;
            this.owner = owner;

            //find alias attributes
            var aliases = Member.GetAliases();
            foreach (var alias in aliases)
            {
                names.Add(alias.name);
            }

            names.Add(Name);

            if (method != null)
            {
                //set parameters
                ParameterInfo[] p = method.GetParameters();
                for (int i = 0; i < p.Length; i++)
                {
                    parameterTypes.Add(p[i].ParameterType);
                    parameterNames.Add(p[i].Name);
                }
            }
            else if (property != null)
            {
                ParameterInfo parameter = null;
                get = property.GetGetMethod();
                set = property.GetSetMethod();

                if (get != null)
                {
                    parameter = get.ReturnParameter;
                }
                if (set != null)
                {
                    parameter = set.ReturnParameter;
                }

                if (parameter != null)
                {
                    parameterTypes.Add(parameter.ParameterType);
                    parameterNames.Add(parameter.Name);
                }
            }
            else if (field != null)
            {
                parameterTypes.Add(field.FieldType);
                parameterNames.Add(field.Name);
            }
        }

        public object Invoke(object owner, params object[] parameters)
        {
            if (method != null)
            {
                return method.Invoke(owner, parameters);
            }
            else if (property != null)
            {
                if (parameters == null || parameters.Length == 0)
                {
                    //if no parameters were passed, then get
                    if (get != null)
                    {
                        return get.Invoke(owner, parameters);
                    }
                }
                else if (parameters != null && parameters.Length == 1)
                {
                    //if 1 parameter was passed, then set
                    if (set != null)
                    {
                        property.SetValue(owner, parameters[0]);
                    }
                }
            }
            else if (field != null)
            {
                if (parameters == null || parameters.Length == 0)
                {
                    return field.GetValue(owner);
                }
                else if (parameters != null && parameters.Length == 1)
                {
                    field.SetValue(owner, parameters[0]);
                }
            }

            return null;
        }

        public static Command Create(MethodInfo method, Type type)
        {
            CommandAttribute attribute = method.GetCommand();

            if (attribute == null) return null;

            Command command = new Command(method, attribute, type);
            return command;
        }

        public static Command Create(PropertyInfo property, Type type)
        {
            CommandAttribute attribute = property.GetCommand();

            if (attribute == null) return null;

            Command command = new Command(property, attribute, type);
            return command;
        }

        public static Command Create(FieldInfo field, Type type)
        {
            CommandAttribute attribute = field.GetCommand();

            if (attribute == null) return null;

            Command command = new Command(field, attribute, type);
            return command;
        }

        public bool Matches(List<string> parametersGiven, out object[] convertedParameters)
        {
            convertedParameters = null;

            //parameter amount mismatch
            bool isProperty = property != null && parametersGiven.Count <= 2;
            bool isField = field != null && parametersGiven.Count <= 2;
            if (parametersGiven.Count != Parameters.Count && method != null)
            {
                return false;
            }

            if (isProperty || isField)
            {
                if (parametersGiven.Count == 0)
                {
                    //get the value
                    return true;
                }
            }

            //try to infer the type from input parameters
            convertedParameters = new object[parameterTypes.Count];
            for (int i = 0; i < parameterTypes.Count; i++)
            {
                string parameter = parametersGiven[i];
                Type parameterType = parameterTypes[i];

                Converter converter = Converter.GetConverter(parameterType);
                if (converter == null)
                {
                    throw new ConverterNotFoundException("No converter for type " + parameterType + " was found");
                }

                object propValue = converter.Convert(parameter);
                if (propValue == null)
                {
                    throw new FailedToConvertException("Failed to convert " + parameter + " to type " + parameterType + " using " + converter.GetType());
                }
                else
                {
                    convertedParameters[i] = propValue;
                }
            }

            return true;
        }
    }
}