using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using Google.Protobuf;
using Grpc.Core;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeer : IPeer
    {
        private readonly Channel _channel;
        private readonly PeerService.PeerServiceClient _client;
        private readonly HandshakeData _handshakeData;

        public Hash CurrentBlockHash { get; set; }
        public long CurrentBlockHeight { get; set; }
        public string PeerAddress { get; }
        public string RemoteEndpoint { get; }

        private byte[] _pubKey;
        public byte[] PublicKey
        {
            get { return _pubKey ?? (_pubKey = _handshakeData?.PublicKey?.ToByteArray()); }
        }

        public GrpcPeer(Channel channel, PeerService.PeerServiceClient client, HandshakeData handshakeData,
            string peerAddress, string remoteEndpoint)
        {
            _channel = channel;
            _client = client;
            _handshakeData = handshakeData;

            RemoteEndpoint = remoteEndpoint;
            PeerAddress = peerAddress;
        }

        public async Task<Block> RequestBlockAsync(Hash hash)
        {
            BlockRequest request = new BlockRequest {Hash = hash};
            var blockReply = await _client.RequestBlockAsync(request);
            return blockReply?.Block;
        }

        public async Task<List<Block>> GetBlocksAsync(Hash firstHash, int count)
        {
            var list = await _client.RequestBlocksAsync(new BlocksRequest {PreviousBlockHash = firstHash, Count = count});

            if (list == null)
                return new List<Block>();

            return list.Blocks.Select(b => b).ToList();
        }

        public async Task AnnounceAsync(PeerNewBlockAnnouncement header)
        {
            await _client.AnnounceAsync(header);
        }

        public async Task SendTransactionAsync(Transaction tx)
        {
            await _client.SendTransactionAsync(tx);
        }

        public async Task StopAsync()
        {
            await _channel.ShutdownAsync();
        }

        public async Task SendDisconnectAsync()
        {
            await _client.DisconnectAsync(new DisconnectReason {Why = DisconnectReason.Types.Reason.Shutdown});
        }
    }
}