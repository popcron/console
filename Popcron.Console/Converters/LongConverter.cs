using System;

namespace Popcron.Console
{
    public class LongConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(long);
            }
        }

        public LongConverter() { }

        public override object Convert(string value)
        {
            long result;
            if (long.TryParse(value, out result))
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