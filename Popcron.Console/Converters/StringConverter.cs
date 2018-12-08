using System;

namespace Popcron.Console
{
    public class StringConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(string);
            }
        }

        public StringConverter() { }

        public override object Convert(string value)
        {
            return value;
        }
    }
}