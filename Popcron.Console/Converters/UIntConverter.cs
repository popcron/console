using System;

namespace Popcron.Console
{
    public class UIntConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(uint);
            }
        }

        public UIntConverter() { }

        public override object Convert(string value)
        {
            uint result;
            if (uint.TryParse(value, out result))
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