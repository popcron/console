using System;

namespace Popcron.Console
{
    public class ULongConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(ulong);
            }
        }

        public ULongConverter() { }

        public override object Convert(string value)
        {
            ulong result;
            if (ulong.TryParse(value, out result))
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