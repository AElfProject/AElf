using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeer : IPeer
    {
        private readonly Channel _channel;
        private readonly PeerService.PeerServiceClient _client;
        private readonly HandshakeData _handshakeData;

        public string PeerAddress { get; }
        public string RemoteEndpoint { get; }

        private byte[] _pubKey;
        public byte[] PublicKey
        {
            get { return _pubKey ?? (_pubKey = _handshakeData?.PublicKey?.ToByteArray()); }
        }

        public GrpcPeer(Channel channel, PeerService.PeerServiceClient client, HandshakeData handshakeData, string peerAddress, string remoteEndpoint)
        {
            _channel = channel;
            _client = client;
            _handshakeData = handshakeData;

            RemoteEndpoint = remoteEndpoint;
            PeerAddress = peerAddress;
        }

        public async Task<Block> RequestBlockAsync(Hash hash, ulong height)
        {
            BlockRequest request = new BlockRequest { Height = height, Id = hash?.Value ?? ByteString.Empty};
            var blockReply = await _client.RequestBlockAsync(request);
            return blockReply?.Block;
        }

        public async Task<List<Hash>> GetBlockIdsAsync(Hash topHash, int count)
        {
            var idList = await _client.RequestBlockIdsAsync(new BlockIdsRequest { FirstBlockId = topHash.Value, Count = count});
            
            if (idList == null || idList.Ids.Count <= 0)
                return new List<Hash>();
            
            return idList.Ids.Select(id => Hash.LoadByteArray(id.ToByteArray())).ToList();
        }

        public async Task AnnounceAsync(BlockHeader header)
        {
            await _client.AnnounceAsync(new Announcement { Header = header});
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
            await _client.DisconnectAsync(new DisconnectReason { Why = DisconnectReason.Types.Reason.Shutdown });
        }
    }
}