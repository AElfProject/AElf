using System;

namespace AElf.Kernel.TransactionPool.RefBlockExceptions
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