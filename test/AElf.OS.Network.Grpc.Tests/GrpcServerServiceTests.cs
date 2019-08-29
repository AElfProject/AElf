using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Helper;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Domain;
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
        private readonly INodeManager _nodeManager;
        private readonly OSTestHelper _osTestHelper;
        private readonly IAccountService _accountService;
        
        private readonly GrpcServerService _service;

        public GrpcServerServiceTests()
        {
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _service = GetRequiredService<GrpcServerService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _peerPool = GetRequiredService<IPeerPool>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            _nodeManager = GetRequiredService<INodeManager>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _accountService = GetRequiredService<IAccountService>();
        }

        private ServerCallContext BuildServerCallContext(Metadata metadata = null, string address = null)
        {
            return TestServerCallContext.Create("mock", null, TimestampHelper.GetUtcNow().AddHours(1).ToDateTime(), metadata ?? new Metadata(), CancellationToken.None, 
                address ?? "127.0.0.1", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(), writeOptions => { });
        }

        [Fact]
        public async Task Connect_Invalid_Test()
        {
            var connectionRequest = new ConnectRequest
            {
                Info = new ConnectionInfo
                {
                    ChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
                    ListeningPort = 2001,
                    Pubkey = ByteString.CopyFromUtf8("pubkey"),
                    Version = 1
                }
            };
            //invalid peer
            var context = BuildServerCallContext();
            var connectResult = await _service.Connect(connectionRequest, context);
            connectResult.Error.ShouldBe(ConnectError.InvalidPeer);

            context = BuildServerCallContext(null, "ipv4:127.0.0.1:2001");
            
            //invalid chainId
            connectionRequest.Info.ChainId = 1234;
            connectResult = await _service.Connect(connectionRequest, context);
            connectResult.Error.ShouldBe(ConnectError.ChainMismatch);
            
            //invalid protocol
            connectionRequest.Info.ChainId = ChainHelper.ConvertBase58ToChainId("AELF");
            connectionRequest.Info.Version = 2;
            connectResult = await _service.Connect(connectionRequest, context);
            connectResult.Error.ShouldBe(ConnectError.ProtocolMismatch);

            //exist peer
            connectionRequest.Info.Version = 1;
            connectionRequest.Info.Pubkey = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(NetworkTestConstants.FakePubkey2));
            connectResult = await _service.Connect(connectionRequest, context);
            connectResult.Error.ShouldBe(ConnectError.ConnectionRefused);

            //not exist peer
            connectionRequest.Info.Pubkey = ByteString.CopyFromUtf8("pubkey");
            connectionRequest.Info.ListeningPort = 1335;
            await Should.ThrowAsync<PeerDialException>(async ()=>await _service.Connect(connectionRequest, context));
        }

        [Fact]
        public async Task DoHandshake_Invalid_Test()
        {
            var context = BuildServerCallContext();
            var request = new HandshakeRequest();

            //invalid handshake
            var result = await _service.DoHandshake(request, context);
            result.Error.ShouldBe(HandshakeError.InvalidHandshake);

            var chain = await _blockchainService.GetChainAsync();
            var header = await _blockchainService.GetBlockHeaderByHashAsync(chain.BestChainHash);
            var pubKeyBytes = await _accountService.GetPublicKeyAsync();
            request = new HandshakeRequest
            {
                Handshake = new Handshake
                {
                    HandshakeData = new HandshakeData
                    {
                        BestChainHead = header,
                        LibBlockHeight = chain.LastIrreversibleBlockHeight,
                        Pubkey = ByteString.CopyFrom(pubKeyBytes) 
                    }
                }
            };
            result = await _service.DoHandshake(request, context);
            result.Error.ShouldBe(HandshakeError.InvalidKey);
            
            //wrong signature
            var metadata = new Metadata
            {
                {GrpcConstants.PubkeyMetadataKey, pubKeyBytes.ToHex()}
            };
            context = BuildServerCallContext(metadata, null);
            result = await _service.DoHandshake(request, context);
            result.Error.ShouldBe(HandshakeError.WrongSignature);

            //wrong connection
            request.Handshake.Signature = ByteString.CopyFrom(await _accountService.SignAsync(Hash.FromMessage(request.Handshake.HandshakeData).ToByteArray()));
            result = await _service.DoHandshake(request, context);
            result.Error.ShouldBe(HandshakeError.WrongConnection);
        }

        #region Announce and transaction

        [Fact]
        public async Task Announce_ShouldPublishEvent_Test()
        {
            AnnouncementReceivedEventData received = null;
            _eventBus.Subscribe<AnnouncementReceivedEventData>(a =>
            {
                received = a;
                return Task.CompletedTask;
            });
            
            await _service.SendAnnouncement(null, BuildServerCallContext());
            Assert.Null(received);

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
        public async Task BroadcastAnnouncement_FromStream_Test()
        {
            var received = new List<AnnouncementReceivedEventData>();
            _eventBus.Subscribe<AnnouncementReceivedEventData>(a =>
            {
                received.Add(a);
                return Task.CompletedTask;
            });
            
            var announcements = new List<BlockAnnouncement>();
            for (var i = 0; i < 5; i++)
            {
                announcements.Add(new BlockAnnouncement
                {
                    BlockHash = Hash.FromString($"block-{i}"),
                    BlockHeight = 10 + i,
                    HasFork = false
                });
            }
            var context = BuildServerCallContext();
            var requestStream = new TestAsyncStreamReader<BlockAnnouncement>(announcements.ToArray());
            
            
            var result = await _service.AnnouncementBroadcastStream(requestStream, context);
            result.ShouldBe(new VoidReply());
            received.Count.ShouldBe(5);
        }

        [Fact]
        public async Task BroadcastBlockWithTxs_FromStream_Test()
        {
            var received = new List<BlockReceivedEvent>();
            _eventBus.Subscribe<BlockReceivedEvent>(a =>
            {
                received.Add(a);
                return Task.CompletedTask;
            });

            var blocks = new List<BlockWithTransactions>();
            blocks.Add(_osTestHelper.GenerateBlockWithTransactions(Hash.FromString("block1"), 1, (await _osTestHelper.GenerateTransferTransactions(1)).ToList()));
            blocks.Add(_osTestHelper.GenerateBlockWithTransactions(Hash.FromString("block2"), 2, (await _osTestHelper.GenerateTransferTransactions(2)).ToList()));
            blocks.Add(_osTestHelper.GenerateBlockWithTransactions(Hash.FromString("block3"), 3, (await _osTestHelper.GenerateTransferTransactions(3)).ToList()));

            var context = BuildServerCallContext();
            var requestStream = new TestAsyncStreamReader<BlockWithTransactions>(blocks.ToArray());
            
            var result = await _service.BlockBroadcastStream(requestStream, context);
            result.ShouldBe(new VoidReply());
            received.Count.ShouldBe(3);
        }
        
        [Fact]
        public async Task SendTx_ShouldPublishEvent_Test()
        {
            TransactionsReceivedEvent received = null;
            _eventBus.Subscribe<TransactionsReceivedEvent>(t =>
            {
                received = t;
                return Task.CompletedTask;
            });

            var tx = new Transaction
            {
                From = SampleAddress.AddressList[0], 
                To = SampleAddress.AddressList[1]
            };

            await _service.SendTransaction(tx, BuildServerCallContext());
            
            received?.Transactions.ShouldNotBeNull();
            received.Transactions.Count().ShouldBe(1);
            received.Transactions.First().From.ShouldBe(tx.From);
        }

        [Fact]
        public async Task BroadcastTx_FromStream_Test()
        {
            var received = new List<TransactionsReceivedEvent>();
            _eventBus.Subscribe<TransactionsReceivedEvent>(t =>
            {
                received.Add(t);
                return Task.CompletedTask;
            });
            var context = BuildServerCallContext();
            var transactions = await _osTestHelper.GenerateTransferTransactions(3);
            var requestStream = new TestAsyncStreamReader<Transaction>(transactions.ToArray());
            
            var result = await _service.TransactionBroadcastStream(requestStream, context);
            result.ShouldBe(new VoidReply());
            
            received.Count.ShouldBe(3);
        }
        
        [Fact]
        public async Task SendTx_WithHighTxRef_ShouldNotPublishEvent_Test()
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
        public async Task SendTx_ToHigh_ShouldPublishEvent_Test()
        {
            TransactionsReceivedEvent received = null;
            _eventBus.Subscribe<TransactionsReceivedEvent>(t =>
            {
                received = t;
                return Task.CompletedTask;
            });

            var tx = new Transaction
            {
                From = SampleAddress.AddressList[0], 
                To = SampleAddress.AddressList[1]
            };

            await _service.SendTransaction(tx, BuildServerCallContext());
            
            received?.Transactions.ShouldNotBeNull();
            received.Transactions.Count().ShouldBe(1);
            received.Transactions.First().From.ShouldBe(tx.From);
        }
        
        #endregion Announce and transaction
        
        #region RequestBlock

        [Fact]
        public async Task RequestBlock_Random_ReturnsBlock_Test()
        {
            var reqBlockCtxt = BuildServerCallContext();
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _service.RequestBlock(new BlockRequest { Hash = chain.LongestChainHash }, reqBlockCtxt);
            
            Assert.NotNull(reply.Block);
            Assert.True(reply.Block.GetHash() == chain.LongestChainHash);
        }
        
        [Fact]
        public async Task RequestBlock_NonExistant_ReturnsEmpty_Test()
        {
            var reply = await _service.RequestBlock(new BlockRequest { Hash = Hash.FromRawBytes(new byte[]{11,22}) }, BuildServerCallContext());
            
            Assert.NotNull(reply);
            Assert.Null(reply.Block);
        }
        
        [Fact]
        public async Task RequestBlock_NoHash_ReturnsEmpty_Test()
        {
            var reply = await _service.RequestBlock(new BlockRequest(), BuildServerCallContext());
            
            Assert.NotNull(reply);
            Assert.Null(reply.Block);
        }

        #endregion RequestBlock

        #region RequestBlocks

        [Fact]
        public async Task RequestBlocks_FromGenesis_ReturnsBlocks_Test()
        {
            var reqBlockCtxt = BuildServerCallContext();
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _service.RequestBlocks(new BlocksRequest { PreviousBlockHash = chain.GenesisBlockHash, Count = 5 }, reqBlockCtxt);
            
            Assert.True(reply.Blocks.Count == 5);

            reply = await _service.RequestBlocks(new BlocksRequest { PreviousBlockHash = Hash.FromString("invalid"), Count = 5 }, reqBlockCtxt);
            reply.ShouldBe(new BlockList());
        }
        
        [Fact]
        public async Task RequestBlocks_NonExistant_ReturnsEmpty_Test()
        {
            var reply = await _service.RequestBlocks(new BlocksRequest { PreviousBlockHash = Hash.FromRawBytes(new byte[]{12,21}), Count = 5 }, BuildServerCallContext());
            
            Assert.NotNull(reply?.Blocks);
            Assert.Empty(reply.Blocks);
        }
        
        [Fact]
        public async Task RequestBlocks_NoHash_ReturnsEmpty_Test()
        {
            var reply = await _service.RequestBlocks(new BlocksRequest(), BuildServerCallContext());
            
            Assert.NotNull(reply?.Blocks);
            Assert.Empty(reply.Blocks);
        }

        #endregion RequestBlocks
        
        #region Disconnect

        [Fact]
        public async Task Disconnect_ShouldRemovePeer_Test()
        {
            await _service.Disconnect(new DisconnectReason(), BuildServerCallContext(new Metadata {{ GrpcConstants.PubkeyMetadataKey, NetworkTestConstants.FakePubkey2}}));
            Assert.Empty(_peerPool.GetPeers(true));
        }
        
        #endregion Disconnect

        #region Other tests
        [Fact]
        public async Task NetworkServer_Stop_Test()
        {
            await _networkServer.StopAsync(false);

            var peers = _peerPool.GetPeers(true).Cast<GrpcPeer>();

            foreach (var peer in peers)
            {
                peer.IsReady.ShouldBeFalse();
            }
        }

        [Fact]
        public void GrpcUrl_Parse_Test()
        public async Task Auth_UnaryServerHandler_Success_Test()
        {
            //wrong format
            {
                const string address = "127.0.0.1:8000";
                var grpcUrl = GrpcUrl.Parse(address);

                grpcUrl.ShouldBeNull();
            }
            
            //correct format
            {
                const string address = "ipv4:127.0.0.1:8000";
                var grpcUrl = GrpcUrl.Parse(address);
                
                grpcUrl.IpVersion.ShouldBe("ipv4");
                grpcUrl.IpAddress.ShouldBe("127.0.0.1");
                grpcUrl.Port.ShouldBe(8000);

                var ipPortFormat = grpcUrl.ToIpPortFormat();
                ipPortFormat.ShouldBe("127.0.0.1:8000");
            }
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
        public async Task Auth_UnaryServerHandler_Success_Test()
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
        public async Task Auth_UnaryServerHandler_Failed_Test()
        {
            var authInterceptor = GetRequiredService<AuthInterceptor>();
            
            var continuation = new UnaryServerMethod<string, string>((s, y) => Task.FromResult(s));
            var metadata = new Metadata
                {{GrpcConstants.PubkeyMetadataKey, CryptoHelper.GenerateKeyPair().PublicKey.ToHex()}};
            var context = BuildServerCallContext(metadata);
            var headerCount = context.RequestHeaders.Count;
            var result = await authInterceptor.UnaryServerHandler("test", context, continuation);
            result.ShouldBeNull();
            context.RequestHeaders.Count.ShouldBe(headerCount);
        }

        [Fact]
        public async Task ClientStreamingServerHandler_Success_Test()
        {
            var authInterceptor = GetRequiredService<AuthInterceptor>();
            var requestStream = new TestAsyncStreamReader<string>(new []{"test1", "test2", "test3"});
            var continuation = new ClientStreamingServerMethod<string, string>((s,y) => Task.FromResult(s.Current));
            var metadata = new Metadata
                {{GrpcConstants.PubkeyMetadataKey, NetworkTestConstants.FakePubkey2}};
            var context = BuildServerCallContext(metadata);
            var headerCount = context.RequestHeaders.Count;
            
            await requestStream.MoveNext();
            var result = await authInterceptor.ClientStreamingServerHandler(requestStream, context, continuation);
            result.ShouldBe("test1");
            context.RequestHeaders.Count.ShouldBeGreaterThan(headerCount);
            
            await requestStream.MoveNext();
            result = await authInterceptor.ClientStreamingServerHandler(requestStream, context, continuation);
            result.ShouldBe("test2");
            
            await requestStream.MoveNext();
            result = await authInterceptor.ClientStreamingServerHandler(requestStream, context, continuation);
            result.ShouldBe("test3");
        }

        [Fact]
        public async Task GetNodes_Test()
        {
            var context = BuildServerCallContext();
            var result = await _service.GetNodes(null, context);
            result.ShouldBe(new NodeList());

            var node = new NodeInfo
            {
                Endpoint = "127.0.0.1:2001",
                Pubkey = ByteString.CopyFromUtf8("pubkey1")
            };
            await _nodeManager.AddNodeAsync(node);
            var request = new NodesRequest
            {
                MaxCount = 1
            };
            result = await _service.GetNodes(request, context);
            result.Nodes.Count.ShouldBe(1);
            result.Nodes[0].ShouldBe(node);
        }

        [Fact]
        public async Task Ping_Test()
        {
            var pingResult = await _service.Ping(new PingRequest(), BuildServerCallContext());
            pingResult.ShouldBe(new PongReply());
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