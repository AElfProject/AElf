using System;

namespace AElf.Crosschain.Exceptions
{
    public class PrivateKeyException : Exception
    {
        public PrivateKeyException(string unableToLoadPrivateKey) : base(unableToLoadPrivateKey)
        {
        }
    }
}