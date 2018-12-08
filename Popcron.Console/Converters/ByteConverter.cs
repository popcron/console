using System;

namespace Popcron.Console
{
    public class ByteConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(byte);
            }
        }

        public ByteConverter() { }

        public override object Convert(string value)
        {
            byte result;
            if (byte.TryParse(value, out result))
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