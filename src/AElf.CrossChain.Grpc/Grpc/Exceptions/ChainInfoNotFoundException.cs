using System;

namespace AElf.CrossChain.Grpc.Exceptions
{
    public class ChainInfoNotFoundException : Exception
    {
        public ChainInfoNotFoundException(string unableToGetChainInfo) : base(unableToGetChainInfo)
        {
        }
    }
}