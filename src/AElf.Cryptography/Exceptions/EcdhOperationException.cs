using System;

namespace AElf.Cryptography.Exceptions
{

    public class EcdhOperationException : Exception
    {
        public EcdhOperationException(string message) : base(message)
        {
        }
    }
}