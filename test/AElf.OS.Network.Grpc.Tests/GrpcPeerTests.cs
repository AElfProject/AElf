using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcPeerTests : GrpcNetworkTestBase
    {
        private IBlockchainService _blockchainService;
        private IAElfNetworkServer _networkServer;
        
        private IPeerPool _pool;
        private GrpcPeer _grpcPeer;
        private GrpcPeer _nonInterceptedPeer;

        public GrpcPeerTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _pool = GetRequiredService<IPeerPool>();

            _grpcPeer = GrpcTestPeerHelpers.CreateNewPeer();
            _grpcPeer.IsConnected = true;

            _nonInterceptedPeer = GrpcTestPeerHelpers.CreateNewPeer("127.0.0.1:2000", false);
            _nonInterceptedPeer.IsConnected = true;

            _pool.TryAddPeer(_grpcPeer);
        }

        public override void Dispose()
        {
            AsyncHelper.RunSync(() => _networkServer.StopAsync(false));
        }

        [Fact]
        public void EnqueueBlock_ShouldExecuteCallback()
        {
            AutoResetEvent executed = new AutoResetEvent(false);
            
            NetworkException exception = null;
            bool called = false;
            _nonInterceptedPeer.EnqueueBlock(new BlockWithTransactions(), ex =>
            {
                exception = ex;
                called = true;
                executed.Set();
            });

            executed.WaitOne(TimeSpan.FromMilliseconds(1000));
            exception.ShouldBeNull();
            called.ShouldBeTrue();
        }
        
        [Fact]
        public void EnqueueTransaction_ShouldExecuteCallback()
        {
            AutoResetEvent executed = new AutoResetEvent(false);
            
            NetworkException exception = null;
            bool called = false;
            _nonInterceptedPeer.EnqueueTransaction(new Transaction(), ex =>
            {
                exception = ex;
                called = true;
                executed.Set();
            });

            executed.WaitOne(TimeSpan.FromMilliseconds(1000));
            exception.ShouldBeNull();
            called.ShouldBeTrue();
        }
        
        [Fact]
        public void EnqueueAnnouncement_ShouldExecuteCallback()
        {
            AutoResetEvent executed = new AutoResetEvent(false);
            
            NetworkException exception = null;
            bool called = false;
            _nonInterceptedPeer.EnqueueAnnouncement(new BlockAnnouncement(), ex =>
            {
                exception = ex;
                called = true;
                executed.Set();
            });

            executed.WaitOne(TimeSpan.FromMilliseconds(1000));
            exception.ShouldBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        public async Task RequestBlockAsync_Success()
        {
            var block = await _grpcPeer.GetBlockByHashAsync(Hash.FromRawBytes(new byte[]{1,2,7}));
            block.ShouldBeNull();

            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            block = await _grpcPeer.GetBlockByHashAsync(blockHeader.GetHash());
            block.ShouldNotBeNull();
        }

        [Fact(Skip = "Improve the logic of this test.")]
        public async Task RequestBlockAsync_Failed()
        {
            _grpcPeer = GrpcTestPeerHelpers.CreateNewPeer("127.0.0.1:3000", false);
            _pool.TryAddPeer(_grpcPeer);

            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            var block = await _grpcPeer.GetBlockByHashAsync(blockHeader.GetHash());
            
            block.ShouldBeNull();
        }

        [Fact]
        public async Task GetBlocksAsync_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            var genesisHash = chain.GenesisBlockHash;

            var blocks = await _grpcPeer.GetBlocksAsync(genesisHash, 5);
            blocks.Count.ShouldBe(5);
            blocks.Select(o => o.Height).ShouldBe(new long[] {2, 3, 4, 5, 6});
        }

        [Fact]
        public void GetRequestMetrics_Test()
        {
            var result = _grpcPeer.GetRequestMetrics();
            
            result.Count.ShouldBe(3);
            result.Keys.ShouldContain("GetBlock");
            result.Keys.ShouldContain("GetBlocks");
            result.Keys.ShouldContain("Announce");
        }

        [Fact]
        public async Task GetNodes_Test()
        {
            var nodeList = await _grpcPeer.GetNodesAsync();
            nodeList.Nodes.Count.ShouldBeGreaterThanOrEqualTo(0);
        }

        public async Task DisconnectAsync_Success()
        {
            var peers = _pool.GetPeers();
            peers.Count.ShouldBe(2);

            await _grpcPeer.DisconnectAsync(true);
            peers = _pool.GetPeers();
            peers.Count.ShouldBe(1);
        }
    }
}