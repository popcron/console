using System;

namespace Popcron.Console
{
    public class BooleanConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(bool);
            }
        }

        public BooleanConverter() { }

        public override object Convert(string value)
        {
            return value == "True";
        }
    }
}