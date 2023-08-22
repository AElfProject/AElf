using System;

namespace AElf.Cryptography.Exceptions
{

    public class InvalidPrivateKeyException : Exception
    {
        public InvalidPrivateKeyException(string message) : base(message)
        {
        }
    }
}