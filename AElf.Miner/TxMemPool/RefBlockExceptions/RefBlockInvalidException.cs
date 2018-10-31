using System;

namespace AElf.Miner.TxMemPool.RefBlockExceptions
{
    public class RefBlockInvalidException : Exception
    {
        public RefBlockInvalidException()
        {
        }

        public RefBlockInvalidException(string message) : base(message)
        {
        }
    }
}