using System;

namespace AElf.OS.Network.Grpc
{
    public class PeerDialException : Exception
    {
        public PeerDialException()
        {
        }

        public PeerDialException(string message)
            : base(message)
        {
        }

        public PeerDialException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}