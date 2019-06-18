using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Events;
using AElf.OS.Network.Extensions;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
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
        
        private readonly IPeerPool _peerPool;
        private readonly IBlockchainService _blockchainService;
        private readonly IAccountService _accountService;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcServerService> Logger { get; set; }

        public GrpcServerService(IPeerPool peerPool, IBlockchainService blockchainService, IAccountService accountService)
        {
            _peerPool = peerPool;
            _blockchainService = blockchainService;
            _accountService = accountService;

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
                return new ConnectReply { Err = AuthError.InvalidPeer };

            if (NetworkOptions.MaxPeers != 0)
            {
                int peerCount = _peerPool.GetPeers(true).Count;
                if (peerCount >= NetworkOptions.MaxPeers)
                {
                    Logger.LogWarning($"Cannot add peer, there's currently {peerCount} peers (max. {NetworkOptions.MaxPeers}).");
                    return new ConnectReply { Err = AuthError.ConnectionRefused };
                }
            }

            var error = ValidateHandshake(handshake);

            if (error != AuthError.None)
            {
                Logger.LogWarning($"Handshake not valid: {error}");
                return new ConnectReply {Err = error};
            }
            
            var pubKey = handshake.HskData.PublicKey.ToHex();
            var oldPeer = _peerPool.FindPeerByPublicKey(pubKey);

            if (oldPeer != null)
            {
                Logger.LogDebug($"Cleaning up {oldPeer} before connecting.");
                await _peerPool.RemovePeerAsync(pubKey, false);
            }

            // TODO: find a URI type to use
            var peerAddress = peer.IpAddress + ":" + handshake.HskData.ListeningPort;
            var grpcPeer = DialPeer(peerAddress, handshake);

            // send our credentials
            var hsk = await _peerPool.GetHandshakeAsync();
            
            // If auth ok -> add it to our peers
            _peerPool.AddPeer(grpcPeer);

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

            var pubKey = handshake.HskData.PublicKey.ToHex();
            
            var connectionInfo = new GrpcPeerInfo
            {
                PublicKey = pubKey,
                PeerIpAddress = peerAddress,
                ProtocolVersion = handshake.HskData.Version,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                StartHeight = handshake.Header.Height,
                IsInbound = true
            };

            return new GrpcPeer(channel, client, connectionInfo);
        }

        private AuthError ValidateHandshake(Handshake handshake)
        {
            if (handshake?.HskData == null)
                return AuthError.InvalidHandshake;

            // verify chain id
            if (handshake.HskData.ChainId != _blockchainService.GetChainId())
                return AuthError.ChainMismatch;

            // verify protocol
            if (handshake.HskData.Version != KernelConstants.ProtocolVersion)
                return AuthError.ProtocolMismatch;

            // verify signature
            var validData = CryptoHelpers.VerifySignature(handshake.Signature.ToByteArray(),
                Hash.FromMessage(handshake.HskData).ToByteArray(), handshake.HskData.PublicKey.ToByteArray());
            
            if (!validData)
                return AuthError.WrongSig;
            
            // verify authentication
            var pubKey = handshake.HskData.PublicKey.ToHex();
            if (NetworkOptions.AuthorizedPeers == AuthorizedPeers.Authorized
                && !NetworkOptions.AuthorizedKeys.Contains(pubKey))
            {
                Logger.LogDebug($"{pubKey} not in the authorized peers.");
                return AuthError.ConnectionRefused ;
            }

            return AuthError.None;
        }

        /// <summary>
        /// This method is called when another peer broadcasts a transaction.
        /// </summary>
        public override async Task<VoidReply> SendTransaction(Transaction tx, ServerCallContext context)
        {
            var chain = await _blockchainService.GetChainAsync();
            
            // if this transaction's ref block is a lot higher than our chain 
            // then don't participate in p2p network
            if (tx.RefBlockNumber > chain.LongestChainHeight + NetworkConstants.DefaultMinBlockGapBeforeSync)
                return new VoidReply();
            
            _ = EventBus.PublishAsync(new TransactionsReceivedEvent
            {
                Transactions = new List<Transaction> {tx},
                
            });

            return new VoidReply();
        }

        /// <summary>
        /// This method is called when a peer wants to broadcast an announcement.
        /// </summary>
        public override Task<VoidReply> Announce(PeerNewBlockAnnouncement an, ServerCallContext context)
        {
            if (an?.BlockHash == null)
            {
                Logger.LogError($"Received null announcement or header from {context.GetPeerInfo()}.");
                return Task.FromResult(new VoidReply());
            }
            
            Logger.LogDebug($"Received announce {an.BlockHash} from {context.GetPeerInfo()}.");

            var peerInPool = _peerPool.FindPeerByPublicKey(context.GetPublicKey());
            peerInPool?.HandlerRemoteAnnounce(an);

            _ = EventBus.PublishAsync(new AnnouncementReceivedEvent(an, context.GetPublicKey()));
            
            return Task.FromResult(new VoidReply());
        }

        /// <summary>
        /// This method returns a block. The parameter is a <see cref="BlockRequest"/> object, if the value
        /// of <see cref="BlockRequest.Hash"/> is not null, the request is by ID, otherwise it will be
        /// by height.
        /// </summary>
        public override async Task<BlockReply> RequestBlock(BlockRequest request, ServerCallContext context)
        {
            if (request == null || request.Hash == null) 
                return new BlockReply();
            
            Logger.LogDebug($"Peer {context.GetPeerInfo()} requested block {request.Hash}.");

            var block = await _blockchainService.GetBlockWithTransactionsByHash(request.Hash);
            
            if (block == null)
                Logger.LogDebug($"Could not find block {request.Hash} for {context.GetPeerInfo()}.");

            return new BlockReply { Block = block };
        }

        public override async Task<BlockList> RequestBlocks(BlocksRequest request, ServerCallContext context)
        {
            if (request == null || request.PreviousBlockHash == null) 
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