using System;

namespace AElf.Kernel.CodeCheck
{
    public class InvalidCodeException : Exception
    {
        public InvalidCodeException()
        {
        }

        public InvalidCodeException(string message) : base(message)
        {
        }
    }
}