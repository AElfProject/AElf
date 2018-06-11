using System;

namespace AElf.Cryptography.ECDSA.Exceptions
{
    public class KeyStoreNotFoundException : Exception
    {
        public KeyStoreNotFoundException(string msg, Exception ex) : base(msg, ex)
        {
        }
    }
}