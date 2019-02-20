using System;

namespace AElf.Kernel.TransactionPool.RefBlockExceptions
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