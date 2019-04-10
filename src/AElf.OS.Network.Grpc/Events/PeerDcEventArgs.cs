using System;

namespace AElf.OS.Network.Grpc.Events
{
    public class PeerDcEventArgs : EventArgs
    {
        public string Peer { get; set; }
    }
}