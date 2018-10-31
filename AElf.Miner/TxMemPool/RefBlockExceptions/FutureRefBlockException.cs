using System;

namespace AElf.Miner.TxMemPool.RefBlockExceptions
{
    public class FutureRefBlockException : Exception
    {
        public FutureRefBlockException()
        {
        }

        public FutureRefBlockException(string message) : base(message)
        {
        }
    }
}