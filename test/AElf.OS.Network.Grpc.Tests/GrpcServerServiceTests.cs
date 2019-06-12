using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Testing;
using Grpc.Core.Utils;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcServerServiceTests : GrpcNetworkTestBase
    {
        private readonly IAElfNetworkServer _networkServer;
        private readonly PeerService.PeerServiceBase _service;
        private readonly IBlockchainService _blockchainService;
        private readonly IPeerPool _peerPool;
        private readonly ILocalEventBus _eventBus;

        public GrpcServerServiceTests()
        {
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _service = GetRequiredService<PeerService.PeerServiceBase>();
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

            Hash hash = Hash.Generate();
            await _service.Announce(new PeerNewBlockAnnouncement
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
            tx.From = Address.Generate();
            tx.To = Address.Generate();
            
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
            tx.From = Address.Generate();
            tx.To = Address.Generate();

            var chain = await  _blockchainService.GetChainAsync();
            tx.RefBlockNumber = chain.BestChainHeight + NetworkConstants.DefaultMinBlockGapBeforeSync + 1;
            
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
            await _service.Disconnect(new DisconnectReason(), BuildServerCallContext(new Metadata {{ GrpcConstants.PubkeyMetadataKey, GrpcTestConstants.FakePubKey2}}));
            Assert.Empty(_peerPool.GetPeers(true));
        }
        
        #endregion Disconnect

        #region Other tests

        [Fact]
        public async Task Connect_MaxPeersReached()
        {
            _peerPool.AddPeer(GrpcTestHelper.CreateNewPeer("127.0.0.1:3000", false));
            
            ConnectReply connectReply = await _service.Connect(new Handshake(), BuildServerCallContext(null, "ipv4:127.0.0.1:2000"));
            
            connectReply.Err.ShouldBe(AuthError.ConnectionRefused);
        }
        
        [Fact]
        public async Task Connect_Cleanup_Test()
        {
            // Generate peer identity
            var peerKeyPair = CryptoHelpers.GenerateKeyPair();
            var handshake = CreateHandshake(peerKeyPair);
            
            await _service.Connect(handshake, BuildServerCallContext(null, "ipv4:127.0.0.1:2000"));
            await _service.Connect(handshake, BuildServerCallContext(null, "ipv4:127.0.0.1:2000"));

            var peers = _peerPool.GetPeers().Select(p => p.PubKey)
                .Where(key => key == peerKeyPair.PublicKey.ToHex())
                .ToList();
            
            peers.Count.ShouldBe(1);
            peers.First().ShouldBe(peerKeyPair.PublicKey.ToHex());
        }
        
        private Handshake CreateHandshake(ECKeyPair keyPair)
        {
            var nd = new HandshakeData
            {
                ListeningPort = 1234,
                PublicKey = ByteString.CopyFrom(keyPair.PublicKey),
                Version = KernelConstants.ProtocolVersion,
                ChainId = _blockchainService.GetChainId()
            };

            byte[] sig = CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, Hash.FromMessage(nd).ToByteArray());

            var hsk = new Handshake
            {
                HskData = nd,
                Signature = ByteString.CopyFrom(sig),
                Header = new BlockHeader()
            };

            return hsk;
        }

        [Fact]
        public async Task Connect_Invalid()
        {
            // invalid handshake
            {
                var handshake = new Handshake();
                var metadata = new Metadata
                    {{GrpcConstants.PubkeyMetadataKey, "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399"}};
                var context = BuildServerCallContext(metadata, "ipv4:127.0.0.1:1000");

                var connectReply = await _service.Connect(handshake, context);
                connectReply.Err.ShouldBe(AuthError.InvalidHandshake);
            }
            
            // wrong sig
            {
                var handshake = await _peerPool.GetHandshakeAsync();
                handshake.HskData.PublicKey = ByteString.CopyFrom(CryptoHelpers.GenerateKeyPair().PublicKey);
                var metadata = new Metadata
                    {{GrpcConstants.PubkeyMetadataKey, "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399"}};
                var context = TestServerCallContext.Create("mock", "127.0.0.1", TimestampHelper.GetUtcNow().AddHours(1).ToDateTime(), metadata, CancellationToken.None, 
                    "ipv4:127.0.0.1:2000", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(), writeOptions => { });
                
                var connectReply = await _service.Connect(handshake, context);
                connectReply.Err.ShouldBe(AuthError.WrongSig);
            }
            
            // invalid peer
            {
                var handshake = await _peerPool.GetHandshakeAsync();
                var metadata = new Metadata
                    {{GrpcConstants.PubkeyMetadataKey, "0454dcd0afc20d015e328666d8d25f3f28b13ccd9744eb6b153e4a69709aab399"}};
                var context = BuildServerCallContext(metadata, "127.0.0.1:3000");

                var connectReply = await _service.Connect(handshake, context);
                connectReply.Err.ShouldBe(AuthError.InvalidPeer);
            }
        }
        
        [Fact]
        public async Task Only_Authorized_Test()
        {
            // get the service to be able to switch the options
            var serverService = _service as GrpcServerService;
            
            // authorized 
            ECKeyPair authorizedPeer = CryptoHelpers.GenerateKeyPair();
            ECKeyPair nonAuthorizedPeer = CryptoHelpers.GenerateKeyPair();
            
            // miners only options
            var minersOnlyOptions = new NetworkOptions {
                AuthorizedPeers = AuthorizedPeers.Authorized,
                AuthorizedKeys = new List<string> { authorizedPeer.PublicKey.ToHex() }
            };
            
            // change the options
            serverService.NetworkOptionsSnapshot = CreateIOptionSnapshotMock(minersOnlyOptions);
            
            var context = BuildServerCallContext(null, "ipv4:127.0.0.1:2000");
            
            {
                // handshake as non authorized
                var handshake = CreateHandshake(nonAuthorizedPeer);
                var connectReply = await _service.Connect(handshake, context);

                connectReply.Err.ShouldBe(AuthError.ConnectionRefused);
            }

            {
                // handshake as authorized
                var handshake = CreateHandshake(authorizedPeer);
                var connectReply = await _service.Connect(handshake, context); 
                
                connectReply.Err.ShouldBe(AuthError.None);
            }

            minersOnlyOptions.AuthorizedPeers = AuthorizedPeers.Any;

            {
                // handshake as non authorized
                var handshake = CreateHandshake(nonAuthorizedPeer);
                var connectReply = await _service.Connect(handshake, context);

                connectReply.Err.ShouldBe(AuthError.None);
            }
            
            {
                // handshake as authorized
                var handshake = CreateHandshake(authorizedPeer);
                var connectReply = await _service.Connect(handshake, context); 
                
                connectReply.Err.ShouldBe(AuthError.None);
            }
        }
        
        public static IOptionsSnapshot<T> CreateIOptionSnapshotMock<T>(T value) where T : class, new()
        {
            var mock = new Mock<IOptionsSnapshot<T>>();
            mock.Setup(m => m.Value).Returns(value);
            return mock.Object;
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
        public async Task Auth_UnaryServerHandler_Success()
        {
            var authInterceptor = GetRequiredService<AuthInterceptor>();
            
            var continuation = new UnaryServerMethod<string, string>((s, y) => Task.FromResult(s));
            var metadata = new Metadata
                {{GrpcConstants.PubkeyMetadataKey, GrpcTestConstants.FakePubKey2}};
            var context = BuildServerCallContext(metadata);
            var headerCount = context.RequestHeaders.Count;
            var result = await authInterceptor.UnaryServerHandler("test", context, continuation);
            
            result.ShouldBe("test");
            context.RequestHeaders.Count.ShouldBeGreaterThan(headerCount);
        }

        #endregion
    }
}