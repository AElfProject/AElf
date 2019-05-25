using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Application;
using AElf.Kernel.Node.Infrastructure;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Events;
using AElf.OS.Network.Extensions;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Secp256k1Net;
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
        
        private readonly IPeerPool _peerPool;
        private readonly IBlockchainService _blockChainService;
        private readonly IAccountService _accountService;
        private readonly IBlockChainNodeStateService _blockChainNodeStateService;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcServerService> Logger { get; set; }

        private NetworkOptions NetworkOptions => NetOpts.Value;
        public IOptionsSnapshot<NetworkOptions> NetOpts { get; set; }

        public GrpcServerService(IPeerPool peerPool, IBlockchainService blockChainService, IAccountService accountService, 
            IBlockChainNodeStateService blockChainNodeStateService)
        {
            _peerPool = peerPool;
            _blockChainService = blockChainService;
            _accountService = accountService;
            _blockChainNodeStateService = blockChainNodeStateService;

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

            if (handshake?.HskData == null)
                return new ConnectReply { Err = AuthError.InvalidHandshake };

            // verify chain id
            if (handshake.HskData.ChainId != _blockChainService.GetChainId())
                return new ConnectReply { Err = AuthError.ChainMismatch };

            // verify protocol
            if (handshake.HskData.Version != KernelConstants.ProtocolVersion)
                return new ConnectReply { Err = AuthError.ProtocolMismatch };

            // verify signature
            var validData = CryptoHelpers.VerifySignature(handshake.Signature.ToByteArray(),
                Hash.FromMessage(handshake.HskData).ToByteArray(), handshake.HskData.PublicKey.ToByteArray());
            
            if (!validData)
                return new ConnectReply { Err = AuthError.WrongSig };
            
            var peer = GrpcUrl.Parse(context.Peer);
            
            if (peer == null)
                return new ConnectReply { Err = AuthError.InvalidPeer };
            
            var pubKey = handshake.HskData.PublicKey.ToHex();
            
            var oldPeer = _peerPool.FindPeerByPublicKey(pubKey);

            if (oldPeer != null)
            {
                Logger.LogDebug($"Cleaning up {oldPeer} before connecting.");
                await _peerPool.RemovePeerAsync(pubKey, false);
            }

            // TODO: find a URI type to use
            var peerAddress = peer.IpAddress + ":" + handshake.HskData.ListeningPort;
            Logger.LogDebug($"Attempting to create channel to {peerAddress}");

            Channel channel = new Channel(peerAddress, ChannelCredentials.Insecure, new List<ChannelOption>
            {
                new ChannelOption(ChannelOptions.MaxSendMessageLength, GrpcConsts.DefaultMaxSendMessageLength),
                new ChannelOption(ChannelOptions.MaxReceiveMessageLength, GrpcConsts.DefaultMaxReceiveMessageLength)
            });
            
            var client = new PeerService.PeerServiceClient(channel.Intercept(metadata =>
            {
                metadata.Add(GrpcConsts.PubkeyMetadataKey, AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync()).ToHex());
                return metadata;
            }));

            if (channel.State != ChannelState.Ready)
            {
                var c = channel.WaitForStateChangedAsync(channel.State);
            }
            
            var grpcPeer = new GrpcPeer(channel, client, pubKey, peerAddress, handshake.HskData.Version,
                DateTime.UtcNow.ToTimestamp().Seconds, handshake.Header.Height);

            // send our credentials
            var hsk = await _peerPool.GetHandshakeAsync();
            
            // If auth ok -> add it to our peers
            _peerPool.AddPeer(grpcPeer);

            return new ConnectReply { Handshake = hsk };
        }

        /// <summary>
        /// This method is called when another peer broadcasts a transaction.
        /// </summary>
        public override Task<VoidReply> SendTransaction(Transaction tx, ServerCallContext context)
        {
            if (_blockChainNodeStateService.IsNodeSyncing()) 
                return Task.FromResult(new VoidReply());
            
            _ = EventBus.PublishAsync(new TransactionsReceivedEvent { Transactions = new List<Transaction> {tx} });

            return Task.FromResult(new VoidReply());
        }

        /// <summary>
        /// This method is called when a peer wants to broadcast an announcement.
        /// </summary>
        public override Task<VoidReply> Announce(PeerNewBlockAnnouncement an, ServerCallContext context)
        {
            if (an?.BlockHash == null || an?.BlockTime == null)
            {
                Logger.LogError($"Received null announcement or header from {context.GetPeerInfo()}.");
                return Task.FromResult(new VoidReply());
            }

            var peerInPool = _peerPool.FindPeerByPublicKey(context.GetPublicKey());
            peerInPool?.HandlerRemoteAnnounce(an);

            Logger.LogDebug($"Received announce {an.BlockHash} from {context.GetPeerInfo()}.");
            
            _ = EventBus.PublishAsync(new AnnouncementReceivedEventData(an, context.GetPublicKey()));

            return Task.FromResult(new VoidReply());
        }

        /// <summary>
        /// This method returns a block. The parameter is a <see cref="BlockRequest"/> object, if the value
        /// of <see cref="BlockRequest.Id"/> is not null, the request is by ID, otherwise it will be
        /// by height.
        /// </summary>
        public override async Task<BlockReply> RequestBlock(BlockRequest request, ServerCallContext context)
        {
            if (request == null || request.Hash == null) 
                return new BlockReply();
            
            Logger.LogDebug($"Peer {context.GetPeerInfo()} requested block {request.Hash}.");

            var block = await _blockChainService.GetBlockWithTransactionsByHash(request.Hash);
            
            if (block == null)
                Logger.LogDebug($"Could not find block {request.Hash} for {context.GetPeerInfo()}.");

            return new BlockReply { Block = block };
        }

        public override async Task<BlockList> RequestBlocks(BlocksRequest request, ServerCallContext context)
        {
            if (request == null || request.PreviousBlockHash == null || _blockChainNodeStateService.IsNodeSyncing()) 
                return new BlockList();
            
            Logger.LogDebug($"Peer {context.GetPeerInfo()} requested {request.Count} blocks from {request.PreviousBlockHash}.");

            var blockList = new BlockList();
            
            var blocks = await _blockChainService.GetBlocksWithTransactions(request.PreviousBlockHash, request.Count);

            if (blocks == null)
                return blockList;
            
            blockList.Blocks.AddRange(blocks);

            if (blockList.Blocks.Count != request.Count)
                Logger.LogTrace($"Replied with {blockList.Blocks.Count} blocks for request {request}");

            if (NetworkOptions.CompressBlocksOnRequest)
            {
                var headers = new Metadata{new Metadata.Entry(GrpcConsts.GrpcRequestCompressKey, GrpcConsts.GrpcGzipConst)};
                await context.WriteResponseHeadersAsync(headers);
            }
            
            return blockList;
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