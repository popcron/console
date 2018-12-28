using System;

namespace Popcron.Console
{
    public class UShortConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(ushort);
            }
        }

        public UShortConverter() { }

        public override object Convert(string value)
        {
            ushort result;
            if (ushort.TryParse(value, out result))
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