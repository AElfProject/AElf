using System;

namespace AElf.TxPool.RefBlockExceptions
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