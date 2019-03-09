using System;

namespace AElf.CrossChain.Grpc.Exceptions
{
    public class ClientShutDownException : Exception
    {
        public ClientShutDownException(string message) : base(message)
        {
        }
    }
}