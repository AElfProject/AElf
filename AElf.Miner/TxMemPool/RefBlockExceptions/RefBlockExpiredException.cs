using System;

namespace AElf.Miner.TxMemPool.RefBlockExceptions
{
    public class RefBlockExpiredException : Exception
    {
        public RefBlockExpiredException()
        {
        }

        public RefBlockExpiredException(string message) : base(message)
        {
        }
    }
}