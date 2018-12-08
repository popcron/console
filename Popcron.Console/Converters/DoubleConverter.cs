using System;

namespace Popcron.Console
{
    public class DoubleConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(double);
            }
        }

        public DoubleConverter() { }

        public override object Convert(string value)
        {
            double result;
            if (double.TryParse(value, out result))
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