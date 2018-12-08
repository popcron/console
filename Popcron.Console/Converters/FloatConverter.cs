using System;

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
            if (float.TryParse(value, out result))
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