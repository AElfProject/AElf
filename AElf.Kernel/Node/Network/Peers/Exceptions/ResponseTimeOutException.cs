using System;

namespace AElf.Kernel.Node.Network.Peers.Exceptions
{
    public class ResponseTimeOutException : Exception
    {
        public const string ConnectionTimeout = "The distant peer took too long to respond, the connection was closed.";
            
        public ResponseTimeOutException(Exception inner) : base(ConnectionTimeout, inner) { }
    }
}