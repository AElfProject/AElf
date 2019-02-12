using System;

namespace AElf.Crosschain.Exceptions
{
    public class ChainInfoNotFoundException : Exception
    {
        public ChainInfoNotFoundException(string unableToGetChainInfo) : base(unableToGetChainInfo)
        {
        }
    }
}