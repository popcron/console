using System;

namespace Popcron.Console
{
    public class SByteConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(sbyte);
            }
        }

        public SByteConverter() { }

        public override object Convert(string value)
        {
            sbyte result;
            if (sbyte.TryParse(value, out result))
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