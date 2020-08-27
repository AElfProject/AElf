using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Protocol;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Grpc
{
    public class PeerDialerTests : PeerDialerTestBase
    {
        private readonly IPeerDialer _peerDialer;
        private readonly IBlockchainService _blockchainService;
        private readonly IHandshakeProvider _handshakeProvider;

        public PeerDialerTests()
        {
            _peerDialer = GetRequiredService<IPeerDialer>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _handshakeProvider = GetRequiredService<IHandshakeProvider>();
        }

        [Fact]
        public async Task DialPeer_Test()
        {
            AElfPeerEndpointHelper.TryParse("127.0.0.1:2000", out var endpoint);
            var grpcPeer = await _peerDialer.DialPeerAsync(endpoint);
            
            grpcPeer.ShouldNotBeNull();

            grpcPeer.CurrentBlockHash.ShouldBe(HashHelper.ComputeFrom("BestChainHash"));
            grpcPeer.CurrentBlockHeight.ShouldBe(10);
            grpcPeer.LastKnownLibHeight.ShouldBe(1);
        }
        
        [Fact]
        public async Task DialPeer_InvalidEndpoint_Test()
        {
            AElfPeerEndpointHelper.TryParse("127.0.0.1:2001", out var endpoint);
            var grpcPeer = await _peerDialer.DialPeerAsync(endpoint);
            
            grpcPeer.ShouldBeNull();
        }

        [Fact]
        public async Task DialBackPeer_Test()
        {
            AElfPeerEndpointHelper.TryParse("127.0.0.1:2000", out var endpoint);
            var handshake = await _handshakeProvider.GetHandshakeAsync();
            
            var grpcPeer = await _peerDialer.DialBackPeerAsync(endpoint, handshake);
            
            grpcPeer.ShouldNotBeNull();
            grpcPeer.CurrentBlockHash.ShouldBe(handshake.HandshakeData.BestChainHash);
            grpcPeer.CurrentBlockHeight.ShouldBe(handshake.HandshakeData.BestChainHeight);
            grpcPeer.LastKnownLibHeight.ShouldBe(handshake.HandshakeData.LastIrreversibleBlockHeight);
            grpcPeer.Info.Pubkey.ShouldBe(handshake.HandshakeData.Pubkey.ToHex());
            grpcPeer.Info.ProtocolVersion.ShouldBe(handshake.HandshakeData.Version);
        }
        
        [Fact]
        public async Task DialBackPeer_InvalidEndpoint_Test()
        {
            AElfPeerEndpointHelper.TryParse("127.0.0.1:2001", out var endpoint);
            var handshake = await _handshakeProvider.GetHandshakeAsync();
            
            var grpcPeer = await _peerDialer.DialBackPeerAsync(endpoint, handshake);
            
            grpcPeer.ShouldBeNull();
        }
    }
}