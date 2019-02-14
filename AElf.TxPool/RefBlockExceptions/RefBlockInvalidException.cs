using System;

namespace AElf.TxPool.RefBlockExceptions
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