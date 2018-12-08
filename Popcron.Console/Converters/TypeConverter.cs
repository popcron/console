using System;

namespace Popcron.Console
{
    public class TypeConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(Type);
            }
        }

        public TypeConverter() { }

        public override object Convert(string value)
        {
            Type type = Type.GetType(value);
            return type;
        }
    }
}