using System;

namespace AElf.TxPool.RefBlockExceptions
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