using System;

namespace AElf.Miner.Rpc.Exceptions
{
    public class ChainInfoNotFoundException : Exception
    {
        public ChainInfoNotFoundException(string unableToGetChainInfo) : base(unableToGetChainInfo)
        {
        }
    }
}