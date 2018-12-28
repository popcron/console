using System;

namespace Popcron.Console
{
    public class CharConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(char);
            }
        }

        public CharConverter() { }

        public override object Convert(string value)
        {
            if (value == null) return null;

            string str = value.ToString();
            if (str.Length == 0) return null;

            return str[0];
        }
    }
}