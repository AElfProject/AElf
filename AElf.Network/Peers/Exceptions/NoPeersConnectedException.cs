using System;

namespace AElf.Network.Peers.Exceptions
{
    public class NoPeersConnectedException : Exception
    {
        public NoPeersConnectedException(string message) : base(message)
        {
        }
    }
}