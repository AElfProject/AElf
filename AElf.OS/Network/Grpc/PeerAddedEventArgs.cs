using System;
using AElf.OS.Network.Grpc.Generated;

namespace AElf.OS.Network.Grpc
{
    internal class PeerAddedEventArgs : EventArgs
    {
        public PeerService.PeerServiceClient Client;
    }
}