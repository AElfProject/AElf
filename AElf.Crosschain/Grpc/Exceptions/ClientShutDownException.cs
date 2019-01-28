using System;

namespace AElf.Crosschain.Exceptions
{
    public class ClientShutDownException : Exception
    {
        public ClientShutDownException(string message) : base(message)
        {
        }
    }
}