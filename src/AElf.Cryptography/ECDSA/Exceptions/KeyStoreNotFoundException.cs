using System;

namespace AElf.Cryptography.ECDSA.Exceptions
{
    public class KeyStoreNotFoundException : Exception
    {
        public KeyStoreNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}