using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.Attributes;
using AElf.Common.Collections;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Miner.EventMessages;
using AElf.Network;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Eventing;
using AElf.Network.Peers;
using AElf.Node.AElfChain;
using AElf.Node.EventMessages;
using AElf.Node.Protocol.Events;
using AElf.Synchronization.BlockSynchronization;
using AElf.Synchronization.EventMessages;
using Akka.Dispatch;
using Easy.MessageHub;
using Google.Protobuf;
using NLog;

[assembly:InternalsVisibleTo("AElf.Network.Tests")]

namespace AElf.Node.Protocol
{
    [LoggerName(nameof(NetworkManager))]
    public class NetworkManager : INetworkManager
    {
        #region Settings

        public const int DefaultHeaderRequestCount = 3;
        public const int DefaultMaxBlockHistory = 15;
        public const int DefaultMaxTransactionHistory = 20;

        public const int DefaultRequestTimeout = 2000;
        public const int DefaultRequestMaxRetry = TimeoutRequest.DefaultMaxRetry;

        public int MaxBlockHistory { get; set; } = DefaultMaxBlockHistory;
        public int MaxTransactionHistory { get; set; } = DefaultMaxTransactionHistory;

        public int RequestTimeout { get; set; } = DefaultRequestTimeout;
        public int RequestMaxRetry { get; set; } = DefaultRequestMaxRetry;

        #endregion

        public event EventHandler MessageReceived;
        
        private readonly IPeerManager _peerManager;
        private readonly IChainService _chainService;
        private readonly ILogger _logger;
        private readonly IBlockSynchronizer _blockSynchronizer;
        private readonly INodeService _nodeService;

        private readonly List<IPeer> _peers = new List<IPeer>();

        private BoundedByteArrayQueue _lastBlocksReceived;
        private BoundedByteArrayQueue _lastTxReceived;
        private BoundedByteArrayQueue _lastAnnouncementsReceived;

        private readonly BlockingPriorityQueue<PeerMessageReceivedArgs> _incomingJobs;

        private IPeer _currentSyncSource;
        private int _localHeight = 0;

        private bool _isSyncing = false;

        private readonly Hash _chainId;
        
        private readonly object _syncLock = new object();

        public NetworkManager(IPeerManager peerManager, IBlockSynchronizer blockSynchronizer, INodeService nodeService, ILogger logger)
        {
            _incomingJobs = new BlockingPriorityQueue<PeerMessageReceivedArgs>();

            _peerManager = peerManager;
            _logger = logger;
            _blockSynchronizer = blockSynchronizer;
            _nodeService = nodeService;

            _chainId = new Hash
            {
                Value = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(ChainConfig.Instance.ChainId))
            };

            peerManager.PeerEvent += OnPeerAdded;

            MessageHub.Instance.Subscribe<TransactionAddedToPool>(async inTx =>
            {
                if (inTx?.Transaction == null)
                {
                    _logger?.Warn("[event] Transaction null.");
                    return;
                }

                var txHash = inTx.Transaction.GetHashBytes();

                if (txHash != null)
                    _lastTxReceived.Enqueue(txHash);

                await BroadcastMessage(AElfProtocolMsgType.NewTransaction, inTx.Transaction.Serialize());
            });

            MessageHub.Instance.Subscribe<BlockMined>(inBlock =>
            {
                if (inBlock?.Block == null)
                {
                    _logger?.Warn("[event] Block null.");
                    return;
                }

                byte[] blockHash = inBlock.Block.GetHash().DumpByteArray();

                if (blockHash != null)
                    _lastBlocksReceived.Enqueue(blockHash);

                AnnounceBlock((Block) inBlock.Block);

                _logger?.Info($"Block produced, announcing {blockHash.ToHex()} to peers ({string.Join("|", _peers)}) with " +
                              $"{inBlock.Block.Body.TransactionsCount} txs, block height {inBlock.Block.Header.Index}.");

                _localHeight++;
            });

            MessageHub.Instance.Subscribe<BlockExecuted>(inBlock =>
            {
                if (inBlock?.Block == null)
                {
                    _logger?.Warn("[event] Block null.");
                    return;
                }
                
                _localHeight++;

                var blockHash = inBlock.Block.GetHash().DumpByteArray();
                var blockHeight = inBlock.Block.Header.Index;

                if (blockHash != null)
                    _lastBlocksReceived.Enqueue(blockHash);

                _logger?.Trace($"Block accepted, announcing {blockHash.ToHex()} to peers ({string.Join("|", _peers)}), " +
                               $"block height {inBlock.Block.Header.Index}.");

                AnnounceBlock(inBlock.Block);

                lock (_syncLock)
                {
                    if (_currentSyncSource == null)
                    {
                        _logger?.Warn("Unexpected situation, executed a block but no peer is currently syncing.");
                    }
                    else if (!_currentSyncSource.IsSyncing)
                    {
                        _logger?.Warn("We have a sync source but he is not in a syncing state.");
                    }
                    else if (_currentSyncSource.IsSyncingHistory)
                    {
                        if ((int)blockHeight != _currentSyncSource.CurrentlyRequestedHeight)
                            _logger?.Warn("Unexpected situation, the block executed was not the exepected height.");
                    
                        bool hasReqNext = _currentSyncSource.SyncNextHistory();

                        if (hasReqNext)
                            return;
                    
                        _logger?.Trace("History block synced.");
                    
                        if (_currentSyncSource.AnyStashed)
                        {
                            if (_currentSyncSource.SyncNextAnnouncement())
                            {
                                _logger?.Trace("The current peer has some unsynced announcements - started sync.");
                                return;
                            }
                        
                            _logger?.Warn("Failed to start announcement sync.");
                        }
                    }
                    else if (_currentSyncSource.IsSyncingAnnounced)
                    {
                        // todo check hash
                        bool hasReqNext = _currentSyncSource.SyncNextAnnouncement();

                        if (hasReqNext)
                            return;
                    
                        _logger?.Trace("Catched up to announcements.");
                    }
                
                    _currentSyncSource = null;
                    SetSyncState(false);

                    var newPeer = _peers.FirstOrDefault(p => p.AnyStashed);

                    if (newPeer != null)
                    {
                        newPeer.SyncNextAnnouncement();
                        SetSyncState(true);
                        _logger?.Debug($"Catching up with {newPeer} ");
                        return;
                    }

                    _logger?.Debug("Catched up all peers.");
                }
            });

            MessageHub.Instance.Subscribe<UnlinkableHeader>(unlinkableHeaderMsg =>
            {
                if (unlinkableHeaderMsg?.Header == null)
                {
                    _logger?.Warn("[event] message or header null.");
                    return;
                }

                IPeer target = _currentSyncSource ??
                               _peers.FirstOrDefault(p => p.KnownHeight >= (int) unlinkableHeaderMsg.Header.Index);

                if (target == null)
                {
                    _logger?.Warn("[event] no peers to sync from.");
                    return;
                }

                target.RequestHeaders((int) unlinkableHeaderMsg.Header.Index, DefaultHeaderRequestCount);
            });

            MessageHub.Instance.Subscribe<HeaderAccepted>(header =>
            {
                if (header?.Header == null)
                {
                    _logger?.Warn("[event] message or header null.");
                    return;
                }

                IPeer target = _currentSyncSource ??
                               _peers.FirstOrDefault(p => p.KnownHeight >= (int) header.Header.Index);

                if (target == null)
                {
                    _logger?.Warn("[event] no peers to sync from.");
                    return;
                }

                // StartSync(target, (int) header.Header.Index, target.KnownHeight);
                // todo re-implement this logic
                // todo current height is lower here
            });

            MessageHub.Instance.Subscribe<ChainInitialized>(inBlock =>
            {
                _peerManager.Start();
                Task.Run(StartProcessingIncoming).ConfigureAwait(false);
            });
        }

        private void SetSyncState(bool newState)
        {
            if (_isSyncing == newState)
                return;

            _isSyncing = newState;
            Task.Run(() => MessageHub.Instance.Publish(new SyncStateChanged(newState)));

            _logger?.Trace($"Sync state changed {_isSyncing}.");
        }

        /// <summary>
        /// This method start the server that listens for incoming
        /// connections and sets up the manager.
        /// </summary>
        public async Task Start()
        {
            // init the queue
            _lastBlocksReceived = new BoundedByteArrayQueue(MaxBlockHistory);
            _lastTxReceived = new BoundedByteArrayQueue(MaxTransactionHistory);
            _lastAnnouncementsReceived = new BoundedByteArrayQueue(MaxBlockHistory);

            _localHeight = await _nodeService.GetCurrentBlockHeightAsync();

            _logger?.Info($"Network initialized at height {_localHeight}.");
        }

        private void AnnounceBlock(IBlock block)
        {
            if (block?.Header == null)
            {
                _logger?.Error("Block or block header is null.");
                return;
            }

            try
            {
                Announce anc = new Announce();
                anc.Height = (int) block.Header.Index;
                anc.Id = ByteString.CopyFrom(block.GetHashBytes());

                byte[] serializedMsg = anc.ToByteArray();
                Message packet = NetRequestFactory.CreateMessage(AElfProtocolMsgType.Announcement, serializedMsg);

                BroadcastMessage(AElfProtocolMsgType.Announcement, anc.ToByteArray());
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while announcing block.");
            }
        }

        #region Eventing

        private void OnPeerAdded(object sender, EventArgs eventArgs)
        {
            if (eventArgs is PeerEventArgs peer && peer.Peer != null && peer.Actiontype == PeerEventType.Added)
            {
                int peerHeight = peer.Peer.KnownHeight;
                
                _peers.Add(peer.Peer);

                peer.Peer.MessageReceived += HandleNewMessage;
                peer.Peer.PeerDisconnected += ProcessClientDisconnection;

                // Sync if needed
                lock (_syncLock)
                {
                    if (_currentSyncSource == null && _localHeight < peerHeight)
                    {
                        _currentSyncSource = peer.Peer;
                        _currentSyncSource.SyncToHeight(_localHeight + 1, peerHeight);
                        
                        SetSyncState(true);
                    }
                }
            }
        }

        /// <summary>
        /// Callback for when a Peer fires a <see cref="PeerDisconnected"/> event. It unsubscribes
        /// the manager from the events and removes it from the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessClientDisconnection(object sender, EventArgs e)
        {
            if (sender != null && e is PeerDisconnectedArgs args && args.Peer != null)
            {
                IPeer peer = args.Peer;

                peer.MessageReceived -= HandleNewMessage;
                peer.PeerDisconnected -= ProcessClientDisconnection;

                _peers.Remove(args.Peer);
            }
        }

        private void HandleNewMessage(object sender, EventArgs e)
        {
            if (e is PeerMessageReceivedArgs args)
            {
                if (args.Message.Type == (int)AElfProtocolMsgType.Block)
                {
                    // If we get a block deserialize it here to stop the request timer asap
                    var block = HandleBlockReception(args.Message.Payload, args.Peer);

                    if (block == null)
                        return;
                    
                    args.Block = block;
                }
                
                _incomingJobs.Enqueue(args, 0);
            }
        }

        #endregion

        #region Message processing

        private async Task StartProcessingIncoming()
        {
            while (true)
            {
                try
                {
                    PeerMessageReceivedArgs msg = _incomingJobs.Take();
                    await ProcessPeerMessage(msg);
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Error while processing incoming messages");
                }
            }
        }

        private async Task ProcessPeerMessage(PeerMessageReceivedArgs args)
        {
            if (args?.Peer == null)
            {
                _logger.Warn("Peer is invalid.");
                return;
            }
            
            if (args.Message?.Payload == null)
            {
                _logger?.Warn($"Message from {args.Peer}, message/payload is null.");
                return;
            }

            AElfProtocolMsgType msgType = (AElfProtocolMsgType) args.Message.Type;

            switch (msgType)
            {
                case AElfProtocolMsgType.Announcement:
                    HandleAnnouncement(msgType, args.Message, args.Peer);
                    break;
                case AElfProtocolMsgType.Block:
                    MessageHub.Instance.Publish(new BlockReceived(args.Block));
                    break;
                // New transaction issue from a broadcast.
                case AElfProtocolMsgType.NewTransaction:
                    HandleNewTransaction(msgType, args.Message, args.Peer);
                    break;
                case AElfProtocolMsgType.Headers:
                    HandleHeaders(msgType, args.Message, args.Peer);
                    break;
                case AElfProtocolMsgType.RequestBlock:
                    await HandleBlockRequestJob(args);
                    break;
                case AElfProtocolMsgType.HeaderRequest:
                    await HandleHeaderRequest(args);
                    break;
            }
        }

        private async Task HandleHeaderRequest(PeerMessageReceivedArgs args)
        {
            try
            {
                var hashReq = BlockHeaderRequest.Parser.ParseFrom(args.Message.Payload);
                
                var blockHeaderList = await _nodeService.GetBlockHeaderList((ulong) hashReq.Height, hashReq.Count);
                
                var req = NetRequestFactory.CreateMessage(AElfProtocolMsgType.Headers, blockHeaderList.ToByteArray());
                
                if (args.Message.HasId)
                    req.Id = args.Message.Id;

                args.Peer.EnqueueOutgoing(req);

                _logger?.Debug($"Send {blockHeaderList.Headers.Count} block headers start " +
                               $"from {blockHeaderList.Headers.FirstOrDefault()?.GetHash().DumpHex()}, to node {args.Peer}.");
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while during HandleBlockRequest.");
            }
        }

        private async Task HandleBlockRequestJob(PeerMessageReceivedArgs args)
        { 
            try
            {
                var breq = BlockRequest.Parser.ParseFrom(args.Message.Payload);
                
                Block b;
                
                if (breq.Id != null && breq.Id.Length > 0)
                {
                    b = await _nodeService.GetBlockFromHash(breq.Id.ToByteArray());
                }
                else
                {
                    b = await _nodeService.GetBlockAtHeight(breq.Height);
                }

                if (b == null)
                {
                    _logger?.Warn($"Block not found {breq.Id?.ToByteArray().ToHex()}");
                    return;
                }
                
                Message req = NetRequestFactory.CreateMessage(AElfProtocolMsgType.Block, b.ToByteArray());
                
                if (args.Message.HasId)
                    req.Id = args.Message.Id;

                // Send response
                args.Peer.EnqueueOutgoing(req, (_) =>
                {
                    _logger?.Debug($"Block sent {{ hash: {b.BlockHashToHex}, to: {args.Peer} }}");
                });
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while during HandleBlockRequest.");
            }
        }

        private void HandleHeaders(AElfProtocolMsgType msgType, Message msg, Peer peer)
        {
            try
            {
                BlockHeaderList blockHeaders = BlockHeaderList.Parser.ParseFrom(msg.Payload);
                MessageHub.Instance.Publish(new HeadersReceived(blockHeaders.Headers.ToList()));
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while handling header list.");
            }
        }

        private void HandleAnnouncement(AElfProtocolMsgType msgType, Message msg, Peer peer)
        {
            try
            {
                Announce a = Announce.Parser.ParseFrom(msg.Payload);

                byte[] blockHash = a.Id.ToByteArray();

                if (_lastAnnouncementsReceived.Contains(blockHash))
                    return;

                _lastAnnouncementsReceived.Enqueue(blockHash);

                IBlock bbh = _blockSynchronizer.GetBlockByHash(new Hash { Value = ByteString.CopyFrom(blockHash) });

                _logger?.Debug($"Peer {peer} annouced block {blockHash.ToHex()} height {a.Height} " + (bbh == null ? "(unknown)" : "(known)"));

                if (bbh != null)
                    return;
                
                peer.StashAnnouncement(a);

                //SetSyncState(true);
                lock (_syncLock)
                {
                    if (_currentSyncSource == null)
                    {
                        _currentSyncSource = peer;
                        _currentSyncSource.SyncNextAnnouncement();
                        
                        SetSyncState(true);
                    }
                }
                
                // todo move completely inside peer class.
                peer.OnAnnouncementMessage(a);
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while handling annoucement.");
            }
        }

        private void HandleNewTransaction(AElfProtocolMsgType msgType, Message msg, Peer peer)
        {
            try
            {
                Transaction tx = Transaction.Parser.ParseFrom(msg.Payload);

                byte[] txHash = tx.GetHashBytes();

                if (_lastTxReceived.Contains(txHash))
                    return;

                _lastTxReceived.Enqueue(txHash);

                MessageHub.Instance.Publish(new TxReceived(tx));
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while handling new transaction reception.");
            }
        }

        private Block HandleBlockReception(byte[] serializedBlock, Peer peer)
        {
            try
            {
                Block block = Block.Parser.ParseFrom(serializedBlock);

                byte[] blockHash = block.GetHashBytes();

                if (_lastBlocksReceived.Contains(blockHash))
                {
                    _logger.Warn("Block already in network cache");
                    return null;
                }

                _lastBlocksReceived.Enqueue(blockHash);

                peer.OnBlockReceived(block);

                return block;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while handling block reception");
            }

            return null;
        }

        #endregion

        public async Task<int> BroadcastBlock(byte[] hash, byte[] payload)
        {
            _lastBlocksReceived.Enqueue(hash);
            return await BroadcastMessage(AElfProtocolMsgType.NewBlock, payload);
        }

        /// <summary>
        /// This message broadcasts data to all of its peers. This creates and
        /// sends a <see cref="AElfPacketData"/> object with the provided pay-
        /// load and message type.
        /// </summary>
        /// <param name="messageMsgType"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public async Task<int> BroadcastMessage(AElfProtocolMsgType messageMsgType, byte[] payload)
        {
            try
            {
                Message packet = NetRequestFactory.CreateMessage(messageMsgType, payload);
                return BroadcastMessage(packet);
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while sending a message to the peers.");
                return 0;
            }
        }

        public int BroadcastMessage(Message message)
        {
            if (_peers == null || !_peers.Any())
                return 0;

            int count = 0;

            try
            {
                foreach (var peer in _peers)
                {
                    try
                    {
                        peer.EnqueueOutgoing(message); //todo
                        count++;
                    }
                    catch (Exception e)
                    {
                        _logger?.Error(e, "Error while enqueue outgoing message.");
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while sending a message to the peers.");
            }

            return count;
        }
    }
}