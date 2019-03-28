using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Grpc.Core.Testing;
using Grpc.Core.Utils;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcServerServiceTests : GrpcNetworkTestBase
    {
        public GrpcServerServiceTests()
        {
            _service = GetRequiredService<PeerService.PeerServiceBase>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _peerPool = GetRequiredService<IPeerPool>();
            _eventBus = GetRequiredService<ILocalEventBus>();
        }

        private readonly PeerService.PeerServiceBase _service;
        private readonly IBlockchainService _blockchainService;
        private readonly IPeerPool _peerPool;
        private readonly ILocalEventBus _eventBus;

        private ServerCallContext BuildServerCallContext(Metadata metadata = null)
        {
            var meta = metadata ?? new Metadata();
            return TestServerCallContext.Create("mock", null, DateTime.UtcNow.AddHours(1), meta, CancellationToken.None,
                "127.0.0.1", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(), writeOptions => { });
        }

        [Fact]
        public async Task Announce_ShouldPublishEvent()
        {
            AnnouncementReceivedEventData received = null;
            _eventBus.Subscribe<AnnouncementReceivedEventData>(a =>
            {
                received = a;
                return Task.CompletedTask;
            });

            var hash = Hash.Generate();
            await _service.Announce(new PeerNewBlockAnnouncement {BlockHeight = 10, BlockHash = hash},
                BuildServerCallContext());

            Assert.NotNull(received);
            Assert.Equal(10, received.Announce.BlockHeight);
            Assert.Equal(received.Announce.BlockHash, hash);
        }

        [Fact]
        public async Task Disconnect_ShouldRemovePeer()
        {
            await _service.Disconnect(new DisconnectReason(),
                BuildServerCallContext(new Metadata
                {
                    {GrpcConsts.PubkeyMetadataKey, "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399"}
                }));
            Assert.Empty(_peerPool.GetPeers(true));
        }

        [Fact]
        public async Task RequestBlock_NoHash_ReturnsEmpty()
        {
            var reply = await _service.RequestBlock(new BlockRequest(), BuildServerCallContext());

            Assert.NotNull(reply);
            Assert.Null(reply.Block);
        }

        [Fact]
        public async Task RequestBlock_NonExistant_ReturnsEmpty()
        {
            var reply = await _service.RequestBlock(new BlockRequest {Hash = Hash.Generate()},
                BuildServerCallContext());

            Assert.NotNull(reply);
            Assert.Null(reply.Block);
        }

        [Fact]
        public async Task RequestBlock_Random_ReturnsBlock()
        {
            var reqBlockCtxt = BuildServerCallContext();
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _service.RequestBlock(new BlockRequest {Hash = chain.LongestChainHash}, reqBlockCtxt);

            Assert.NotNull(reply.Block);
            Assert.True(reply.Block.GetHash() == chain.LongestChainHash);
        }

        [Fact]
        public async Task RequestBlocks_FromGenesis_ReturnsBlocks()
        {
            var reqBlockCtxt = BuildServerCallContext();
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _service.RequestBlocks(
                new BlocksRequest {PreviousBlockHash = chain.GenesisBlockHash, Count = 5}, reqBlockCtxt);

            Assert.True(reply.Blocks.Count == 5);
        }

        [Fact]
        public async Task RequestBlocks_NoHash_ReturnsEmpty()
        {
            var reply = await _service.RequestBlocks(new BlocksRequest(), BuildServerCallContext());

            Assert.NotNull(reply?.Blocks);
            Assert.Empty(reply.Blocks);
        }

        [Fact]
        public async Task RequestBlocks_NonExistant_ReturnsEmpty()
        {
            var reply = await _service.RequestBlocks(new BlocksRequest {PreviousBlockHash = Hash.Generate(), Count = 5},
                BuildServerCallContext());

            Assert.NotNull(reply?.Blocks);
            Assert.Empty(reply.Blocks);
        }

        [Fact]
        public async Task SendTx_ShouldPublishEvent()
        {
            TransactionsReceivedEvent received = null;
            _eventBus.Subscribe<TransactionsReceivedEvent>(t =>
            {
                received = t;
                return Task.CompletedTask;
            });

            var tx = new Transaction();
            tx.From = Address.Generate();
            tx.To = Address.Generate();

            await _service.SendTransaction(tx, BuildServerCallContext());

            Assert.NotNull(received?.Transactions);
            Assert.Equal(1, received.Transactions.Count());
            Assert.Equal(received.Transactions.First().From, tx.From);
        }
    }
}