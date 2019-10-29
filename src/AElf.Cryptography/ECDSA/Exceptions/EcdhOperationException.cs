using System;

namespace AElf.Cryptography.ECDSA.Exceptions
{
    public class EcdhOperationException : Exception
    {
        public EcdhOperationException(string msg) : base(msg)
        {
            
        }
    }
}