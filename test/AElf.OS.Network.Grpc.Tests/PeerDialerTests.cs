using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Protocol;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class PeerDialerTests : PeerDialerTestBase
    {
        private readonly IPeerDialer _peerDialer;
        private readonly IBlockchainService _blockchainService;
        private readonly IHandshakeProvider _handshakeProvider;
        private readonly NetworkTestContext _networkTestContext;

        public PeerDialerTests()
        {
            _peerDialer = GetRequiredService<IPeerDialer>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _handshakeProvider = GetRequiredService<IHandshakeProvider>();
            _networkTestContext = GetRequiredService<NetworkTestContext>();
        }

        [Fact]
        public async Task DialPeer_Test()
        {
            var endpoint = IpEndPointHelper.Parse("127.0.0.1:2000");
            var grpcPeer = await _peerDialer.DialPeerAsync(endpoint);
            
            grpcPeer.ShouldNotBeNull();

            var peersHandshake = _networkTestContext.GeneratedHandshakes[endpoint.Address.ToString()];
            grpcPeer.CurrentBlockHash.ShouldBe(peersHandshake.HandshakeData.BestChainHash);
            grpcPeer.CurrentBlockHeight.ShouldBe(peersHandshake.HandshakeData.BestChainHeight);
            grpcPeer.LastKnownLibHeight.ShouldBe(peersHandshake.HandshakeData.LastIrreversibleBlockHeight);
        }

        [Fact]
        public async Task DialBackPeer_Test()
        {
            var endpoint = IpEndPointHelper.Parse("127.0.0.1:2000");
            var handshake = await _handshakeProvider.GetHandshakeAsync();
            
            var grpcPeer = await _peerDialer.DialBackPeerAsync(endpoint, handshake);
            
            grpcPeer.ShouldNotBeNull();
            grpcPeer.CurrentBlockHash.ShouldBe(handshake.HandshakeData.BestChainHash);
            grpcPeer.CurrentBlockHeight.ShouldBe(handshake.HandshakeData.BestChainHeight);
            grpcPeer.LastKnownLibHeight.ShouldBe(handshake.HandshakeData.LastIrreversibleBlockHeight);
            grpcPeer.Info.Pubkey.ShouldBe(handshake.HandshakeData.Pubkey.ToHex());
            grpcPeer.Info.ProtocolVersion.ShouldBe(handshake.HandshakeData.Version);
        }
    }
}