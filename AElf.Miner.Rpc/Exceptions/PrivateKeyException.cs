using System;

namespace AElf.Miner.Rpc.Exceptions
{
    public class PrivateKeyException : Exception
    {
        public PrivateKeyException(string unableToLoadPrivateKey) : base(unableToLoadPrivateKey)
        {
        }
    }
}