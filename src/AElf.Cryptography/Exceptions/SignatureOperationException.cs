using System;

namespace AElf.Cryptography.Exceptions
{

    public class SignatureOperationException : Exception
    {
        public SignatureOperationException(string message) : base(message)
        {
        }
    }
}