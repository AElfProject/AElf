using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Testing;
using Grpc.Core.Utils;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcServerServiceTests : GrpcNetworkTestBase
    {
        private IAElfNetworkServer _networkServer;
        private readonly PeerService.PeerServiceBase _service;
        private readonly IBlockchainService _blockchainService;
        private readonly IPeerPool _peerPool;
        private readonly ILocalEventBus _eventBus;
        private IAccountService _acc;

        public GrpcServerServiceTests()
        {
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _service = GetRequiredService<PeerService.PeerServiceBase>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _peerPool = GetRequiredService<IPeerPool>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            _acc = GetRequiredService<IAccountService>();
        }

        private ServerCallContext BuildServerCallContext(Metadata metadata = null)
        {
            var meta = metadata ?? new Metadata();
            return TestServerCallContext.Create("mock", null, DateTime.UtcNow.AddHours(1), meta, CancellationToken.None, 
                "127.0.0.1", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(), writeOptions => { });
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
            
            Hash hash = Hash.Generate();
            await _service.Announce(new PeerNewBlockAnnouncement { BlockHeight = 10, BlockHash = hash}, BuildServerCallContext());
            
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
            tx.From = Address.Generate();
            tx.To = Address.Generate();
            
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
            var reply = await _service.RequestBlock(new BlockRequest { Hash = Hash.Generate() }, BuildServerCallContext());
            
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
            var reply = await _service.RequestBlocks(new BlocksRequest { PreviousBlockHash = Hash.Generate(), Count = 5 }, BuildServerCallContext());
            
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
            await _service.Disconnect(new DisconnectReason(), BuildServerCallContext(new Metadata {{ GrpcConsts.PubkeyMetadataKey, "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399"}}));
            Assert.Empty(_peerPool.GetPeers(true));
        }
        
        #endregion Disconnect

        #region Other tests

        [Fact]
        public async Task Connect_Invalid()
        {
            //invalid handshake
            {
                var handshake = new Handshake();
                var metadata = new Metadata
                    {{GrpcConsts.PubkeyMetadataKey, "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399"}};
                var context = BuildServerCallContext(metadata);

                var connectReply = await _service.Connect(handshake, context);
                connectReply.Err.ShouldBe(AuthError.InvalidHandshake);
            }
            
            //wrong sig
            {
                var handshake = await _peerPool.GetHandshakeAsync();
                handshake.HskData.PublicKey = ByteString.CopyFrom(CryptoHelpers.GenerateKeyPair().PublicKey);
                var metadata = new Metadata
                    {{GrpcConsts.PubkeyMetadataKey, "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399"}};
                var context = TestServerCallContext.Create("mock", "127.0.0.1", DateTime.UtcNow.AddHours(1), metadata, CancellationToken.None, 
                    "ipv4:127.0.0.1:2000", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(), writeOptions => { });
                
                var connectReply = await _service.Connect(handshake, context);
                connectReply.Err.ShouldBe(AuthError.WrongSig);
            }
            
            //invalid peer
            {
                var handshake = await _peerPool.GetHandshakeAsync();
                var metadata = new Metadata
                    {{GrpcConsts.PubkeyMetadataKey, "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399"}};
                var context = BuildServerCallContext(metadata);

                var connectReply = await _service.Connect(handshake, context);
                connectReply.Err.ShouldBe(AuthError.InvalidPeer);
            }
            
            //wrong auth
            {
                var handshake = await _peerPool.GetHandshakeAsync();
                var metadata = new Metadata
                    {{GrpcConsts.PubkeyMetadataKey, "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399"}};
                var context = TestServerCallContext.Create("mock", "127.0.0.1", DateTime.UtcNow.AddHours(1), metadata, CancellationToken.None, 
                    "ipv4:127.0.0.1:2000", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(), writeOptions => { });
                
                var connectReply = await _service.Connect(handshake, context);
                connectReply.Err.ShouldBe(AuthError.WrongAuth);
            }
        }
        
        [Fact]
        public async Task NetworkServer_StopTest()
        {
            await _networkServer.StopAsync();

            var peers = _peerPool.GetPeers(true).Cast<GrpcPeer>();

            foreach (var peer in peers)
            {
                peer.IsReady.ShouldBeFalse();
            }
        }
        
        [Fact]
        public void GrpcUrl_ParseTest()
        {
            //wrong format
            {
                string address = "127.0.0.1:8000";
                var grpcUrl = GrpcUrl.Parse(address);

                grpcUrl.ShouldBeNull();
            }
            
            //correct format
            {
                string address = "ipv4:127.0.0.1:8000";
                var grpcUrl = GrpcUrl.Parse(address);
                
                grpcUrl.IpVersion.ShouldBe("ipv4");
                grpcUrl.IpAddress.ShouldBe("127.0.0.1");
                grpcUrl.Port.ShouldBe(8000);

                var ipPortFormat = grpcUrl.ToIpPortFormat();
                ipPortFormat.ShouldBe("127.0.0.1:8000");
            }
        }
        
        [Fact]
        public async Task UnaryServerHandler_Success()
        {
            var authInterceptor = GetRequiredService<AuthInterceptor>();
            
            var continuation = new UnaryServerMethod<string, string>((s, y) => Task.FromResult(s));
            var metadata = new Metadata
                {{GrpcConsts.PubkeyMetadataKey, "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399"}};
            var context = BuildServerCallContext(metadata);
            var headerCount = context.RequestHeaders.Count;
            var result = await authInterceptor.UnaryServerHandler("test", context, continuation);
            
            result.ShouldBe("test");
            context.RequestHeaders.Count.ShouldBeGreaterThan(headerCount);
        }

        #endregion
    }
}