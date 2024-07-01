using System;

namespace AElf.Cryptography.Exceptions
{

    public class InvalidKeyPairException : Exception
    {
        public InvalidKeyPairException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}