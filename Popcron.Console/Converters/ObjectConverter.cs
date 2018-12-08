using System;

namespace Popcron.Console
{
    public class ObjectConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(object);
            }
        }

        public ObjectConverter() { }

        public override object Convert(string value)
        {
            return value as object;
        }
    }
}