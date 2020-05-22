using System;
using System.Globalization;

namespace Popcron.Console
{
    public class FloatConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(float);
            }
        }

        public FloatConverter() { }

        public override object Convert(string value)
        {
            float result;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
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