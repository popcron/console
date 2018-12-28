using System;

namespace Popcron.Console
{
    public class ConverterNotFoundException : Exception
    {
        public ConverterNotFoundException(string message) : base(message)
        {

        }
    }
}