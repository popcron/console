using System;

namespace Popcron.Console
{
    public class DateTimeConverter : Converter
    {
        public override Type Type
        {
            get
            {
                return typeof(DateTime);
            }
        }

        public DateTimeConverter() { }

        public override object Convert(string value)
        {
            DateTime result;
            if (DateTime.TryParse(value, out result))
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