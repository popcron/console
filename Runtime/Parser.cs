using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Popcron.Console
{
    public class Parser
    {
        //&lt; is escaped version of the < char
        /// <summary>
        /// A fake left angle bracket that looks like &lt; but isnt one
        /// </summary>
        public const char LeftAngleBracket = '˂';

        /// <summary>
        /// A fake right angle bracket that looks like > but isnt one
        /// </summary>
        public const char RightAngleBracket = '˃';

        /// <summary>
        /// Prefix to use when specifying instance commands with an ID.
        /// </summary>
        public const string IDPrefix = "@";

        /// <summary>
        /// A string that contains all valid characters.
        /// </summary>
        public const string ValidChars = " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890'/!@#$%^&*()_+-=[]{};':\",.˂˃/?";

        private static readonly StringBuilder stringBuilder = new StringBuilder();
        private static readonly Dictionary<string, Owner> idToOwner = new Dictionary<string, Owner>();

        public static IEnumerable<Owner> Owners
        {
            get
            {
                foreach (KeyValuePair<string, Owner> owner in idToOwner)
                {
                    yield return owner.Value;
                }
            }
        }

        /// <summary>
        /// Registers this object with a unique ID.
        /// </summary>
        public static void Register(object owner, int id)
        {
            Register(owner, id.ToString());
        }

        /// <summary>
        /// Registers this object with a unique ID.
        /// </summary>
        public static void Register(object owner, string id)
        {
            if (!idToOwner.TryGetValue(id, out Owner ownerValue))
            {
                ownerValue = new Owner();
                idToOwner.Add(id, ownerValue);
            }

            ownerValue.owner = owner;
            ownerValue.id = id;

            ownerValue.FindMethods();
            ownerValue.FindProperties();
            ownerValue.FindFields();
        }

        /// <summary>
        /// Unregisters this object so that it's no longer something that can be identified with @.
        /// </summary>
        public static void Unregister(object owner)
        {
            foreach (KeyValuePair<string, Owner> stringToOwner in idToOwner)
            {
                if (stringToOwner.Value.owner == owner)
                {
                    Unregister(stringToOwner.Value.id);
                    break;
                }
            }
        }

        /// <summary>
        /// Unregisters this object so that it's no longer something that can be identified with @.
        /// </summary>
        public static void Unregister(int id)
        {
            Unregister(id.ToString());
        }

        /// <summary>
        /// Unregisters this object so that it's no longer something that can be identified with @.
        /// </summary>
        public static void Unregister(string id)
        {
            //remove this object as an owner for any methods
            if (idToOwner.ContainsKey(id))
            {
                idToOwner.Remove(id);
            }
        }

        /// <summary>
        /// Removes any tags inside angle brackets.
        /// </summary>
        public static string RemoveRichText(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length == 0)
            {
                return input;
            }

            if (input.IndexOf('<') == -1 && input.IndexOf('>') == -1)
            {
                return input;
            }

            int length = input.Length;
            char[] array = new char[length];
            int arrayIndex = 0;
            bool inside = false;
            for (int i = 0; i < length; i++)
            {
                char c = input[i];
                if (c == '<')
                {
                    inside = true;
                    continue;
                }
                else if (c == '>')
                {
                    inside = false;
                    continue;
                }

                if (!inside)
                {
                    array[arrayIndex] = c;
                    arrayIndex++;
                }
            }

            return new string(array, 0, arrayIndex);
        }

        /// <summary>
        /// Sanitizes the input string for proper GUI rendering.
        /// </summary>
        public static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length == 0)
            {
                return string.Empty;
            }

            stringBuilder.Clear();
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '<')
                {
                    stringBuilder.Append(LeftAngleBracket);
                }
                else if (c == '>')
                {
                    stringBuilder.Append(RightAngleBracket);
                }
                else if (ValidChars.Contains(c))
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Runs a command.
        /// </summary>
        public static async Task<object> Run(string input)
        {
            //sanitize the input so that it only contains alphanumeric characters
            input = Sanitize(input);

            //if input starts with id flag
            //remove the id flag and store it separately
            string id = null;
            if (input.StartsWith(IDPrefix))
            {
                if (input.IndexOf(' ') < 0) // in case command contained only id name
                {
                    id = input.Substring(1);
                    input = string.Empty;
                }
                else
                {
                    id = input.Substring(1, input.IndexOf(' ') - 1);
                    input = input.Replace(IDPrefix + id + " ", string.Empty);
                }
            }

            int commandsCount = Library.Commands.Count;
            for (int c = 0; c < commandsCount; c++)
            {
                Command command = Library.Commands[c];
                bool nameMatch = false;
                string commandInput = null;
                for (int n = 0; n < command.Names.Count; n++)
                {
                    string name = command.Names[n];
                    if (input.StartsWith(name))
                    {
                        nameMatch = true;
                        commandInput = name;
                        break;
                    }
                }

                //name matches? oooh, ;) go on...
                if (nameMatch)
                {
                    string text = input.Replace(commandInput, "");
                    List<string> parameters = GetParameters(text);
                    if (command.Matches(parameters, out object[] converted))
                    {
                        object owner = FindOwner(command, id);
                        if (owner == null && !command.IsStatic)
                        {
                            //this was an instance method that didnt have an id
                            return new NullReferenceException($"Couldn't find owner with ID {id}");
                        }

                        //try to exec
                        try
                        {
                            object result = command.Invoke(owner, converted);
                            if (result is Task)
                            {
                                Task task = result as Task;
                                await task.ConfigureAwait(false);
                                return task.GetType().GetProperty("Result").GetValue(task);
                            }
                            else
                            {
                                return result;
                            }
                        }
                        catch (Exception exception)
                        {
                            return exception;
                        }
                    }
                }
            }

            if (Settings.Current.reportUnknownCommand)
            {
                return "Command not found";
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns an object with this registered ID for this command.
        /// </summary>
        public static object FindOwner(Command command, string id)
        {
            //id passed was null, stoppp
            if (id == null)
            {
                return null;
            }

            //static commands cant have any owners
            if (command.IsStatic)
            {
                return null;
            }

            string memberName = command.Name;
            if (idToOwner.TryGetValue(id, out Owner owner))
            {
                //check methods
                for (int i = 0; i < owner.methods.Count; i++)
                {
                    if (owner.methods[i].name == memberName)
                    {
                        return owner.owner;
                    }
                }

                //check properties
                for (int i = 0; i < owner.properties.Count; i++)
                {
                    if (owner.properties[i].name == memberName)
                    {
                        return owner.owner;
                    }
                }

                //check fields
                for (int i = 0; i < owner.fields.Count; i++)
                {
                    if (owner.fields[i].name == memberName)
                    {
                        return owner.owner;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a list of strings separated by a space from this text.
        /// </summary>
        public static List<string> GetParameters(string input)
        {
            List<string> parameters = Regex.Matches(input, @"[\""].+?[\""]|[^ ]+").Cast<Match>().Select(x => x.Value).ToList();
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].StartsWith("\"") && parameters[i].EndsWith("\""))
                {
                    parameters[i] = parameters[i].TrimStart('\"').TrimEnd('\"');
                }
            }

            return parameters;
        }
    }
}
