using System;
using System.Collections.Generic;
using System.Reflection;

namespace Popcron.Console
{
    [Serializable]
    public class Command
    {
        public delegate void OnMethodInvokedEvent();

        private string name;
        private List<string> names = new List<string>();
        private string description;
        private MethodInfo method;
        private PropertyInfo property;
        private FieldInfo field;

        private MethodInfo get;
        private MethodInfo set;

        private Type ownerClass;
        private List<object> parameters = new List<object>();

        public string Name => name;
        public List<string> Names => names;

        /// <summary>
        /// The class in which the command is defined in.
        /// </summary>
        public Type OwnerClass => ownerClass;

        public string Description => description;
        public OnMethodInvokedEvent OnMethodInvoked { get; set; }

        public List<string> Parameters
        {
            get
            {
                List<string> names = new List<string>();
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i] is FieldInfo field)
                    {
                        names.Add(field.Name);
                    }
                    else if (parameters[i] is ParameterInfo param)
                    {
                        names.Add(param.Name);
                    }
                }

                return names;
            }
        }

        public bool IsStatic
        {
            get
            {
                if (method != null)
                {
                    return method.IsStatic;
                }
                else if (field != null)
                {
                    return field.IsStatic;
                }
                else if (property != null)
                {
                    if (get != null)
                    {
                        return get.IsStatic;
                    }
                    else if (set != null)
                    {
                        return get.IsStatic;
                    }
                }

                return false;
            }
        }

        public MemberInfo Member
        {
            get
            {
                if (method != null)
                {
                    return method;
                }
                else if (property != null)
                {
                    return property;
                }
                else if (field != null)
                {
                    return field;
                }

                return null;
            }
        }

        private Command(string name, string description, MethodInfo method, Type ownerClassType = null)
        {
            this.name = name;
            this.description = description;
            this.method = method;
            this.ownerClass = ownerClassType;
            Initialize();
        }

        private Command(string name, string description, PropertyInfo property, Type ownerClassType = null)
        {
            this.name = name;
            this.description = description;
            this.property = property;
            this.ownerClass = ownerClassType;
            Initialize();
        }

        private Command(string name, string description, FieldInfo field, Type ownerClassType = null)
        {
            this.name = name;
            this.description = description;
            this.field = field;
            this.ownerClass = ownerClassType;
            Initialize();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 1622228699;
                hashCode = hashCode * -1521134295 + name.GetHashCode();

                if (!string.IsNullOrEmpty(description))
                {
                    hashCode = hashCode * -1521134295 + description.GetHashCode();
                }

                hashCode = hashCode * -1521134295 + Member.GetHashCode();
                return hashCode;
            }
        }

        private void Initialize()
        {
            //find alias attributes
            AliasAttribute[] aliases = Member.GetAliases();
            foreach (AliasAttribute alias in aliases)
            {
                names.Add(alias.name);
            }

            names.Add(Name);

            if (method != null)
            {
                //set parameters
                parameters.Clear();
                ParameterInfo[] methodParameters = method.GetParameters();
                for (int i = 0; i < methodParameters.Length; i++)
                {
                    parameters.Add(methodParameters[i]);
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

                if (parameter != null)
                {
                    parameters.Add(parameter);
                }
            }
            else if (field != null)
            {
                parameters.Add(field);
            }
        }

        /// <summary>
        /// Returns the parameter info from this command with this name.
        /// </summary>
        public ParameterInfo GetParameter(string parameterName)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i] is ParameterInfo parameter)
                {
                    if (parameter.Name == parameterName)
                    {
                        return parameter;
                    }
                }
            }

            return null;
        }

        public object Invoke(object owner, params object[] parameters)
        {
            if (method != null)
            {
                OnMethodInvoked?.Invoke();
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

        public static Command Create(string name, string description, MemberInfo member, Type memberOwningType = null)
        {
            if (member is FieldInfo field)
            {
                return Create(name, description, field, memberOwningType);
            }
            else if (member is MethodInfo method)
            {
                return Create(name, description, method, memberOwningType);
            }
            else if (member is PropertyInfo property)
            {
                return Create(name, description, property, memberOwningType);
            }
            else
            {
                return null;
            }
        }

        public static Command Create(string name, string description, MethodInfo method, Type methodOwningType = null)
        {
            Command command = new Command(name, description, method, methodOwningType);
            return command;
        }

        public static Command Create(MethodInfo method, Type methodOwningType = null)
        {
            CommandAttribute attribute = method.GetCommand();
            if (attribute == null)
            {
                return null;
            }

            return Create(attribute.name, attribute.description, method, methodOwningType);
        }

        public static Command Create(string name, string description, PropertyInfo property, Type propertyOwningType = null)
        {
            Command command = new Command(name, description, property, propertyOwningType);
            return command;
        }

        public static Command Create(PropertyInfo property, Type propertyOwningType = null)
        {
            CommandAttribute attribute = property.GetCommand();
            if (attribute == null)
            {
                return null;
            }

            return Create(attribute.name, attribute.description, property, propertyOwningType);
        }

        public static Command Create(string name, string description, FieldInfo field, Type fieldOwningType = null)
        {
            Command command = new Command(name, description, field, fieldOwningType);
            return command;
        }

        public static Command Create(FieldInfo field, Type fieldOwningType = null)
        {
            CommandAttribute attribute = field.GetCommand();
            if (attribute == null)
            {
                return null;
            }

            return Create(attribute.name, attribute.description, field, fieldOwningType);
        }

        public bool Matches(List<string> parametersGiven, out object[] convertedParameters)
        {
            convertedParameters = null;

            bool isProperty = property != null && parametersGiven.Count <= 2;
            bool isField = field != null && parametersGiven.Count <= 2;

            //get the total amount of params required
            int paramsRequired = 0;
            int optionalParams = 0;
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i] is FieldInfo field)
                {
                    paramsRequired++;
                }
                else if (parameters[i] is ParameterInfo param)
                {
                    if (!param.IsOptional)
                    {
                        paramsRequired++;
                    }
                    else
                    {
                        optionalParams++;
                    }
                }
            }

            //parameter amount mismatch
            if (method != null)
            {
                if (parametersGiven.Count < paramsRequired || parametersGiven.Count > paramsRequired + optionalParams)
                {
                    return false;
                }
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
            convertedParameters = new object[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                string parameter = null;
                ParameterInfo param = parameters[i] as ParameterInfo;
                FieldInfo field = parameters[i] as FieldInfo;
                if (i >= parametersGiven.Count)
                {
                    //out of bounds, optional parameter
                    param = parameters[i] as ParameterInfo;
                    parameter = param.DefaultValue.ToString();
                }
                else
                {
                    parameter = parametersGiven[i];
                }

                object propValue = null;
                Type parameterType = param?.ParameterType ?? field.FieldType;
                if (!parameterType.IsEnum)
                {
                    Converter converter = Converter.GetConverter(parameterType);
                    if (converter == null)
                    {
                        throw new ConverterNotFoundException("No converter for type " + parameterType.Name + " was found");
                    }

                    propValue = converter.Convert(parameter);

                    //couldnt get a value
                    if (propValue == null)
                    {
                        throw new FailedToConvertException("Failed to convert " + parameter + " to type " + parameterType.Name + " using " + converter.GetType().Name);
                    }
                }
                else
                {
                    //manually convert here if its an enum
                    propValue = Enum.Parse(parameterType, parameter);

                    //couldnt get a value
                    if (propValue == null)
                    {
                        throw new FailedToConvertException("Failed to parse " + parameter + " to " + parameterType.Name);
                    }
                }

                convertedParameters[i] = propValue;
            }

            return true;
        }
    }
}