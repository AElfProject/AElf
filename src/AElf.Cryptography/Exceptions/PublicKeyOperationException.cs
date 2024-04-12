using System;

namespace AElf.Cryptography.Exceptions
{

    public class PublicKeyOperationException : Exception
    {
        public PublicKeyOperationException(string message) : base(message)
        {
        }
    }
}