using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcPeerTests : GrpcNetworkTestBase
    {
        private OSTestHelper _osTestHelper;
        
        private IBlockchainService _blockchainService;
        private IAElfNetworkServer _networkServer;
        private ILocalEventBus _eventBus;
        
        private IPeerPool _pool;
        private GrpcPeer _grpcPeer;

        public GrpcPeerTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _pool = GetRequiredService<IPeerPool>();

            _grpcPeer = GrpcTestPeerHelpers.CreateNewPeer();
            _pool.TryAddPeer(_grpcPeer);
        }

        public override void Dispose()
        {
            AsyncHelper.RunSync(() => _networkServer.StopAsync(false));
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

        // TODO
        [Fact(Skip = "Either test client side logic or server side with corresponding mocks. Not both here.")]
        public async Task AnnounceAsync_Success()
        {
            AnnouncementReceivedEventData received = null;
            _eventBus.Subscribe<AnnouncementReceivedEventData>(a =>
            {
                received = a;
                return Task.CompletedTask;
            });

            var header = new BlockAnnouncement
            {
                BlockHeight = 100,
                BlockHash = Hash.FromRawBytes(new byte[]{9,2})
            };

            await _grpcPeer.SendAnnouncementAsync(header);

            received.ShouldNotBeNull();
            received.Announce.BlockHeight.ShouldBe(100);
        }

        // TODO
        [Fact(Skip = "Either test client side logic or server side with corresponding mocks. Not both here.")]
        public async Task SendTransactionAsync_Success()
        {
            TransactionsReceivedEvent received = null;
            _eventBus.Subscribe<TransactionsReceivedEvent>(t =>
            {
                received = t;
                return Task.CompletedTask;
            });
            var transactions = await _osTestHelper.GenerateTransferTransactions(1);
            await _grpcPeer.SendTransactionAsync(transactions.First());

            await Task.Delay(200);
            received.ShouldNotBeNull();
            received.Transactions.Count().ShouldBe(1);
            received.Transactions.First().From.ShouldBe(transactions.First().From);
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