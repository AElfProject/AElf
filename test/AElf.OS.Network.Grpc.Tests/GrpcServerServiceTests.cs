using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Helper;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using AElf.Types;
using Grpc.Core;
using Grpc.Core.Testing;
using Grpc.Core.Utils;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcServerServiceTests : GrpcNetworkTestBase
    {
        private readonly IAElfNetworkServer _networkServer;
        private readonly IBlockchainService _blockchainService;
        private readonly IPeerPool _peerPool;
        private readonly ILocalEventBus _eventBus;
        
        private readonly GrpcServerService _service;

        public GrpcServerServiceTests()
        {
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _service = GetRequiredService<GrpcServerService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _peerPool = GetRequiredService<IPeerPool>();
            _eventBus = GetRequiredService<ILocalEventBus>();
        }

        private ServerCallContext BuildServerCallContext(Metadata metadata = null, string address = null)
        {
            return TestServerCallContext.Create("mock", null, TimestampHelper.GetUtcNow().AddHours(1).ToDateTime(), metadata ?? new Metadata(), CancellationToken.None, 
                address ?? "127.0.0.1", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(), writeOptions => { });
        }

        #region Announce and transaction

        [Fact]
        public async Task Announce_ShouldPublishEvent()
        {
            AnnouncementReceivedEventData received = null;
            _eventBus.Subscribe<AnnouncementReceivedEventData>(a =>
            {
                received = a;
                return Task.CompletedTask;
            });

            Hash hash = Hash.FromRawBytes(new byte[]{3,6,9});
            await _service.SendAnnouncement(new BlockAnnouncement
            {
                BlockHeight = 10, BlockHash = hash
            }, BuildServerCallContext());

            Assert.NotNull(received);
            Assert.Equal(10, received.Announce.BlockHeight);
            Assert.Equal(received.Announce.BlockHash, hash);
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
            
            Transaction tx = new Transaction();
            tx.From = SampleAddress.AddressList[0];
            tx.To = SampleAddress.AddressList[1];
            
            await _service.SendTransaction(tx, BuildServerCallContext());
            
            received?.Transactions.ShouldNotBeNull();
            received.Transactions.Count().ShouldBe(1);
            received.Transactions.First().From.ShouldBe(tx.From);
        }
        
        [Fact]
        public async Task SendTx_WithHighTxRef_ShouldNotPublishEvent()
        {
            TransactionsReceivedEvent received = null;
            _eventBus.Subscribe<TransactionsReceivedEvent>(t =>
            {
                received = t;
                return Task.CompletedTask;
            });
            
            Transaction tx = new Transaction();
            tx.From = SampleAddress.AddressList[0];
            tx.To = SampleAddress.AddressList[1];

            var chain = await  _blockchainService.GetChainAsync();
            tx.RefBlockNumber = chain.BestChainHeight + NetworkConstants.DefaultInitialSyncOffset + 1;
            
            await _service.SendTransaction(tx, BuildServerCallContext());
            
            received.ShouldBeNull();
        }
        
        [Fact]
        public async Task SendTx_ToHigh_ShouldPublishEvent()
        {
            TransactionsReceivedEvent received = null;
            _eventBus.Subscribe<TransactionsReceivedEvent>(t =>
            {
                received = t;
                return Task.CompletedTask;
            });
            
            Transaction tx = new Transaction();
            tx.From = SampleAddress.AddressList[0];
            tx.To = SampleAddress.AddressList[1];
            
            await _service.SendTransaction(tx, BuildServerCallContext());
            
            received?.Transactions.ShouldNotBeNull();
            received.Transactions.Count().ShouldBe(1);
            received.Transactions.First().From.ShouldBe(tx.From);
        }
        
        #endregion Announce and transaction
        
        #region RequestBlock

        [Fact]
        public async Task RequestBlock_Random_ReturnsBlock()
        {
            var reqBlockCtxt = BuildServerCallContext();
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _service.RequestBlock(new BlockRequest { Hash = chain.LongestChainHash }, reqBlockCtxt);
            
            Assert.NotNull(reply.Block);
            Assert.True(reply.Block.GetHash() == chain.LongestChainHash);
        }
        
        [Fact]
        public async Task RequestBlock_NonExistant_ReturnsEmpty()
        {
            var reply = await _service.RequestBlock(new BlockRequest { Hash = Hash.FromRawBytes(new byte[]{11,22}) }, BuildServerCallContext());
            
            Assert.NotNull(reply);
            Assert.Null(reply.Block);
        }
        
        [Fact]
        public async Task RequestBlock_NoHash_ReturnsEmpty()
        {
            var reply = await _service.RequestBlock(new BlockRequest(), BuildServerCallContext());
            
            Assert.NotNull(reply);
            Assert.Null(reply.Block);
        }

        #endregion RequestBlock

        #region RequestBlocks

        [Fact]
        public async Task RequestBlocks_FromGenesis_ReturnsBlocks()
        {
            var reqBlockCtxt = BuildServerCallContext();
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _service.RequestBlocks(new BlocksRequest { PreviousBlockHash = chain.GenesisBlockHash, Count = 5 }, reqBlockCtxt);
            
            Assert.True(reply.Blocks.Count == 5);
        }
        
        [Fact]
        public async Task RequestBlocks_NonExistant_ReturnsEmpty()
        {
            var reply = await _service.RequestBlocks(new BlocksRequest { PreviousBlockHash = Hash.FromRawBytes(new byte[]{12,21}), Count = 5 }, BuildServerCallContext());
            
            Assert.NotNull(reply?.Blocks);
            Assert.Empty(reply.Blocks);
        }
        
        [Fact]
        public async Task RequestBlocks_NoHash_ReturnsEmpty()
        {
            var reply = await _service.RequestBlocks(new BlocksRequest(), BuildServerCallContext());
            
            Assert.NotNull(reply?.Blocks);
            Assert.Empty(reply.Blocks);
        }

        #endregion RequestBlocks
        
        #region Disconnect

        [Fact]
        public async Task Disconnect_ShouldRemovePeer()
        {
            await _service.Disconnect(new DisconnectReason(), BuildServerCallContext(new Metadata {{ GrpcConstants.PubkeyMetadataKey, NetworkTestConstants.FakePubkey2}}));
            Assert.Empty(_peerPool.GetPeers(true));
        }
        
        #endregion Disconnect

        #region Other tests
        [Fact]
        public async Task NetworkServer_Stop_Test()
        {
            await _networkServer.StopAsync();

            var peers = _peerPool.GetPeers(true).Cast<GrpcPeer>();

            foreach (var peer in peers)
            {
                peer.IsReady.ShouldBeFalse();
            }
        }

        [Fact]
        public async Task Auth_UnaryServerHandler_Success_Test()
        {
            var authInterceptor = GetRequiredService<AuthInterceptor>();
            
            var continuation = new UnaryServerMethod<string, string>((s, y) => Task.FromResult(s));
            var metadata = new Metadata
                {{GrpcConstants.PubkeyMetadataKey, NetworkTestConstants.FakePubkey2}};
            var context = BuildServerCallContext(metadata);
            var headerCount = context.RequestHeaders.Count;
            var result = await authInterceptor.UnaryServerHandler("test", context, continuation);
            
            result.ShouldBe("test");
            context.RequestHeaders.Count.ShouldBeGreaterThan(headerCount);
        }

        [Fact]
        public async Task Auth_UnaryServerHandler_Failed_Test()
        {
            var authInterceptor = GetRequiredService<AuthInterceptor>();
            
            var continuation = new UnaryServerMethod<string, string>((s, y) => Task.FromResult(s));
            var metadata = new Metadata
                {{GrpcConstants.PubkeyMetadataKey, "invalid-pubkey"}};
            var context = BuildServerCallContext(metadata);
            var result = await authInterceptor.UnaryServerHandler("test", context, continuation);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Auth_ClientStreamingServerHandler_Success_Test()
        {
            var authInterceptor = GetRequiredService<AuthInterceptor>();
            var request = Mock.Of<IAsyncStreamReader<string>>();
            var continuation = new ClientStreamingServerMethod<string, string>((s, y) => Task.FromResult("test"));
            var metadata = new Metadata
                {{GrpcConstants.PubkeyMetadataKey, NetworkTestConstants.FakePubkey2}};
            var context = BuildServerCallContext(metadata);
            var headerCount = context.RequestHeaders.Count;
            
            var result = await authInterceptor.ClientStreamingServerHandler(request, context, continuation);
            result.ShouldBe("test");
            context.RequestHeaders.Count.ShouldBeGreaterThan(headerCount);
        }

        [Fact]
        public async Task Auth_ClientStreamingServerHandler_Failed_Test()
        {
            var authInterceptor = GetRequiredService<AuthInterceptor>();
            var request = Mock.Of<IAsyncStreamReader<string>>();
            var continuation = new ClientStreamingServerMethod<string, string>((s, y) => Task.FromResult("test"));
            var metadata = new Metadata
                {{GrpcConstants.PubkeyMetadataKey, "invalid-pubkey"}};
            var context = BuildServerCallContext(metadata);
            
            var result = await authInterceptor.ClientStreamingServerHandler(request, context, continuation);
            result.ShouldBeNull();
        }
        #endregion
    }
}