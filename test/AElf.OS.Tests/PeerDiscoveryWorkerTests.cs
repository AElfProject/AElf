using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using AElf.OS.Worker;
using Grpc.Core;
using Shouldly;
using Xunit;

namespace AElf.OS
{
    public class PeerDiscoveryWorkerTests : OSTestBase
    {
        private readonly PeerDiscoveryWorker _peerDiscoveryWorker;
        private INetworkService _networkService;
        private IPeerPool _peerPool;

        public PeerDiscoveryWorkerTests()
        {
            _peerDiscoveryWorker = GetRequiredService<PeerDiscoveryWorker>();
            _networkService = GetRequiredService<INetworkService>();
            _peerPool = GetRequiredService<IPeerPool>();
        }

        [Fact]
        public async Task ProcessPeerDiscoveryJob_Test()
        {
            _peerDiscoveryWorker.ShouldNotBeNull();

            var beforePeers = _networkService.GetPeers().Count;

            await _peerDiscoveryWorker.ProcessPeerDiscoveryJobAsync();

            var afterDiscoveryPeers = _networkService.GetPeers().Count;
            beforePeers.ShouldBe(afterDiscoveryPeers);
        }

        private GrpcPeer CreateNewPeer()
        {
            var pubkey = "048f5ced21f8d687cb9ade1c22dc0e183b05f87124c82073f5d82a09b139cc466efbfb6f28494d0a9d7366fcb769fe5436cfb7b5d322a2b0f69c4bcb1c33ac24ad";
            var ipAddress = "127.0.0.1:888";
            AElfPeerEndpointHelper.TryParse(ipAddress, out var remoteEndpoint);
            var channel = new Channel(ipAddress, ChannelCredentials.Insecure);
            var client = new PeerService.PeerServiceClient(channel);

            var connectionInfo = new PeerConnectionInfo
            {
                Pubkey = pubkey,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow(),
                IsInbound = true
            };

            return new GrpcPeer(new GrpcClient(channel, client), remoteEndpoint, connectionInfo);
        }
    }
}