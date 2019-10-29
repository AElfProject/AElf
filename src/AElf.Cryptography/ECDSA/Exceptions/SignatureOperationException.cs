using System;

namespace AElf.Cryptography.ECDSA.Exceptions
{
    public class SignatureOperationException : Exception
    {
        public SignatureOperationException(string msg) : base(msg)
        {
            
        }
    }
}