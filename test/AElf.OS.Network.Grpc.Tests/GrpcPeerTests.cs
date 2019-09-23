using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Grpc.Core;
using Grpc.Core.Testing;
using Moq;
using NSubstitute;
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

            //_nonInterceptedPeer = GrpcTestPeerHelpers.CreateNewPeer("127.0.0.1:2000", false);
            //_nonInterceptedPeer.IsConnected = true;
            _nonInterceptedPeer = MockServiceClient("127.0.0.1:2000");

            _pool.TryAddPeer(_grpcPeer);
        }

        public override void Dispose()
        {
            AsyncHelper.RunSync(() => _networkServer.StopAsync(false));
        }

        [Fact]
        public void EnqueueBlock_ShouldExecuteCallback_Test()
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

            executed.WaitOne();
            exception.ShouldBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        public void EnqueueTransaction_ShouldExecuteCallback_Test()
        {
            AutoResetEvent executed = new AutoResetEvent(false);

            NetworkException exception = null;
            var transaction = new Transaction();
            bool called = false;
            _nonInterceptedPeer.EnqueueTransaction(transaction, ex =>
            {
                exception = ex;
                called = true;
                executed.Set();
            });

            executed.WaitOne();
            exception.ShouldBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        public void EnqueueAnnouncement_ShouldExecuteCallback_Test()
        {
            AutoResetEvent executed = new AutoResetEvent(false);

            NetworkException exception = null;
            var called = false;
            _nonInterceptedPeer.EnqueueAnnouncement(new BlockAnnouncement(), ex =>
            {
                exception = ex;
                called = true;
                executed.Set();
            });

            executed.WaitOne();
            exception.ShouldBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        public void EnqueueAnnouncement_WithPeerNotReady_Test()
        {
            AutoResetEvent executed = new AutoResetEvent(false);

            NetworkException exception = null;
            bool called = false;
            _grpcPeer.IsConnected = false;
            Should.Throw<NetworkException>(() =>
                _grpcPeer.EnqueueAnnouncement(new BlockAnnouncement(), ex =>
                {
                    exception = ex;
                    called = true;
                    executed.Set();
                }));

            Should.Throw<NetworkException>(()=>
                _grpcPeer.EnqueueBlock(new BlockWithTransactions(), ex =>
                {
                    exception = ex;
                    called = true;
                    executed.Set();
                }));
        }

        [Fact]
        public async Task RequestBlockAsync_Success_Test()
        {
            var block = await _grpcPeer.GetBlockByHashAsync(Hash.FromRawBytes(new byte[] {1, 2, 7}));
            block.ShouldBeNull();

            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            block = await _grpcPeer.GetBlockByHashAsync(blockHeader.GetHash());
            block.ShouldNotBeNull();
        }

        [Fact]
        public async Task GetBlocksAsync_Success_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var genesisHash = chain.GenesisBlockHash;

            var blocks = await _grpcPeer.GetBlocksAsync(genesisHash, 5);
            blocks.Count.ShouldBe(5);
            blocks.Select(o => o.Height).ShouldBe(new long[] {2, 3, 4, 5, 6});

            var blockHash = Hash.Empty;
            blocks = await _grpcPeer.GetBlocksAsync(blockHash, 1);
            blocks.ShouldBe(new List<BlockWithTransactions>());
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

        [Fact]
        public async Task DisconnectAsync_Success_Test()
        {
            var peers = _pool.GetPeers(true);
            peers.Count.ShouldBe(2);

            await _grpcPeer.DisconnectAsync(true);
            peers = _pool.GetPeers(true);
            peers.Count.ShouldBe(1);
        }
        
        [Fact]
        public async Task DisconnectAsync_Test()
        {
            var isReady = _grpcPeer.IsReady;
            isReady.ShouldBeTrue();
            
            await _grpcPeer.DisconnectAsync(true);
            
            isReady = _grpcPeer.IsReady;
            isReady.ShouldBeFalse();
        }

        [Fact]
        public void Peer_AddKnowBlock_Test()
        {
            //fork clean hash
            var announcement = new BlockAnnouncement
            {
                HasFork = true
            };
            _grpcPeer.AddKnowBlock(announcement);
            _grpcPeer.RecentBlockHeightAndHashMappings.Count().ShouldBe(0);

            for (var i = 0; i < 10; i++)
            {
                _grpcPeer.AddKnowBlock(new BlockAnnouncement
                {
                    BlockHash = Hash.FromString($"height-{i+1}"),
                    BlockHeight = i+1,
                    HasFork = false
                });
                _grpcPeer.RecentBlockHeightAndHashMappings.Count().ShouldBe(i+1);
            }
            
            //over max value
            _grpcPeer.AddKnowBlock(new BlockAnnouncement
            {
                BlockHash = Hash.FromString($"random-hash"),
                BlockHeight = 20,
                HasFork = false
            });
            _grpcPeer.RecentBlockHeightAndHashMappings.Count().ShouldBe(10);
        }

        private GrpcPeer MockServiceClient(string ipAddress)
        {
            var mockClient = new Mock<PeerService.PeerServiceClient>();
            var testCompletionSource = Task.FromResult(new VoidReply());

            // setup mock announcement stream
            var announcementStreamCall = MockStreamCall<BlockAnnouncement, VoidReply>(testCompletionSource);
            mockClient.Setup(m => m.AnnouncementBroadcastStream(null, null, CancellationToken.None))
                .Returns(announcementStreamCall);
            
            // setup mock transaction stream
            var transactionStreamCall = MockStreamCall<Transaction, VoidReply>(testCompletionSource);
            mockClient.Setup(m => m.TransactionBroadcastStream(null, null, CancellationToken.None))
                .Returns(transactionStreamCall);
            
            // setup mock block stream
            var blockStreamCall = MockStreamCall<BlockWithTransactions, VoidReply>(testCompletionSource);
            mockClient.Setup(m => m.BlockBroadcastStream(null, null, CancellationToken.None))
                .Returns(blockStreamCall);
            
            // create peer
            var grpcPeer = GrpcTestPeerHelpers.CreatePeerWithClient(ipAddress, 
                NetworkTestConstants.FakePubkey, mockClient.Object);
            
            grpcPeer.IsConnected = true;

            return grpcPeer;
        }
        
        private AsyncClientStreamingCall<TReq, TResp> MockStreamCall<TReq, TResp>(Task<TResp> replyTask) where TResp : new()
        {
            var mockRequestStream = new Mock<IClientStreamWriter<TReq>>();
            mockRequestStream.Setup(m => m.WriteAsync(It.IsAny<TReq>()))
                .Returns(replyTask);
            
            var call = TestCalls.AsyncClientStreamingCall(mockRequestStream.Object, Task.FromResult(new TResp()),
                Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });

            return call;
        }
    }
}