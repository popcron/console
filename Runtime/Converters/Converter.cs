using System;
using System.Collections.Generic;
using System.Reflection;

namespace Popcron.Console
{
    public abstract class Converter
    {
        private static Dictionary<Type, Converter> typeToConverter = null;

        public static List<Converter> Converters
        {
            get
            {
                List<Converter> converters = new List<Converter>();

                foreach (var converter in typeToConverter)
                {
                    converters.Add(converter.Value);
                }

                return converters;
            }
        }

        public static Converter GetConverter(Type type)
        {
            if (typeToConverter == null)
            {
                FindConverters();
            }

            if (typeToConverter.TryGetValue(type, out Converter converter))
            {
                return converter;
            }
            else
            {
                return null;
            }
        }

        public static void FindConverters()
        {
            //add the default ones first
            typeToConverter = new Dictionary<Type, Converter>
            {
                { typeof(bool), new BooleanConverter() },
                { typeof(string), new StringConverter() },
                { typeof(sbyte), new SByteConverter() },
                { typeof(byte), new ByteConverter() },
                { typeof(short), new ShortConverter() },
                { typeof(ushort), new UShortConverter() },
                { typeof(int), new IntConverter() },
                { typeof(uint), new UIntConverter() },
                { typeof(long), new LongConverter() },
                { typeof(ulong), new ULongConverter() },
                { typeof(float), new FloatConverter() },
                { typeof(double), new DoubleConverter() },
                { typeof(char), new CharConverter() },
                { typeof(Type), new TypeConverter() },
                { typeof(DateTime), new DateTimeConverter() },
                { typeof(object), new ObjectConverter() }
            };

            //then find any extras
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsAbstract) continue;

                    bool isSubclass = type.IsSubclassOf(typeof(Converter));
                    if (!isSubclass) continue;

                    Converter converter = (Converter)Activator.CreateInstance(type);
                    if (typeToConverter.ContainsKey(converter.Type)) continue;

                    typeToConverter.Add(converter.Type, converter);
                }
            }
        }

        public abstract Type Type { get; }
        public abstract object Convert(string value);
    }
}
