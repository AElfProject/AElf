using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
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
        private readonly IHandshakeProvider _handshakeProvider;
        
        private readonly GrpcServerService _serverService;

        public GrpcServerServiceTests()
        {
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _serverService = GetRequiredService<GrpcServerService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _peerPool = GetRequiredService<IPeerPool>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            _nodeManager = GetRequiredService<INodeManager>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _handshakeProvider = GetRequiredService<IHandshakeProvider>();
        }

        private ServerCallContext BuildServerCallContext(Metadata metadata = null, string address = null)
        {
            return TestServerCallContext.Create("mock", null, TimestampHelper.GetUtcNow().AddHours(1).ToDateTime(), metadata ?? new Metadata(), CancellationToken.None, 
                address ?? "ipv4:127.0.0.1:5555", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(), writeOptions => { });
        }

        [Fact]
        public async Task DoHandshake_Test()
        {
            var request = new HandshakeRequest {Handshake = new Handshake {HandshakeData = new HandshakeData()}};
            var context = BuildServerCallContext(null, "ipv4:127.0.0.1:7878");
            
            //invalid handshake
            var result = await _serverService.DoHandshake(request, context);
            result.Error.ShouldBe(HandshakeError.ChainMismatch);

            request.Handshake.HandshakeData.ChainId = _blockchainService.GetChainId();
            result = await _serverService.DoHandshake(request, context);
            result.Error.ShouldBe(HandshakeError.ProtocolMismatch);

            request.Handshake.HandshakeData.Version = KernelConstants.ProtocolVersion;
            request.Handshake.HandshakeData.Time = TimestampHelper.GetUtcNow();
            result = await _serverService.DoHandshake(request, context);
            result.Error.ShouldBe(HandshakeError.WrongSignature);

            var peerKeyPair = CryptoHelper.GenerateKeyPair();
            var handshake = NetworkTestHelper.CreateValidHandshake(peerKeyPair, 10, ChainHelper.ConvertBase58ToChainId("AELF"), 2000);
            request = new HandshakeRequest {Handshake = handshake};
            result = await _serverService.DoHandshake(request, context);
            result.Error.ShouldBe(HandshakeError.HandshakeOk);
        }

        [Fact]
        public async Task ConfirmHandshake_Test()
        {
            PeerConnectedEventData received = null;
            _eventBus.Subscribe<PeerConnectedEventData>(t =>
            {
                received = t;
                return Task.CompletedTask;
            });
            var request = new ConfirmHandshakeRequest();
            var context = BuildServerCallContext(null, "ipv4:127.0.0.1:7878");

            var result = await _serverService.ConfirmHandshake(request, context);
            result.ShouldBe(new VoidReply());
            received.ShouldBe(null);
            
            var peer = _peerPool.GetPeers(true).First();
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};
            context = BuildServerCallContext(metadata, "ipv4:127.0.0.1:7878");
            result = await _serverService.ConfirmHandshake(request, context);
            result.ShouldBe(new VoidReply());
            received.ShouldNotBeNull();
        }
        
        #region Announce and transaction

        [Fact]
        public async Task Announce_ShouldAddToBlockCache()
        {
            Hash hash = HashHelper.ComputeFrom(new byte[] {3,6,9});
            var announcement = new BlockAnnouncement { BlockHeight = 1, BlockHash = hash };
            var peer = _peerPool.GetPeers(true).First();
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};
           
            await _serverService.SendAnnouncement(announcement, BuildServerCallContext(metadata));
            peer.TryAddKnownBlock(hash).ShouldBeFalse();
        }

        [Fact]
        public async Task Announce_ShouldPublishEvent_Test()
        {
            AnnouncementReceivedEventData received = null;
            _eventBus.Subscribe<AnnouncementReceivedEventData>(a =>
            {
                received = a;
                return Task.CompletedTask;
            });
            
            await _serverService.SendAnnouncement(null, BuildServerCallContext());
            Assert.Null(received);

            var pubkey = _peerPool.GetPeers(true).First().Info.Pubkey;
            var metadata = new Metadata {
            {
                GrpcConstants.PubkeyMetadataKey, pubkey
            }};

            Hash hash = HashHelper.ComputeFrom(new byte[]{3,6,9});
            await _serverService.SendAnnouncement(new BlockAnnouncement
            {
                BlockHeight = 10, BlockHash = hash
            }, BuildServerCallContext(metadata));

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
                    BlockHash = HashHelper.ComputeFrom($"block-{i}"),
                    BlockHeight = 10 + i
                });
            }
            
            var pubkey = _peerPool.GetPeers(true).First().Info.Pubkey;
            var metadata = new Metadata {
            {
                GrpcConstants.PubkeyMetadataKey, pubkey
            }};
            
            var context = BuildServerCallContext(metadata);
            var requestStream = new TestAsyncStreamReader<BlockAnnouncement>(announcements.ToArray());
            
            
            var result = await _serverService.AnnouncementBroadcastStream(requestStream, context);
            result.ShouldBe(new VoidReply());
            received.Count.ShouldBe(5);
        }
        
        [Fact]
        public async Task BroadcastBlockWithTxs_ShouldAddToBlockCache()
        {
            var peer = _peerPool.GetPeers(true).First();
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};

            var block = _osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("block1"), 1, 
                (await _osTestHelper.GenerateTransferTransactions(1)).ToList());
            
            var requestStream = new TestAsyncStreamReader<BlockWithTransactions>(new List<BlockWithTransactions> { block });
            await _serverService.BlockBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            peer.TryAddKnownBlock(block.GetHash()).ShouldBeFalse();
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
            blocks.Add(_osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("block1"), 1, (await _osTestHelper.GenerateTransferTransactions(1)).ToList()));
            blocks.Add(_osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("block2"), 2, (await _osTestHelper.GenerateTransferTransactions(2)).ToList()));
            blocks.Add(_osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("block3"), 3, (await _osTestHelper.GenerateTransferTransactions(3)).ToList()));

            var context = BuildServerCallContext();
            var requestStream = new TestAsyncStreamReader<BlockWithTransactions>(blocks.ToArray());
            
            context.RequestHeaders.Add(new Metadata.Entry(GrpcConstants.PubkeyMetadataKey, NetworkTestConstants.FakePubkey2));
            var result = await _serverService.BlockBroadcastStream(requestStream, context);
            result.ShouldBe(new VoidReply());
            received.Count.ShouldBe(3);
        }
        
        [Fact]
        public async Task SendTx_ShouldAddToBlockCache()
        {
            var peer = _peerPool.GetPeers(true).First();
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};

            var transaction = new Transaction
            {
                From = SampleAddress.AddressList[0], 
                To = SampleAddress.AddressList[1],
                RefBlockNumber = 1,
                MethodName = "Hello"
            };

            await _serverService.SendTransaction(transaction, BuildServerCallContext(metadata));
            
            peer.TryAddKnownTransaction(transaction.GetHash()).ShouldBeFalse();
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
                To = SampleAddress.AddressList[1],
                RefBlockNumber = 1,
                MethodName = "Hello"
            };

            var pubKey = "SomePubKey";
            Metadata metadata = new Metadata {{GrpcConstants.PubkeyMetadataKey, pubKey}};
            var reqBlockCtxt = BuildServerCallContext(metadata);
            _peerPool.TryAddPeer(GrpcTestPeerHelpers.CreateBasicPeer("127.0.0.1:1245", pubKey));
            
            await _serverService.SendTransaction(tx, reqBlockCtxt);
            
            received.Transactions.ShouldNotBeNull();
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
            
            var pubKey = "SomePubKey";
            Metadata metadata = new Metadata {{GrpcConstants.PubkeyMetadataKey, pubKey}};
            var context = BuildServerCallContext(metadata);
            _peerPool.TryAddPeer(GrpcTestPeerHelpers.CreateBasicPeer("127.0.0.1:500", context.GetPublicKey()));
            var transactions = await _osTestHelper.GenerateTransferTransactions(3);
            var requestStream = new TestAsyncStreamReader<Transaction>(transactions.ToArray());
            
            var result = await _serverService.TransactionBroadcastStream(requestStream, context);
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
            
            await _serverService.SendTransaction(tx, BuildServerCallContext());
            
            received.ShouldBeNull();
        }

        [Fact]
        public async Task LibAnnouncementBroadcastStream_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var irreversibleBlockHeight =  (int)chain.LastIrreversibleBlockHeight;
            var bestBlockHeight = (int)chain.BestChainHeight;
            var requestLibAnnouncements = new List<LibAnnouncement>();
            var preBlockHash = chain.LastIrreversibleBlockHash;
            var preBlockHeight = irreversibleBlockHeight;
            for (var i = irreversibleBlockHeight + 1; i <= bestBlockHeight; i++)
            {
                var libBlock = _osTestHelper.GenerateBlock(preBlockHash, preBlockHeight);
                requestLibAnnouncements.Add(new LibAnnouncement
                {
                    LibHeight = preBlockHeight,
                    LibHash = preBlockHash
                });
                preBlockHash = libBlock.GetHash();
                preBlockHeight++ ;
            }
            
            var requestStream = new TestAsyncStreamReader<LibAnnouncement>(requestLibAnnouncements);
            var pubKey = NetworkTestConstants.FakePubkey2;
            Metadata metadata = new Metadata {{GrpcConstants.PubkeyMetadataKey, pubKey}};
            var context = BuildServerCallContext(metadata);
            var result = await _serverService.LibAnnouncementBroadcastStream(requestStream, context);
            result.ShouldBe(new VoidReply());

            var peer = _peerPool.FindPeerByPublicKey(pubKey);
            var lastBlock = requestLibAnnouncements.Last();
            peer.LastKnownLibHash.ShouldBe(lastBlock.LibHash);
            peer.LastKnownLibHeight.ShouldBe(lastBlock.LibHeight);
        }
        
        #endregion Announce and transaction
        
        #region RequestBlock
        
        [Fact]
        public async Task RequestBlock_WillAddToPeersBlockCache_Test()
        {
            var pubKey = "SomePubKey";
            Metadata metadata = new Metadata {{GrpcConstants.PubkeyMetadataKey, pubKey}};

            var reqBlockCtxt = BuildServerCallContext(metadata);
            _peerPool.TryAddPeer(GrpcTestPeerHelpers.CreateBasicPeer("127.0.0.1:1245", pubKey));

            var chain = await _blockchainService.GetChainAsync();
            var reply = await _serverService.RequestBlock(new BlockRequest { Hash = chain.LongestChainHash }, reqBlockCtxt);

            _peerPool.FindPeerByPublicKey(reqBlockCtxt.GetPublicKey());
            
            Assert.NotNull(reply.Block);
            Assert.True(reply.Block.GetHash() == chain.LongestChainHash);
        }

        [Fact]
        public async Task RequestBlock_Random_ReturnsBlock_Test()
        {
            var pubKey = "SomePubKey";
            Metadata metadata = new Metadata {{GrpcConstants.PubkeyMetadataKey, pubKey}};
            var context = BuildServerCallContext(metadata);
            _peerPool.TryAddPeer(GrpcTestPeerHelpers.CreateBasicPeer("127.0.0.1:500", context.GetPublicKey()));
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _serverService.RequestBlock(new BlockRequest { Hash = chain.LongestChainHash }, context);
            
            Assert.NotNull(reply.Block);
            Assert.True(reply.Block.GetHash() == chain.LongestChainHash);
        }
        
        [Fact]
        public async Task RequestBlock_NonExistant_ReturnsEmpty_Test()
        {
            var reply = await _serverService.RequestBlock(new BlockRequest { Hash = HashHelper.ComputeFrom(new byte[]{11,22}) }, BuildServerCallContext());
            
            Assert.NotNull(reply);
            Assert.Null(reply.Block);
        }
        
        [Fact]
        public async Task RequestBlock_NoHash_ReturnsEmpty_Test()
        {
            var reply = await _serverService.RequestBlock(new BlockRequest(), BuildServerCallContext());
            
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
            var reply = await _serverService.RequestBlocks(new BlocksRequest { PreviousBlockHash = chain.GenesisBlockHash, Count = 5 }, reqBlockCtxt);
            
            Assert.True(reply.Blocks.Count == 5);

            reply = await _serverService.RequestBlocks(new BlocksRequest { PreviousBlockHash = HashHelper.ComputeFrom("invalid"), Count = 5 }, reqBlockCtxt);
            reply.ShouldBe(new BlockList());
        }
        
        [Fact]
        public async Task RequestBlocks_NonExistant_ReturnsEmpty_Test()
        {
            var reply = await _serverService.RequestBlocks(new BlocksRequest { PreviousBlockHash = HashHelper.ComputeFrom(new byte[]{12,21}), Count = 5 }, BuildServerCallContext());
            
            Assert.NotNull(reply?.Blocks);
            Assert.Empty(reply.Blocks);
        }
        
        [Fact]
        public async Task RequestBlocks_NoHash_ReturnsEmpty_Test()
        {
            var reply = await _serverService.RequestBlocks(new BlocksRequest(), BuildServerCallContext());
            
            Assert.NotNull(reply?.Blocks);
            Assert.Empty(reply.Blocks);
        }

        [Fact]
        public async Task RequestBlocks_MoreThenLimit_ReturnsEmpty()
        {
            var serverCallContext = BuildServerCallContext();
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _serverService.RequestBlocks(new BlocksRequest
            {
                PreviousBlockHash = chain.GenesisBlockHash,
                Count = GrpcConstants.MaxSendBlockCountLimit + 1
            }, serverCallContext);

            Assert.True(reply.Blocks.Count == 0);
        }

        #endregion RequestBlocks
        
        #region Disconnect

        [Fact]
        public async Task Disconnect_ShouldRemovePeer_Test()
        {
            await _serverService.Disconnect(new DisconnectReason(), BuildServerCallContext(new Metadata {{ GrpcConstants.PubkeyMetadataKey, NetworkTestConstants.FakePubkey2}}));
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
            var result = await _serverService.GetNodes(null, context);
            result.ShouldBe(new NodeList());

            var node = new NodeInfo
            {
                Endpoint = "127.0.0.1:2001",
                Pubkey = ByteString.CopyFromUtf8("pubkey1")
            };
            await _nodeManager.AddOrUpdateNodeAsync(node);
            var request = new NodesRequest
            {
                MaxCount = 1
            };
            result = await _serverService.GetNodes(request, context);
            result.Nodes.Count.ShouldBe(1);
            result.Nodes[0].ShouldBe(node);
        }

        [Fact]
        public async Task Ping_Test()
        {
            var pingResult = await _serverService.Ping(new PingRequest(), BuildServerCallContext());
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