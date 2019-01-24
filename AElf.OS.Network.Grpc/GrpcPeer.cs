using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeer
    {
        private readonly Channel _channel;
        private readonly PeerService.PeerServiceClient _client;
        
        public string PeerAddress { get; private set; }

        public GrpcPeer(Channel channel, PeerService.PeerServiceClient client, string peerAddress)
        {
            _channel = channel;
            _client = client;

            PeerAddress = peerAddress;
        }

        public async Task<BlockReply> RequestBlockAsync(BlockRequest blockHash)
        {
            return await _client.RequestBlockAsync(blockHash);
        }

        public async Task AnnounceAsync(Announcement an)
        {
            throw new NotImplementedException();
        }
    }
}