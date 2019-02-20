using System;

namespace AElf.Kernel.TransactionPool.RefBlockExceptions
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