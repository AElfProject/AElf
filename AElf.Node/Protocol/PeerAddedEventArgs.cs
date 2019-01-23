using System;

namespace AElf.Node.Protocol
{
    public class PeerAddedEventArgs : EventArgs
    {
        public Protobuf.Generated.PeerService.PeerServiceClient Client;
    }
}