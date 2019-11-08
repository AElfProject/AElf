using System;

namespace AElf.Cryptography.ECDSA.Exceptions
{
    public class InvalidPrivateKeyException : Exception
    {
        public InvalidPrivateKeyException(string message) : base(message)
        {
        }
    }
}