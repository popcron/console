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
        private readonly MethodInfo method;
        private readonly CommandAttribute attribute;
        private readonly List<ParameterInfo> parameters;
        private readonly Type owner;
        private readonly List<string> names = new List<string>();

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

        internal Type Owner
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

        public List<ParameterInfo> Parameters
        {
            get
            {
                return parameters;
            }
        }

        public bool IsStatic
        {
            get
            {
                return method.IsStatic;
            }
        }

        public MethodInfo Method
        {
            get
            {
                return method;
            }
        }

        private Command(MethodInfo method, CommandAttribute attribute, Type owner)
        {
            this.method = method;
            this.attribute = attribute;
            this.owner = owner;

            //find alias attributes
            var aliases = method.GetAliases();
            foreach (var alias in aliases)
            {
                names.Add(alias.name);
            }

            names.Add(Name);

            //set parameters
            ParameterInfo[] parameters = method.GetParameters();
            this.parameters = new List<ParameterInfo>(parameters.Length);
            for (int i = 0; i < parameters.Length; i++)
            {
                this.parameters.Add(parameters[i]);
            }
        }

        public object Invoke(object owner, params object[] parameters)
        {
            return method.Invoke(owner, parameters);
        }

        public static Command Create(MethodInfo method, Type type)
        {
            CommandAttribute attribute = method.GetCommand();

            if (attribute == null) return null;

            Command command = new Command(method, attribute, type);
            return command;
        }

        public bool Matches(List<string> parameters, out object[] converted)
        {
            converted = null;

            //amount mismatch
            if (parameters.Count != Parameters.Count) return false;

            //try to infer the type from input parameters
            converted = new object[Parameters.Count];
            for (int i = 0; i < Parameters.Count; i++)
            {
                string parameter = parameters[i];
                Type parameterType = Parameters[i].ParameterType;

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
                    converted[i] = propValue;
                }
            }

            return true;
        }
    }
}