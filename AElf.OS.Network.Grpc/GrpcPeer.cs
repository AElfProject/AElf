using System.Threading.Tasks;
using AElf.Kernel;
using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeer
    {
        private readonly Channel _channel;
        private readonly PeerService.PeerServiceClient _client;

        public string PeerAddress { get; private set; }
        public string RemoteEndpoint { get; private set; }

        public GrpcPeer(Channel channel, PeerService.PeerServiceClient client, string peerAddress, string remoteEndpoint)
        {
            _channel = channel;
            _client = client;
            
            RemoteEndpoint = remoteEndpoint;
            PeerAddress = peerAddress;
        }

        public async Task<BlockReply> RequestBlockAsync(BlockRequest blockHash)
        {
            return await _client.RequestBlockAsync(blockHash);
        }

        public async Task AnnounceAsync(Announcement an)
        {
            await _client.AnnounceAsync(an);
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