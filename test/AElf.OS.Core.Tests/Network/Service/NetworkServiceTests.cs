using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class NetworkServiceTests : OSCoreNetworkServiceTestBase
    {
        private readonly INetworkService _networkService;
        private readonly IPeerPool _peerPool;
        private readonly IBlackListedPeerProvider _blackListProvider;

        private readonly KernelTestHelper _kernelTestHelper;

        public NetworkServiceTests()
        {
            _networkService = GetRequiredService<INetworkService>();
            _peerPool = GetRequiredService<IPeerPool>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _blackListProvider = GetRequiredService<IBlackListedPeerProvider>();
        }

        #region Blacklisting

        [Fact]
        public async Task RemovePeerByPubkeyAsync_BlackListTest()
        {
            var peerPubKey = "blacklistpeer";
            AElfPeerEndpointHelper.TryParse("127.0.0.1:5000", out var endpoint);
            var host = endpoint.Host;

            //invalid address
            var result = await _networkService.RemovePeerAsync(peerPubKey);
            result.ShouldBeFalse();
            
            await _networkService.RemovePeerByPubkeyAsync(peerPubKey);
            _blackListProvider.IsIpBlackListed(host).ShouldBeFalse();
            
            await _networkService.RemovePeerByPubkeyAsync(peerPubKey, true);
            _blackListProvider.IsIpBlackListed(host).ShouldBeTrue();
        }

        [Fact]
        public async Task AddInvalidPeer_Test()
        {
            var endpoint = "ipv6:192.168.197.54";
            var result = await _networkService.AddPeerAsync(endpoint);
            result.ShouldBeFalse();

        }
        [Fact]
        public async Task AddPeerAsync_CannotAddBlacklistedPeer()
        {
            AElfPeerEndpointHelper.TryParse("127.0.0.1:5000", out var endpoint);
            var host = endpoint.Host;

            _blackListProvider.AddHostToBlackList(host);
            
            (await _networkService.AddPeerAsync(endpoint.ToString())).ShouldBeFalse();
        }

        #endregion Blacklisting

        #region GetBlocks

        [Fact]
        public async Task GetBlocks_FromNullPeerOrUnfindable_ThrowsException()
        {
            var exceptionNullPeer = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await _networkService.GetBlocksAsync(Hash.FromString("bHash1"), 1, null));
            
            exceptionNullPeer.Message.ShouldBe("Could not find peer .");

            string peerName = "peer_name";
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await _networkService.GetBlocksAsync(Hash.FromString("bHash1"), 1, peerName));
            
            exception.Message.ShouldBe($"Could not find peer {peerName}.");
        }

        [Fact]
        public async Task GetBlocks_NetworkException_ReturnsNonSuccessfulResponse()
        {
            var response = await _networkService.GetBlocksAsync(Hash.FromString("block_hash"), 1, "failed_peer");
            response.Success.ShouldBeFalse();
            response.Payload.ShouldBeNull();
        }

        #endregion GetBlocks

        #region GetBlockByHash

        [Fact]
        public async Task GetBlockByHash_UnfindablePeer_ThrowsExceptionNull()
        {
            var exceptionNullPeer = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _networkService.GetBlockByHashAsync(Hash.FromString("bHash1"), null));
            
            exceptionNullPeer.Message.ShouldBe("Could not find peer .");
            
            string peerName = "peer_name";
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _networkService.GetBlockByHashAsync(Hash.FromString("bHash1"), peerName));
            
            exception.Message.ShouldBe($"Could not find peer {peerName}.");
        }

        [Fact]
        public async Task GetBlockByHash_FromSpecifiedPeer_ReturnsBlocks()
        {
            var block = await _networkService.GetBlockByHashAsync(Hash.FromString("bHash1"), "p1");
            Assert.NotNull(block);
        }

        #endregion GetBlockByHash

        #region GetPeers

        [Fact]
        public void GetPeers_ShouldIncludeFailing()
        {
            Assert.Equal(_networkService.GetPeers().Count, _peerPool.GetPeers(true).Count);
        }

        [Fact]
        public void GetPeersByPubKey()
        {
            var peer = _networkService.GetPeerByPubkey("p1");
            peer.ShouldNotBeNull();

            var fakePubkey = Cryptography.CryptoHelper.GenerateKeyPair().PublicKey.ToHex();
            peer = _networkService.GetPeerByPubkey(fakePubkey);
            peer.ShouldBeNull();
        }

        #endregion GetPeers

        #region Broadcast

        [Fact]
        public async Task BroadcastAnnounce_Test()
        {
            var blockHeader = _kernelTestHelper.GenerateBlock(10, Hash.FromString("test")).Header;

            //old block
            blockHeader.Time = TimestampHelper.GetUtcNow() - TimestampHelper.DurationFromMinutes(20);
            await _networkService.BroadcastAnnounceAsync(blockHeader, false);
            
            //known block
            blockHeader.Time = TimestampHelper.GetUtcNow();
            await _networkService.BroadcastAnnounceAsync(blockHeader, false);

            //broadcast again
            blockHeader = _kernelTestHelper.GenerateBlock(11, Hash.FromString("new")).Header;
            await _networkService.BroadcastAnnounceAsync(blockHeader, false);
        }

        [Fact]
        public void BroadcastBlockWithTransactionsAsync_Test()
        {
            var blockWithTransaction = new BlockWithTransactions
            {
                Header = _kernelTestHelper.GenerateBlock(10, Hash.FromString("test")).Header,
               Transactions =
               {
                   new Transaction(),
                   new Transaction()
               }
            };
            var result = _networkService.BroadcastBlockWithTransactionsAsync(blockWithTransaction);
            result.Status.ShouldBe(TaskStatus.RanToCompletion);
        }
        #endregion
    }
}