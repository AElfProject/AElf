using System;

namespace AElf.Cryptography.ECDSA.Exceptions
{
    public class InvalidKeyPairException : Exception
    {
        public InvalidKeyPairException(string msg, Exception e) : base(msg, e)
        {
        }
    }
}