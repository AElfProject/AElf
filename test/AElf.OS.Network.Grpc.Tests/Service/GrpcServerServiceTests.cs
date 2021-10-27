using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool;
using AElf.OS.Network.Application;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Testing;
using Grpc.Core.Utils;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network.Grpc
{
    public class GrpcServerServiceTests : GrpcNetworkWithChainAndPeerTestBase
    {
        private readonly IAElfNetworkServer _networkServer;
        private readonly IBlockchainService _blockchainService;
        private readonly IPeerPool _peerPool;
        private readonly ILocalEventBus _eventBus;
        private readonly INodeManager _nodeManager;
        private readonly OSTestHelper _osTestHelper;
        private readonly INodeSyncStateProvider _syncStateProvider;
        
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
            _syncStateProvider = GetRequiredService<INodeSyncStateProvider>();
        }

        private ServerCallContext BuildServerCallContext(Metadata metadata = null, string address = null)
        {
            return TestServerCallContext.Create("mock", null, TimestampHelper.GetUtcNow().AddHours(1).ToDateTime(), metadata ?? new Metadata(), CancellationToken.None, 
                address ?? "ipv4:127.0.0.1:5555", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(), writeOptions => { });
        }

        #region Handshake
        
        [Fact]
        public async Task DoHandshake_Test()
        {
            var context = BuildServerCallContext(null, "ipv4:127.0.0.1:7878");
            var peerKeyPair = CryptoHelper.GenerateKeyPair();
            var handshake = NetworkTestHelper.CreateValidHandshake(peerKeyPair, 10, ChainHelper.ConvertBase58ToChainId("AELF"), 2000);
            var request = new HandshakeRequest {Handshake = handshake};
            var result = await _serverService.DoHandshake(request, context);
            result.Error.ShouldBe(HandshakeError.HandshakeOk);
        }

        [Fact]
        public async Task DoHandshake_InvalidPeer_Test()
        {
            var context = BuildServerCallContext(null, "ipv4:127.0.0.1:a");
            var peerKeyPair = CryptoHelper.GenerateKeyPair();
            var handshake = NetworkTestHelper.CreateValidHandshake(peerKeyPair, 10, ChainHelper.ConvertBase58ToChainId("AELF"), 2000);
            var request = new HandshakeRequest {Handshake = handshake};
            var result = await _serverService.DoHandshake(request, context);
            result.Error.ShouldBe(HandshakeError.InvalidConnection);
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

            await _serverService.ConfirmHandshake(request, context);
            received.ShouldBe(null);
            
            var peer = _peerPool.GetPeers(true).First();
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};
            context = BuildServerCallContext(metadata, "ipv4:127.0.0.1:7878");
            await _serverService.ConfirmHandshake(request, context);
            received.ShouldNotBeNull();
        }
        
        #endregion
        
        #region Boradcast
        
        [Fact]
        public async Task BlockBroadcastStream_UnknownPeer_Test()
        {
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, "UnknownPeerPubkey" }};

            var block = _osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("block1"), 1, 
                (await _osTestHelper.GenerateTransferTransactions(1)).ToList());
            
            var requestStream = new TestAsyncStreamReader<BlockWithTransactions>(new List<BlockWithTransactions> { block });
            await _serverService.BlockBroadcastStream(requestStream, BuildServerCallContext(metadata))
                .ShouldThrowAsync<RpcException>();
        }
        
        [Fact]
        public async Task BlockBroadcastStream_RepeatedBlock_Test()
        {
            var peer = _peerPool.GetPeers(true).First();
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};

            var block = _osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("block1"), 1, 
                (await _osTestHelper.GenerateTransferTransactions(1)).ToList());
            
            var requestStream = new TestAsyncStreamReader<BlockWithTransactions>(new List<BlockWithTransactions> { block });
            await _serverService.BlockBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            var received = new List<BlockReceivedEvent>();
            _eventBus.Subscribe<BlockReceivedEvent>(a =>
            {
                received.Add(a);
                return Task.CompletedTask;
            });
            
            requestStream = new TestAsyncStreamReader<BlockWithTransactions>(new List<BlockWithTransactions> { block });
            await _serverService.BlockBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            received.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task BlockBroadcastStream_Test()
        {
            var received = new List<BlockReceivedEvent>();
            _eventBus.Subscribe<BlockReceivedEvent>(a =>
            {
                received.Add(a);
                return Task.CompletedTask;
            });
            
            var peer = _peerPool.GetPeers(true).First();
            peer.SyncState.ShouldNotBe(SyncState.Finished);
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};

            var block = _osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("block1"), 1, 
                (await _osTestHelper.GenerateTransferTransactions(1)).ToList());
            
            var requestStream = new TestAsyncStreamReader<BlockWithTransactions>(new List<BlockWithTransactions> { block });
            await _serverService.BlockBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            peer.TryAddKnownBlock(block.GetHash()).ShouldBeFalse();
            received.Count.ShouldBe(1);
            peer.SyncState.ShouldBe(SyncState.Finished);
        }

        [Fact]
        public async Task BlockBroadcastStream_Stream_Test()
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
            await _serverService.BlockBroadcastStream(requestStream, context);
            received.Count.ShouldBe(3);
        }
        
        [Fact]
        public async Task AnnouncementBroadcastStream_UnknownPeer_Test()
        {
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, "UnknownPeerPubkey" }};

            var block = _osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("block1"), 1, 
                (await _osTestHelper.GenerateTransferTransactions(1)).ToList());

            var requestStream = new TestAsyncStreamReader<BlockAnnouncement>(new List<BlockAnnouncement>
            {
                new BlockAnnouncement
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                }
            });
            
            await _serverService.AnnouncementBroadcastStream(requestStream, BuildServerCallContext(metadata))
                .ShouldThrowAsync<RpcException>();
        }
        
        [Fact]
        public async Task AnnouncementBroadcastStream_RepeatedBlock_Test()
        {
            var peer = _peerPool.GetPeers(true).First();
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};

            var block = _osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("block1"), 1, 
                (await _osTestHelper.GenerateTransferTransactions(1)).ToList());

            var requestStream = new TestAsyncStreamReader<BlockAnnouncement>(new List<BlockAnnouncement>
            {
                new BlockAnnouncement
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                }
            });
            await _serverService.AnnouncementBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            var received = new List<AnnouncementReceivedEventData>();
            _eventBus.Subscribe<AnnouncementReceivedEventData>(a =>
            {
                received.Add(a);
                return Task.CompletedTask;
            });
            
            requestStream = new TestAsyncStreamReader<BlockAnnouncement>(new List<BlockAnnouncement>
            {
                new BlockAnnouncement
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                }
            });
            await _serverService.AnnouncementBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            received.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task AnnouncementBroadcastStream_Test()
        {
            var received = new List<AnnouncementReceivedEventData>();
            _eventBus.Subscribe<AnnouncementReceivedEventData>(a =>
            {
                received.Add(a);
                return Task.CompletedTask;
            });
            
            var peer = _peerPool.GetPeers(true).First();
            peer.SyncState.ShouldNotBe(SyncState.Finished);
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};

            var block = _osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("block1"), 1, 
                (await _osTestHelper.GenerateTransferTransactions(1)).ToList());

            var requestStream = new TestAsyncStreamReader<BlockAnnouncement>(new List<BlockAnnouncement> {null});
            await _serverService.AnnouncementBroadcastStream(requestStream, BuildServerCallContext(metadata));
            received.Count.ShouldBe(0);
            peer.SyncState.ShouldNotBe(SyncState.Finished);
            
            requestStream = new TestAsyncStreamReader<BlockAnnouncement>(new List<BlockAnnouncement>
            {
                new BlockAnnouncement()
            });
            await _serverService.AnnouncementBroadcastStream(requestStream, BuildServerCallContext(metadata));
            received.Count.ShouldBe(0);
            peer.SyncState.ShouldNotBe(SyncState.Finished);
            
            requestStream = new TestAsyncStreamReader<BlockAnnouncement>(new List<BlockAnnouncement>
            {
                new BlockAnnouncement
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                }
            });
            await _serverService.AnnouncementBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            peer.TryAddKnownBlock(block.GetHash()).ShouldBeFalse();
            received.Count.ShouldBe(1);
            peer.SyncState.ShouldBe(SyncState.Finished);
        }

        [Fact]
        public async Task AnnouncementBroadcastStream_Stream_Test()
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
            
            
            await _serverService.AnnouncementBroadcastStream(requestStream, context);
            received.Count.ShouldBe(5);
        }
        
        [Fact]
        public async Task TransactionBroadcastStream_UnknownPeer_Test()
        {
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, "UnknownPeerPubkey" }};

            var transaction = await _osTestHelper.GenerateTransferTransaction();
            
            var requestStream = new TestAsyncStreamReader<Transaction>(new List<Transaction> { transaction });
            await _serverService.TransactionBroadcastStream(requestStream, BuildServerCallContext(metadata))
                .ShouldThrowAsync<RpcException>();
        }
        
        [Fact]
        public async Task TransactionBroadcastStream_RepeatedTransaction_Test()
        {
            var peer = _peerPool.GetPeers(true).First();
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};

            var transaction = await _osTestHelper.GenerateTransferTransaction();
            
            var requestStream = new TestAsyncStreamReader<Transaction>(new List<Transaction> { transaction });
            await _serverService.TransactionBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            var received = new List<TransactionsReceivedEvent>();
            _eventBus.Subscribe<TransactionsReceivedEvent>(a =>
            {
                received.Add(a);
                return Task.CompletedTask;
            });
            
            requestStream = new TestAsyncStreamReader<Transaction>(new List<Transaction> { transaction });
            await _serverService.TransactionBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            received.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task TransactionBroadcastStream_TransactionIsTooHigh_Test()
        {
            var received = new List<TransactionsReceivedEvent>();
            _eventBus.Subscribe<TransactionsReceivedEvent>(a =>
            {
                received.Add(a);
                return Task.CompletedTask;
            });
            
            var peer = _peerPool.GetPeers(true).First();
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};

            var chain = await _blockchainService.GetChainAsync();
            
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            transaction.RefBlockNumber = chain.LongestChainHeight + NetworkConstants.DefaultInitialSyncOffset + 1;
            
            var requestStream = new TestAsyncStreamReader<Transaction>(new List<Transaction> { transaction });
            await _serverService.TransactionBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            peer.TryAddKnownBlock(transaction.GetHash()).ShouldBeTrue();
            received.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task TransactionBroadcastStream_Test()
        {
            var received = new List<TransactionsReceivedEvent>();
            _eventBus.Subscribe<TransactionsReceivedEvent>(a =>
            {
                received.Add(a);
                return Task.CompletedTask;
            });
            
            var peer = _peerPool.GetPeers(true).First();
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};

            var transaction = await _osTestHelper.GenerateTransferTransaction();
            
            var requestStream = new TestAsyncStreamReader<Transaction>(new List<Transaction> { transaction });
            await _serverService.TransactionBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            peer.TryAddKnownTransaction(transaction.GetHash()).ShouldBeFalse();
            received.Count.ShouldBe(1);
        }

        [Fact]
        public async Task TransactionBroadcastStream_Stream_Test()
        {
            var received = new List<TransactionsReceivedEvent>();
            _eventBus.Subscribe<TransactionsReceivedEvent>(t =>
            {
                received.Add(t);
                return Task.CompletedTask;
            });
            
            var peer = _peerPool.GetPeers(true).First();
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};
            
            var transactions = await _osTestHelper.GenerateTransferTransactions(3);
            var requestStream = new TestAsyncStreamReader<Transaction>(transactions.ToArray());
            
            await _serverService.TransactionBroadcastStream(requestStream, BuildServerCallContext(metadata));
            
            received.Count.ShouldBe(3);
        }
        
        [Fact]
        public async Task LibAnnouncementBroadcastStream_UnknownPeer_Test()
        {
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, "UnknownPeerPubkey" }};

            var transaction = await _osTestHelper.GenerateTransferTransaction();

            var requestStream = new TestAsyncStreamReader<LibAnnouncement>(new List<LibAnnouncement>
            {
                new LibAnnouncement
                {
                    LibHeight = 100,
                    LibHash = HashHelper.ComputeFrom("LibHash")
                }
            });
            await _serverService.LibAnnouncementBroadcastStream(requestStream, BuildServerCallContext(metadata))
                .ShouldThrowAsync<RpcException>();
        }
        
        [Fact]
        public async Task LibAnnouncementBroadcastStream_Test()
        {
            var libHeight = 100;
            var libHash = HashHelper.ComputeFrom("LibHash");
            var peer = _peerPool.GetPeers(true).First();
            peer.SyncState.ShouldNotBe(SyncState.Finished);
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};
            var context = BuildServerCallContext(metadata);

            var lastLibHeight = peer.LastKnownLibHeight;
            var lastLibHash = peer.LastKnownLibHash;
            var requestStream = new TestAsyncStreamReader<LibAnnouncement>(new List<LibAnnouncement>
            {
                null
            });
            await _serverService.LibAnnouncementBroadcastStream(requestStream, context);
            peer.SyncState.ShouldNotBe(SyncState.Finished);
            peer.LastKnownLibHash.ShouldBe(lastLibHash);
            peer.LastKnownLibHeight.ShouldBe(lastLibHeight);
            
            requestStream = new TestAsyncStreamReader<LibAnnouncement>(new List<LibAnnouncement>
            {
                new LibAnnouncement()
            });
            await _serverService.LibAnnouncementBroadcastStream(requestStream, context);
            peer.SyncState.ShouldNotBe(SyncState.Finished);
            peer.LastKnownLibHash.ShouldBe(lastLibHash);
            peer.LastKnownLibHeight.ShouldBe(lastLibHeight);

            requestStream = new TestAsyncStreamReader<LibAnnouncement>(new List<LibAnnouncement>
            {
                new LibAnnouncement
                {
                    LibHeight = libHeight,
                    LibHash = libHash
                }
            });
            await _serverService.LibAnnouncementBroadcastStream(requestStream, context);

            peer.SyncState.ShouldBe(SyncState.Finished);
            peer.LastKnownLibHash.ShouldBe(libHash);
            peer.LastKnownLibHeight.ShouldBe(libHeight);
        }

        [Fact]
        public async Task LibAnnouncementBroadcastStream_Stream_Test()
        {
            long libHeight = 0;
            var libHash = Hash.Empty;
            var libAnnouncements = new List<LibAnnouncement>();
            for (var i = 0; i <= 3; i++)
            {
                libHeight = 100 + i;
                libHash = HashHelper.ComputeFrom(libHeight);
                
                libAnnouncements.Add(new LibAnnouncement
                {
                    LibHeight = libHeight,
                    LibHash = libHash
                });
            }
            var peer = _peerPool.GetPeers(true).First();
            peer.SyncState.ShouldNotBe(SyncState.Finished);
            var pubkey = peer.Info.Pubkey;
            var metadata = new Metadata {{ GrpcConstants.PubkeyMetadataKey, pubkey }};
            
            var requestStream = new TestAsyncStreamReader<LibAnnouncement>(libAnnouncements);
            var context = BuildServerCallContext(metadata);
             await _serverService.LibAnnouncementBroadcastStream(requestStream, context);

             peer.SyncState.ShouldBe(SyncState.Finished);
             peer.LastKnownLibHash.ShouldBe(libHash);
             peer.LastKnownLibHeight.ShouldBe(libHeight);
        }
        
        #endregion
        
        #region RequestBlock
        
        [Fact]
        public async Task RequestBlock_SyncStateIsNotFinished_Test()
        {
            _syncStateProvider.SetSyncTarget(0);
            
            var peer = _peerPool.GetPeers(true).First();
            var pubKey = peer.Info.Pubkey;
            Metadata metadata = new Metadata {{GrpcConstants.PubkeyMetadataKey, pubKey}};
            var context = BuildServerCallContext(metadata);
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _serverService.RequestBlock(new BlockRequest { Hash = chain.LongestChainHash }, context);
            
            Assert.NotNull(reply);
            Assert.Null(reply.Block);
        }

        [Fact]
        public async Task RequestBlock_Test()
        {
            var peer = _peerPool.GetPeers(true).First();
            var pubKey = peer.Info.Pubkey;
            Metadata metadata = new Metadata {{GrpcConstants.PubkeyMetadataKey, pubKey}};
            var context = BuildServerCallContext(metadata);
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _serverService.RequestBlock(new BlockRequest { Hash = chain.LongestChainHash }, context);
            
            Assert.NotNull(reply.Block);
            Assert.True(reply.Block.GetHash() == chain.LongestChainHash);
            
            peer.TryAddKnownBlock(chain.LongestChainHash).ShouldBeFalse();
        }

        [Fact]
        public async Task RequestBlock_NotExistBlock_ReturnsEmpty()
        {
            var reply = await _serverService.RequestBlock(new BlockRequest {Hash = HashHelper.ComputeFrom("NotExist")},
                BuildServerCallContext());

            Assert.NotNull(reply);
            Assert.Null(reply.Block);
        }

        [Fact]
        public async Task RequestBlock_NoHash_ReturnsEmpty()
        {
            var reply = await _serverService.RequestBlock(null, BuildServerCallContext());
            Assert.Null(reply.Block);
            
            reply = await _serverService.RequestBlock(new BlockRequest(), BuildServerCallContext());
            Assert.Null(reply.Block);
        }

        #endregion RequestBlock

        #region RequestBlocks

        [Fact]
        public async Task RequestBlocks_Test()
        {
            var reqBlockContext = BuildServerCallContext();
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _serverService.RequestBlocks(new BlocksRequest { PreviousBlockHash = chain.GenesisBlockHash, Count = 5 }, reqBlockContext);
            
            Assert.True(reply.Blocks.Count == 5);
            reply.Blocks.First().Header.PreviousBlockHash.ShouldBe(chain.GenesisBlockHash);
            
        }
        
        [Fact]
        public async Task RequestBlocks_SyncStateIsNotFinished_Test()
        {
            _syncStateProvider.SetSyncTarget(0);
            
            var reqBlockContext = BuildServerCallContext();
            var chain = await _blockchainService.GetChainAsync();
            var reply = await _serverService.RequestBlocks(new BlocksRequest { PreviousBlockHash = chain.GenesisBlockHash, Count = 5 }, reqBlockContext);
            
            Assert.Empty(reply.Blocks);
        }
        
        [Fact]
        public async Task RequestBlocks_NotExist_ReturnsEmpty()
        {
            var reply = await _serverService.RequestBlocks(new BlocksRequest { PreviousBlockHash = HashHelper.ComputeFrom("NotExist"), Count = 5 }, BuildServerCallContext());
            Assert.Empty(reply.Blocks);
        }
        
        [Fact]
        public async Task RequestBlocks_NoHash_ReturnsEmpty()
        {
            var reply = await _serverService.RequestBlocks(null, BuildServerCallContext());
            Assert.Empty(reply.Blocks);
            
            reply = await _serverService.RequestBlocks(new BlocksRequest(), BuildServerCallContext());
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
        
        [Fact]
        public async Task Disconnect_ShouldRemovePeer_Test()
        {
            await _serverService.Disconnect(new DisconnectReason(), BuildServerCallContext(new Metadata {{ GrpcConstants.PubkeyMetadataKey, NetworkTestConstants.FakePubkey2}}));
            Assert.Empty(_peerPool.GetPeers(true));
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
            await _nodeManager.AddNodeAsync(node);
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
        public async Task CheckHealth_Test()
        {
            var pingResult = await _serverService.CheckHealth(new HealthCheckRequest(), BuildServerCallContext());
            pingResult.ShouldBe(new HealthCheckReply());
        }
    }
}