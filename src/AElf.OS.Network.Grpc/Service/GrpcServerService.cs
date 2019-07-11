using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Extensions;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;

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
        private readonly IPeerDiscoveryService _peerDiscoveryService;
        private readonly IHandshakeProvider _handshakeProvider;
        private readonly IPeerClientFactory _peerClientFactory;
        private readonly IConnectionInfoProvider _connectionInfoProvider;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcServerService> Logger { get; set; }

        public GrpcServerService(ISyncStateService syncStateService, IPeerPool peerPool, 
            IBlockchainService blockchainService, IPeerDiscoveryService peerDiscoveryService, 
            IHandshakeProvider handshakeProvider, IPeerClientFactory peerClientFactory, 
            IConnectionInfoProvider connectionInfoProvider)
        {
            _syncStateService = syncStateService;
            _peerPool = peerPool;
            _blockchainService = blockchainService;
            _peerDiscoveryService = peerDiscoveryService;
            _handshakeProvider = handshakeProvider;
            _peerClientFactory = peerClientFactory;
            _connectionInfoProvider = connectionInfoProvider;

            EventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<GrpcServerService>.Instance;
        }

        /// <summary>
        /// First step of the connect/auth process. Used to initiate a connection. The provided payload should be the
        /// clients authentication information. When receiving this call, protocol dictates you send the client your auth
        /// information. The response says whether or not you can connect.
        /// </summary>
        public override async Task<ConnectReply> Connect(ConnectRequest connectionRequest, ServerCallContext context)
        {
            // TODO limit the amount of connections per host and number of peers "connecting"
            Logger.LogTrace($"{context.Peer} has initiated a connection.");
            
            var peer = GrpcUrl.Parse(context.Peer);
            
            if (peer == null)
                return new ConnectReply { Error = ConnectError.InvalidPeer }; // TODO connect error
            
            if (ValidateConnectionInfo(connectionRequest.Info) != ConnectError.ConnectOk)
                return new ConnectReply { Error = ConnectError.ConnectionRefused };
            
            string pubKey = connectionRequest.Info.Pubkey.ToHex();
            
            var oldPeer = _peerPool.FindPeerByPublicKey(pubKey);
            if (oldPeer != null)
            {
                // TODO: Is this valid ? this is just discarding the previous connection
                Logger.LogDebug($"Cleaning up {oldPeer} before connecting.");
                await _peerPool.RemovePeerAsync(pubKey, false); //TODO report disconnect
            }

            // TODO: find a URI type to use
            var peerAddress = peer.IpAddress + ":" + connectionRequest.Info.ListeningPort;
            var grpcPeer = DialPeer(peerAddress, connectionRequest.Info);

            // If auth ok -> add it to our peers
            if (_peerPool.TryAddPeer(grpcPeer))
                Logger.LogDebug($"Added to pool {grpcPeer.Info.Pubkey}.");

            // todo handle case where add is false (edge case)
            
            var connectInfo = await _connectionInfoProvider.GetConnectionInfoAsync();

            return new ConnectReply { Info = connectInfo };
        }
        
        private ConnectError ValidateConnectionInfo(ConnectionInfo connectionInfo)
        {
            // verify chain id
            if (connectionInfo.ChainId != _blockchainService.GetChainId())
                return ConnectError.ChainMismatch;

            // verify protocol
            if (connectionInfo.Version != KernelConstants.ProtocolVersion)
                return ConnectError.ProtocolMismatch;
            
            if (NetworkOptions.MaxPeers != 0 && _peerPool.IsFull())
            {
                Logger.LogWarning($"Cannot add peer, there's currently {_peerPool.PeerCount} peers (max. {NetworkOptions.MaxPeers}).");
                return ConnectError.ConnectionRefused;
            }

            return ConnectError.ConnectOk;
        }
        
        private GrpcPeer DialPeer(string peerAddress, ConnectionInfo connectionInfo)
        {
            Logger.LogDebug($"Attempting to create channel to {peerAddress}");

            var (channel, client) = _peerClientFactory.CreateClientAsync(peerAddress);
            
            // TODO ping back (maybe do something on transient failure)
            
            return new GrpcPeer(channel, client, peerAddress, connectionInfo.ToPeerInfo(true));
        }
        
        public override async Task<HandshakeReply> DoHandshake(HandshakeRequest request, ServerCallContext context)
        {
            Logger.LogDebug($"Peer {context.GetPeerInfo()} has requested handshake data.");
            
            var error = ValidateHandshake(request.Handshake);

            if (error != HandshakeError.HandshakeOk)
            {
                Logger.LogWarning($"Handshake not valid: {error}");
                return new HandshakeReply { Error = error };
            }
            
            return new HandshakeReply { Handshake = await _handshakeProvider.GetHandshakeAsync() };
        }

        private HandshakeError ValidateHandshake(Handshake handshake)
        {
            if (handshake?.HandshakeData == null)
                return HandshakeError.InvalidHandshake;

            // verify signature
            var validData = CryptoHelper.VerifySignature(handshake.Signature.ToByteArray(),
                Hash.FromMessage(handshake.HandshakeData).ToByteArray(), handshake.HandshakeData.Pubkey.ToByteArray());
            
            if (!validData)
                return HandshakeError.WrongSignature;
            
            // verify authentication
            var pubKey = handshake.HandshakeData.Pubkey.ToHex();
            if (NetworkOptions.AuthorizedPeers == AuthorizedPeers.Authorized
                && !NetworkOptions.AuthorizedKeys.Contains(pubKey))
            {
                Logger.LogDebug($"{pubKey} not in the authorized peers.");
                return HandshakeError.NotListed;
            }

            return HandshakeError.HandshakeOk;
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