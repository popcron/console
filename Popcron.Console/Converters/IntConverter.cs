using System;

namespace Popcron.Console
{
    public class IntConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(int);
            }
        }

        public IntConverter() { }

        public override object Convert(string value)
        {
            int result;
            if (int.TryParse(value, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }
    }
}