using System;

namespace Popcron.Console
{
    public class FailedToConvertException : Exception
    {
        public FailedToConvertException(string message) : base(message)
        {

        }
    }
}