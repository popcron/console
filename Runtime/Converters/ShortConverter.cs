using System;

namespace Popcron.Console
{
    public class ShortConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(short);
            }
        }

        public ShortConverter() { }

        public override object Convert(string value)
        {
            short result;
            if (short.TryParse(value, out result))
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