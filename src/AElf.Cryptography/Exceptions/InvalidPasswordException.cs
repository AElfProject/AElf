using System;

namespace AElf.Cryptography.Exceptions
{

    public class InvalidPasswordException : Exception
    {
        public InvalidPasswordException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}