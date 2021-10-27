using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Application
{
    public class NetworkServiceTests : OSCoreNetworkServiceTestBase
    {
        private IBlockchainService _blockchainService;
        private readonly INetworkService _networkService;
        private readonly IPeerPool _peerPool;
        private readonly IBlackListedPeerProvider _blackListProvider;

        private readonly KernelTestHelper _kernelTestHelper;

        public NetworkServiceTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _networkService = GetRequiredService<INetworkService>();
            _peerPool = GetRequiredService<IPeerPool>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _blackListProvider = GetRequiredService<IBlackListedPeerProvider>();
        }
        
        [Fact]
        public async Task AddPeer_Test()
        {
            var endpoint = "127.0.0.1:5000";
            var result = await _networkService.AddPeerAsync(endpoint);
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task AddInvalidPeer_Test()
        {
            var endpoint = "ipv6:192.168.197.54";
            var result = await _networkService.AddPeerAsync(endpoint);
            result.ShouldBeFalse();

        }
        [Fact]
        public async Task AddPeer_DotNotRemoveBlackList_Test()
        {
            AElfPeerEndpointHelper.TryParse("127.0.0.1:5000", out var endpoint);
            var host = endpoint.Host;

            _blackListProvider.AddHostToBlackList(host, NetworkConstants.DefaultPeerRemovalSeconds);

            await _networkService.AddPeerAsync(endpoint.ToString());
            
            _blackListProvider.IsIpBlackListed(host).ShouldBeTrue();
        }
        
        [Fact]
        public async Task AddTrustedPeer_Test()
        {
            var endpoint = "127.0.0.1:5000";
            AElfPeerEndpointHelper.TryParse(endpoint, out var aelfPeerEndpoint);
            var host = aelfPeerEndpoint.Host;

            _blackListProvider.AddHostToBlackList(host, 5000);
            
            await _networkService.AddTrustedPeerAsync(endpoint);
            
            _blackListProvider.IsIpBlackListed(host).ShouldBeFalse();
        }
        
        [Fact]
        public async Task AddTrustedInvalidPeer_Test()
        {
            var endpoint = "ipv6:192.168.197.54";
            var result = await _networkService.AddTrustedPeerAsync(endpoint);
            
            result.ShouldBeFalse();
        }
        
        [Fact]
        public async Task RemovePeerByPubkey_Test()
        {
            AElfPeerEndpointHelper.TryParse("192.168.100.200:5000", out var endpoint);
            var host = endpoint.Host;

            //invalid pubkey
            var result = await _networkService.RemovePeerByPubkeyAsync("InvalidPubkey");
            result.ShouldBeFalse();
            
            result = await _networkService.RemovePeerByPubkeyAsync("NormalPeer");
            result.ShouldBeTrue();
            _blackListProvider.IsIpBlackListed("192.168.100.200").ShouldBeTrue();
        }
        
        [Fact]
        public async Task RemovePeerByEndpoint_Test()
        {
            var endpointString = "192.168.100.200:5000";
            AElfPeerEndpointHelper.TryParse(endpointString, out var endpoint);
            var host = endpoint.Host;

            //invalid address
            var result = await _networkService.RemovePeerByEndpointAsync("");
            result.ShouldBeFalse();
            
            result = await _networkService.RemovePeerByEndpointAsync(endpointString);
            result.ShouldBeTrue();
            _blackListProvider.IsIpBlackListed(host).ShouldBeTrue();
        }

        #region GetBlocks

        [Fact]
        public async Task GetBlocks_FromNullPeerOrUnfindable_ThrowsException()
        {
            var exceptionNullPeer = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await _networkService.GetBlocksAsync(HashHelper.ComputeFrom("bHash1"), 1, null));
            
            exceptionNullPeer.Message.ShouldBe("Could not find peer .");

            string peerName = "peer_name";
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await _networkService.GetBlocksAsync(HashHelper.ComputeFrom("bHash1"), 1, peerName));
            
            exception.Message.ShouldBe($"Could not find peer {peerName}.");
        }

        [Fact]
        public async Task GetBlocks_NetworkException_ReturnsNonSuccessfulResponse()
        {
            var response = await _networkService.GetBlocksAsync(HashHelper.ComputeFrom("block_hash"), 1, "FailedPeer");
            response.Success.ShouldBeFalse();
            response.Payload.ShouldBeNull();
        }

        #endregion GetBlocks

        #region GetBlockByHash

        [Fact]
        public async Task GetBlockByHash_UnfindablePeer_ThrowsExceptionNull()
        {
            var exceptionNullPeer = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _networkService.GetBlockByHashAsync(HashHelper.ComputeFrom("bHash1"), null));
            
            exceptionNullPeer.Message.ShouldBe("Could not find peer .");
            
            string peerName = "peer_name";
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _networkService.GetBlockByHashAsync(HashHelper.ComputeFrom("bHash1"), peerName));
            
            exception.Message.ShouldBe($"Could not find peer {peerName}.");
        }

        [Fact]
        public async Task GetBlockByHash_FromSpecifiedPeer_ReturnsBlocks()
        {
            var block = await _networkService.GetBlockByHashAsync(HashHelper.ComputeFrom("bHash1"), "NormalPeer");
            Assert.NotNull(block);
        }

        #endregion GetBlockByHash

        #region GetPeers

        [Fact]
        public void GetPeers_ShouldNotIncludeFailing()
        {
            Assert.Equal(_networkService.GetPeers(false).Count, _peerPool.GetPeers().Count);
        }
        
        [Fact]
        public void GetPeers_ShouldIncludeFailing()
        {
            Assert.Equal(_networkService.GetPeers().Count, _peerPool.GetPeers(true).Count);
        }

        [Fact]
        public void GetPeersByPubKey()
        {
            var pubkey = "NormalPeer";
            var peerInfo = _networkService.GetPeerByPubkey(pubkey);
            peerInfo.ShouldNotBeNull();
            
            var peer = _peerPool.FindPeerByPublicKey(pubkey);
            peerInfo.IpAddress.ShouldBe(peer.RemoteEndpoint.ToString());
            peerInfo.Pubkey.ShouldBe(peer.Info.Pubkey);
            peerInfo.LastKnownLibHeight.ShouldBe(peer.LastKnownLibHeight);
            peerInfo.ProtocolVersion.ShouldBe(peer.Info.ProtocolVersion);
            peerInfo.ConnectionTime.ShouldBe(peer.Info.ConnectionTime.Seconds);
            peerInfo.ConnectionStatus.ShouldBe(peer.ConnectionStatus);
            peerInfo.Inbound.ShouldBe(peer.Info.IsInbound);
            peerInfo.SyncState.ShouldBe(peer.SyncState);
            peerInfo.BufferedAnnouncementsCount.ShouldBe(peer.BufferedAnnouncementsCount);
            peerInfo.BufferedBlocksCount.ShouldBe(peer.BufferedBlocksCount);
            peerInfo.BufferedTransactionsCount.ShouldBe(peer.BufferedTransactionsCount);

            var fakePubkey = Cryptography.CryptoHelper.GenerateKeyPair().PublicKey.ToHex();
            peerInfo = _networkService.GetPeerByPubkey(fakePubkey);
            peerInfo.ShouldBeNull();
        }

        [Fact]
        public void IsPeerPoolFull_Test()
        {
            var peerPoolIsFull = _peerPool.IsFull();
            _networkService.IsPeerPoolFull().ShouldBe(peerPoolIsFull);
        }
        
        #endregion GetPeers

        [Fact]
        public async Task CheckPeersHealth_Test()
        {
            var failedPeerPubkey = "FailedPeer";
            var peer = _peerPool.FindPeerByPublicKey(failedPeerPubkey);
            peer.ShouldNotBeNull();

            await _networkService.CheckPeersHealthAsync();
            
            peer = _peerPool.FindPeerByPublicKey(failedPeerPubkey);
            peer.ShouldBeNull();
        }
    }
}