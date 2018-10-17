using System;

namespace AElf.Miner.Rpc.Exceptions
{
    public class ClientShutDownException : Exception
    {
        public ClientShutDownException(string message) : base(message)
        {
        }
    }
}