using System;

namespace AElf.Kernel.Exceptions
{
    public class DataNotFoundException : ApplicationException
    {
        public DataNotFoundException(string message)
            : base(message)
        {
        }

        public DataNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}