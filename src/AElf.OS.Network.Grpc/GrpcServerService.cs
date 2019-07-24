using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Extensions;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Grpc
{
    /// <summary>
    /// Implementation of the grpc generated service. It contains the rpc methods
    /// exposed to peers.
    /// </summary>
    public class GrpcServerService : PeerService.PeerServiceBase
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        private readonly ISyncStateService _syncStateService;
        private readonly IPeerPool _peerPool;
        private readonly IBlockchainService _blockchainService;
        private readonly IAccountService _accountService;
        private readonly IPeerDiscoveryService _peerDiscoveryService;
        private readonly ITaskQueueManager _taskQueueManager;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcServerService> Logger { get; set; }

        public GrpcServerService(ISyncStateService syncStateService, IPeerPool peerPool, 
            IBlockchainService blockchainService, IAccountService accountService, 
            IPeerDiscoveryService peerDiscoveryService, ITaskQueueManager taskQueueManager)
        {
            _syncStateService = syncStateService;
            _peerPool = peerPool;
            _blockchainService = blockchainService;
            _accountService = accountService;
            _peerDiscoveryService = peerDiscoveryService;
            _taskQueueManager = taskQueueManager;

            EventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<GrpcServerService>.Instance;
        }

        /// <summary>
        /// First step of the connect/auth process. Used to initiate a connection. The provided payload should be the
        /// clients authentication information. When receiving this call, protocol dictates you send the client your auth
        /// information. The response says whether or not you can connect.
        /// </summary>
        public override async Task<ConnectReply> Connect(Handshake handshake, ServerCallContext context)
        {
            Logger.LogTrace($"{context.Peer} has initiated a connection request.");
            
            var peer = GrpcUrl.Parse(context.Peer);
            
            if (peer == null)
                return new ConnectReply { Error = AuthError.InvalidPeer };

            if (NetworkOptions.MaxPeers != 0)
            {
                int peerCount = _peerPool.GetPeers(true).Count;
                if (peerCount >= NetworkOptions.MaxPeers)
                {
                    Logger.LogWarning($"Cannot add peer, there's currently {peerCount} peers (max. {NetworkOptions.MaxPeers}).");
                    return new ConnectReply { Error = AuthError.ConnectionRefused };
                }
            }

            var error = ValidateHandshake(handshake);

            if (error != AuthError.None)
            {
                Logger.LogWarning($"Handshake not valid: {error}");
                return new ConnectReply {Error = error};
            }
            
            var pubKey = handshake.HandshakeData.Pubkey.ToHex();
            var oldPeer = _peerPool.FindPeerByPublicKey(pubKey);

            if (oldPeer != null)
            {
                Logger.LogDebug($"Cleaning up {oldPeer} before connecting.");
                await _peerPool.RemovePeerAsync(pubKey, false);
            }

            // TODO: find a URI type to use
            var peerAddress = peer.IpAddress + ":" + handshake.HandshakeData.ListeningPort;
            var grpcPeer = DialPeer(peerAddress, handshake);

            // send our credentials
            var hsk = await _peerPool.GetHandshakeAsync();
            
            // If auth ok -> add it to our peers
            if (_peerPool.AddPeer(grpcPeer))
                Logger.LogDebug($"Added to pool {grpcPeer.Info.Pubkey}.");
            
            _taskQueueManager.CreateQueue(grpcPeer.Info.Pubkey);

            // todo handle case where add is false (edge case)

            return new ConnectReply { Handshake = hsk };
        }

        private GrpcPeer DialPeer(string peerAddress, Handshake handshake)
        {
            Logger.LogDebug($"Attempting to create channel to {peerAddress}");

            Channel channel = new Channel(peerAddress, ChannelCredentials.Insecure, new List<ChannelOption>
            {
                new ChannelOption(ChannelOptions.MaxSendMessageLength, GrpcConstants.DefaultMaxSendMessageLength),
                new ChannelOption(ChannelOptions.MaxReceiveMessageLength, GrpcConstants.DefaultMaxReceiveMessageLength)
            });
            
            var client = new PeerService.PeerServiceClient(channel.Intercept(metadata =>
            {
                metadata.Add(GrpcConstants.PubkeyMetadataKey, AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync()).ToHex());
                return metadata;
            }).Intercept(new RetryInterceptor()));

            if (channel.State != ChannelState.Ready)
            {
                var c = channel.WaitForStateChangedAsync(channel.State);
            }

            var pubKey = handshake.HandshakeData.Pubkey.ToHex();
            
            var connectionInfo = new PeerInfo
            {
                Pubkey = pubKey,
                ProtocolVersion = handshake.HandshakeData.Version,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                StartHeight = handshake.BestChainBlockHeader.Height,
                IsInbound = true,
                LibHeightAtHandshake = handshake.LibBlockHeight
            };
            
            return new GrpcPeer(channel, client, peerAddress, connectionInfo);
        }

        private AuthError ValidateHandshake(Handshake handshake)
        {
            if (handshake?.HandshakeData == null)
                return AuthError.InvalidHandshake;

            // verify chain id
            if (handshake.HandshakeData.ChainId != _blockchainService.GetChainId())
                return AuthError.ChainMismatch;

            // verify protocol
            if (handshake.HandshakeData.Version != KernelConstants.ProtocolVersion)
                return AuthError.ProtocolMismatch;

            // verify signature
            var validData = CryptoHelper.VerifySignature(handshake.Signature.ToByteArray(),
                Hash.FromMessage(handshake.HandshakeData).ToByteArray(), handshake.HandshakeData.Pubkey.ToByteArray());
            
            if (!validData)
                return AuthError.WrongSignature;
            
            // verify authentication
            var pubKey = handshake.HandshakeData.Pubkey.ToHex();
            if (NetworkOptions.AuthorizedPeers == AuthorizedPeers.Authorized
                && !NetworkOptions.AuthorizedKeys.Contains(pubKey))
            {
                Logger.LogDebug($"{pubKey} not in the authorized peers.");
                return AuthError.ConnectionRefused ;
            }

            return AuthError.None;
        }
        
        public override async Task<VoidReply> BlockBroadcastStream(IAsyncStreamReader<BlockWithTransactions> requestStream, ServerCallContext context)
        {
            await requestStream.ForEachAsync(r =>
            {
                _ = EventBus.PublishAsync(new BlockReceivedEvent(r,context.GetPublicKey()));
                return Task.CompletedTask;
            });
            
            return new VoidReply();
        }

        public override Task<FinalizeConnectReply> FinalizeConnect(Handshake request, ServerCallContext context)
        {
            var peer = _peerPool.FindPeerByPublicKey(context.GetPublicKey());
            
            if (peer == null)
                return Task.FromResult(new FinalizeConnectReply { Success = false });

            peer.IsConnected = true;
            
            return Task.FromResult(new FinalizeConnectReply { Success = true });
        }

        public override async Task<VoidReply> AnnouncementBroadcastStream(IAsyncStreamReader<BlockAnnouncement> requestStream, ServerCallContext context)
        {
            await requestStream.ForEachAsync(async r => await ProcessAnnouncement(r, context));
            return new VoidReply();
        }

        public Task ProcessAnnouncement(BlockAnnouncement announcement, ServerCallContext context)
        {
            if (announcement?.BlockHash == null)
            {
                Logger.LogError($"Received null announcement or header from {context.GetPeerInfo()}.");
                return Task.CompletedTask;
            }
            
            Logger.LogDebug($"Received announce {announcement.BlockHash} from {context.GetPeerInfo()}.");

            var peer = _peerPool.FindPeerByPublicKey(context.GetPublicKey());
            peer?.ProcessReceivedAnnouncement(announcement);

            _ = EventBus.PublishAsync(new AnnouncementReceivedEventData(announcement, context.GetPublicKey()));
            
            return Task.CompletedTask;
        }
        
        public override async Task<VoidReply> TransactionBroadcastStream(IAsyncStreamReader<Transaction> requestStream, ServerCallContext context)
        {
            await requestStream.ForEachAsync(async tx => await ProcessTransaction(tx, context));
            return new VoidReply();
        }

        /// <summary>
        /// This method is called when another peer broadcasts a transaction.
        /// </summary>
        public override async Task<VoidReply> SendTransaction(Transaction tx, ServerCallContext context)
        {
            await ProcessTransaction(tx, context);
            return new VoidReply();
        }

        private async Task ProcessTransaction(Transaction tx, ServerCallContext context)
        {
            var chain = await _blockchainService.GetChainAsync();
            
            // if this transaction's ref block is a lot higher than our chain 
            // then don't participate in p2p network
            if (tx.RefBlockNumber > chain.LongestChainHeight + NetworkConstants.DefaultInitialSyncOffset)
                return;
            
            _ = EventBus.PublishAsync(new TransactionsReceivedEvent { Transactions = new List<Transaction> {tx} });
        }

        /// <summary>
        /// This method is called when a peer wants to broadcast an announcement.
        /// </summary>
        public override async Task<VoidReply> SendAnnouncement(BlockAnnouncement an, ServerCallContext context)
        {
            await ProcessAnnouncement(an, context);
            return new VoidReply();
        }

        /// <summary>
        /// This method returns a block. The parameter is a <see cref="BlockRequest"/> object, if the value
        /// of <see cref="BlockRequest.Hash"/> is not null, the request is by ID, otherwise it will be
        /// by height.
        /// </summary>
        public override async Task<BlockReply> RequestBlock(BlockRequest request, ServerCallContext context)
        {
            if (request == null || request.Hash == null || _syncStateService.SyncState != SyncState.Finished) 
                return new BlockReply();
            
            Logger.LogDebug($"Peer {context.GetPeerInfo()} requested block {request.Hash}.");

            var block = await _blockchainService.GetBlockWithTransactionsByHash(request.Hash);
            
            if (block == null)
                Logger.LogDebug($"Could not find block {request.Hash} for {context.GetPeerInfo()}.");

            return new BlockReply { Block = block };
        }

        public override async Task<BlockList> RequestBlocks(BlocksRequest request, ServerCallContext context)
        {
            if (request == null || request.PreviousBlockHash == null || _syncStateService.SyncState != SyncState.Finished)
                return new BlockList();
            
            Logger.LogDebug($"Peer {context.GetPeerInfo()} requested {request.Count} blocks from {request.PreviousBlockHash}.");

            var blockList = new BlockList();
            
            var blocks = await _blockchainService.GetBlocksWithTransactions(request.PreviousBlockHash, request.Count);

            if (blocks == null)
                return blockList;
            
            blockList.Blocks.AddRange(blocks);

            if (blockList.Blocks.Count != request.Count)
                Logger.LogTrace($"Replied with {blockList.Blocks.Count} blocks for request {request}");

            if (NetworkOptions.CompressBlocksOnRequest)
            {
                var headers = new Metadata{new Metadata.Entry(GrpcConstants.GrpcRequestCompressKey, GrpcConstants.GrpcGzipConst)};
                await context.WriteResponseHeadersAsync(headers);
            }
            
            return blockList;
        }

        public override async Task<Handshake> UpdateHandshake(UpdateHandshakeRequest request, ServerCallContext context)
        {
            Logger.LogDebug($"Peer {context.GetPeerInfo()} has requested handshake data.");
            
            return await _peerPool.GetHandshakeAsync();
        }

        public override async Task<NodeList> GetNodes(NodesRequest request, ServerCallContext context)
        {
            if (request == null) 
                return new NodeList();
            
            Logger.LogDebug($"Peer {context.GetPeerInfo()} requested {request.MaxCount} nodes.");
            
            var nodes = await _peerDiscoveryService.GetNodesAsync(request.MaxCount);
            
            Logger.LogDebug($"Sending {nodes.Nodes.Count} to {context.GetPeerInfo()}.");

            return nodes;
        }

        /// <summary>
        /// Clients should call this method to disconnect explicitly.
        /// </summary>
        public override async Task<VoidReply> Disconnect(DisconnectReason request, ServerCallContext context)
        {
            Logger.LogDebug($"Peer {context.GetPeerInfo()} has sent a disconnect request.");
            
            await _peerPool.RemovePeerAsync(context.GetPublicKey(), false);
            
            return new VoidReply();
        }
    }
}