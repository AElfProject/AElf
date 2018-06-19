using System;

namespace AElf.Kernel.Node.Protocol.Exceptions
{
    public class InvalidBlockException : Exception
    {
        public InvalidBlockException(string message) : base(message)
        {
        }
    }
}