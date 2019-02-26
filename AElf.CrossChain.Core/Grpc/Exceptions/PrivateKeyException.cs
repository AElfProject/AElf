using System;

namespace AElf.CrossChain.Grpc.Exceptions
{
    public class PrivateKeyException : Exception
    {
        public PrivateKeyException(string unableToLoadPrivateKey) : base(unableToLoadPrivateKey)
        {
        }
    }
}