using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.Collections;
using AElf.Kernel;
using AElf.Kernel.EventMessages;
using AElf.Network;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Eventing;
using AElf.Network.Peers;
using AElf.Node.AElfChain;
using AElf.Node.EventMessages;
using AElf.Synchronization.BlockSynchronization;
using AElf.Synchronization.EventMessages;
using Easy.MessageHub;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

[assembly: InternalsVisibleTo("AElf.Network.Tests")]

namespace AElf.Node.Protocol
{
    public class NetworkManager : INetworkManager, ISingletonDependency
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

        private readonly IPeerManager _peerManager;
        public ILogger<NetworkManager> Logger { get; set; }
        private readonly IBlockSynchronizer _blockSynchronizer;
        private readonly INodeService _nodeService;

        private readonly List<IPeer> _peers;

        private BoundedByteArrayQueue _lastBlocksReceived;
        private BoundedByteArrayQueue _lastTxReceived;
        private BoundedByteArrayQueue _temp = new BoundedByteArrayQueue(10);

        private readonly BlockingPriorityQueue<PeerMessageReceivedArgs> _incomingJobs;

        private ulong _currentLibNum;

        internal IPeer CurrentSyncSource { get; set; }
        internal int LocalHeight;

        internal int UnlinkableHeaderIndex;

        private readonly object _syncLock = new object();

        public NetworkManager(IPeerManager peerManager, IBlockSynchronizer blockSynchronizer, INodeService nodeService)
        {
            _incomingJobs = new BlockingPriorityQueue<PeerMessageReceivedArgs>();
            _peers = new List<IPeer>();

            _peerManager = peerManager;
            Logger = NullLogger<NetworkManager>.Instance;
            _blockSynchronizer = blockSynchronizer;
            _nodeService = nodeService;

            peerManager.PeerEvent += OnPeerAdded;

            MessageHub.Instance.Subscribe<TransactionAddedToPool>(inTx =>
            {
                if (inTx?.Transaction == null)
                {
                    Logger.LogWarning("[event] Transaction null.");
                    return;
                }

                var txHash = inTx.Transaction.GetHashBytes();

                if (txHash != null)
                    _lastTxReceived.Enqueue(txHash);

                if (_peers == null || !_peers.Any())
                    return;

                BroadcastMessage(AElfProtocolMsgType.NewTransaction, inTx.Transaction.Serialize());
            });

            MessageHub.Instance.Subscribe<BlockMined>(inBlock =>
            {
                if (inBlock?.Block == null)
                {
                    Logger.LogWarning("[event] Block null.");
                    return;
                }

                byte[] blockHash = inBlock.Block.GetHash().DumpByteArray();

                if (blockHash != null)
                    _lastBlocksReceived.Enqueue(blockHash);

                AnnounceBlock((Block) inBlock.Block);

                Logger.LogInformation(
                    $"Block produced, announcing {blockHash.ToHex()} to peers ({string.Join("|", _peers)}) with " +
                    $"{inBlock.Block.Body.TransactionsCount} txs, block height {inBlock.Block.Header.Height}.");

                LocalHeight = (int) inBlock.Block.Height;
            });

            MessageHub.Instance.Subscribe<BlockAccepted>(inBlock =>
            {
                if (inBlock?.Block == null)
                {
                    Logger.LogWarning("[event] Block null.");
                    return;
                }

                // Note - This should not happen during header this
                if (UnlinkableHeaderIndex != 0)
                    return;

                IBlock acceptedBlock = inBlock.Block;

                var blockHash = acceptedBlock.GetHash().DumpByteArray();

                // todo TEMP 
                if (_temp.Contains(blockHash))
                    return;

                _temp.Enqueue(blockHash);

                if (blockHash != null)
                    _lastBlocksReceived.Enqueue(blockHash);

                Logger.LogTrace(
                    $"Block accepted, announcing {blockHash.ToHex()} to peers ({string.Join("|", _peers)}), " +
                    $"block height {acceptedBlock.Header.Height}.");

                lock (_syncLock)
                {
                    if (CurrentSyncSource == null || !CurrentSyncSource.IsSyncingHistory)
                    {
                        AnnounceBlock(acceptedBlock);
                    }
                }
            });

            MessageHub.Instance.Subscribe<BlockExecuted>(inBlock =>
                {
                    if (inBlock?.Block == null)
                    {
                        Logger.LogWarning("[event] Block null.");
                        return;
                    }

                // Note - This should not happen during header this
                if (UnlinkableHeaderIndex != 0)
                    return;
            
                LocalHeight = (int) inBlock.Block.Height;
            
                DoNext(inBlock.Block);
            });

            MessageHub.Instance.Subscribe<UnlinkableHeader>(unlinkableHeaderMsg =>
            {
                if (unlinkableHeaderMsg?.Header == null)
                {
                    Logger.LogWarning("[event] message or header null.");
                    return;
                }

                // The reception of this event means that the chain has discovered 
                // that the current block it is trying to execute (height H) is 
                // not linkable to the block we have at H-1.

                // At this point we stop all current syncing activities and repetedly 
                // download previous headers to the block we couldn't link (in other 
                // word "his branch") until we find a linkable block (at wich point 
                // the HeaderAccepted event should be launched.

                // note that when this event is called, our knowledge of the local 
                // height doesn't mean much.

                lock (_syncLock)
                {
                    // If this is already != 0, it means that the previous batch of 
                    // headers was not linked and that more need to be requested. 
                    if (UnlinkableHeaderIndex != 0)
                    {
                        // Set state with the first occurence of the unlinkable block
                        UnlinkableHeaderIndex = (int) unlinkableHeaderMsg.Header.Height;
                    }
                    else
                    {
                        CurrentSyncSource = null;

                        // Reset all syncing operations
                        foreach (var peer in _peers)
                        {
                            peer.ResetSync();
                        }

                        LocalHeight = 0;
                    }
                }

                Logger.LogTrace($"Header unlinkable, height {unlinkableHeaderMsg.Header.Height}.");

                // Use the peer with the highest target to request headers.
                IPeer target = _peers
                    .Where(p => p.KnownHeight >= (int) unlinkableHeaderMsg.Header.Height)
                    .OrderByDescending(p => p.KnownHeight)
                    .FirstOrDefault();

                if (target == null)
                {
                    Logger.LogWarning("[event] no peers to sync from.");
                    return;
                }

                target.RequestHeaders((int) unlinkableHeaderMsg.Header.Height, DefaultHeaderRequestCount);
            });

            MessageHub.Instance.Subscribe<HeaderAccepted>(header =>
            {
                if (header?.Header == null)
                {
                    Logger.LogWarning("[event] message or header null.");
                    return;
                }

                if (UnlinkableHeaderIndex != 0)
                {
                    Logger.LogWarning("[event] HeaderAccepted but network module not in recovery mode.");
                    return;
                }

                if (CurrentSyncSource != null)
                {
                    // todo possible sync reset
                    Logger.LogWarning("[event] current sync source is not null");
                    return;
                }

                lock (_syncLock)
                {
                    // Local height reset 
                    LocalHeight = (int) header.Header.Height - 1;

                    // Reset Unlinkable header state
                    UnlinkableHeaderIndex = 0;

                    Logger.LogTrace(
                        $"[event] header accepted, height {header.Header.Height}, local height reset to {header.Header.Height - 1}.");

                    // Use the peer with the highest target that is higher than our height.
                    IPeer target = _peers
                        .Where(p => p.KnownHeight > LocalHeight)
                        .OrderByDescending(p => p.KnownHeight)
                        .FirstOrDefault();

                    if (target == null)
                    {
                        Logger.LogWarning("[event] no peers to sync from.");
                        return;
                    }

                    CurrentSyncSource = target;
                    CurrentSyncSource?.SyncToHeight(LocalHeight + 1, target.KnownHeight);

                    FireSyncStateChanged(true);
                }
            });

            MessageHub.Instance.Subscribe<ChainInitialized>(inBlock =>
            {
                _peerManager.Start();
                Task.Run(StartProcessingIncoming).ConfigureAwait(false);
            });

            MessageHub.Instance.Subscribe<MinorityForkDetected>(inBlock => { OnMinorityForkDetected(); });

            MessageHub.Instance.Subscribe<NewLibFound>(msg =>
            {
                _currentLibNum = msg.Height;
                Logger.LogDebug($"Network lib updated : {_currentLibNum}.");
            });

            MessageHub.Instance.Subscribe<BlockRejected>(msg =>
            {
                // the block that is currently been synced has failed 
                lock (_syncLock)
                {
                    if (msg?.Block == null)
                        Logger.LogWarning("[event] Block rejected: block null.");
                    
                    if (CurrentSyncSource == null)
                        Logger.LogWarning("Unexpected situation, rejected a block but no peer is currently syncing.");

                    if (CurrentSyncSource != null && CurrentSyncSource.IsSyncingHistory)
                    {
                        // If we're currently syncing history
                        var next = _peers.FirstOrDefault(p => p != CurrentSyncSource && p.KnownHeight > LocalHeight);

                        if (next == null)
                        {
                            Logger.LogWarning("Rejected block but no other peer to sync from. ");
                            return;
                        }
                        
                        CurrentSyncSource = next;
                        CurrentSyncSource.SyncToHeight(LocalHeight + 1, next.KnownHeight);
                    }
                    else
                    {
                        CurrentSyncSource.ResetSync();
                        SyncNext(); // get another peer to sync from
                    }
                }
            });
            
            MessageHub.Instance.Subscribe<BlockLinked>(msg =>
            {
                if (msg?.Block == null)
                {
                    Logger.LogWarning($"{nameof(BlockLinked)} Message or block was null.");
                    return;
                }
                
                DoNext(msg.Block);
            });
        }

        private void DoNext(IBlock acceptedBlock)
        {
            var blockHash = acceptedBlock.GetHash().DumpByteArray();
            var blockHeight = acceptedBlock.Header.Height;
            
            lock (_syncLock)
            {
                if (CurrentSyncSource == null)
                {
                    Logger.LogWarning("Unexpected situation, executed a block but no peer is currently syncing.");
                }
                else if (!CurrentSyncSource.IsSyncing)
                {
                    Logger.LogWarning($"{CurrentSyncSource} is sync source but not in sync state.");
                }
                else if (CurrentSyncSource.IsSyncingHistory)
                {
                    if ((int)blockHeight != CurrentSyncSource.CurrentlyRequestedHeight)
                        Logger.LogWarning($"{CurrentSyncSource} unexpected situation, the block executed was not the exepected height.");
                
                    bool hasReqNext = CurrentSyncSource.SyncNextHistory();

                    if (hasReqNext)
                        return;
                
                    Logger.LogTrace($"{CurrentSyncSource} history blocks synced, local height {LocalHeight}.");
                    
                    CurrentSyncSource = null;

                    var next = _peers
                        .Where(p => !p.IsDisposed && p.AnyStashed && p.KnownHeight > LocalHeight)
                        .Select(p => new
                        {
                            PeerMinAnnouncement = p.GetLowestAnnouncement(),
                            Peer = p
                        })
                        .OrderBy(p => p.PeerMinAnnouncement)
                        .FirstOrDefault();

                    if (next == null)
                    {
                        Logger.LogWarning($"History blocks synced and no announcements to get.");
                    }
                    else if (LocalHeight+1 < next.PeerMinAnnouncement)
                    {
                        CurrentSyncSource = next.Peer;
                        Logger.LogDebug($"Re-sync because lowest peer, {CurrentSyncSource} is too high {next.PeerMinAnnouncement}.");
                            
                        CurrentSyncSource.SyncToHeight(LocalHeight+1, next.PeerMinAnnouncement-1);
                        return;
                    }
                }
                else if (CurrentSyncSource.IsSyncingAnnounced)
                {
                    // we check if the hash of the accepted block is the one the sync source fetched 
                    if (!CurrentSyncSource.SyncedAnnouncement.Id.ToByteArray().BytesEqual(blockHash))
                        Logger.LogWarning($"Block {blockHash.ToHex()} accepted by the chain but not currently synced.");
                    
                    foreach (var peer in _peers)
                    {
                        // Clear the announcement or any previous announcement to not request 
                        // again.
                        peer.CleanAnnouncements((int)blockHeight);
                    }
                    
                    bool hasReqNext = CurrentSyncSource.SyncNextAnnouncement();

                    if (hasReqNext)
                        return;
                
                    Logger.LogTrace($"Catched up to announcements with {CurrentSyncSource}.");
                }
                
                SyncNext();
            }
        }

        private void OnMinorityForkDetected()
        {
            try
            {
                // reset everything inside the lock to keep a coherent state

                lock (_syncLock)
                {
                    CurrentSyncSource = null;
                    foreach (var peer in _peers)
                    {
                        peer.ResetSync();
                    }

                    _lastBlocksReceived.Clear();
                    _lastTxReceived.Clear();
                    _temp.Clear();

                    LocalHeight = (int) _currentLibNum;

                    // todo should be from a BP that is not part of the minority
                    IPeer syncPeer = _peers.Where(p => p.KnownHeight > LocalHeight).OrderByDescending(p => p.KnownHeight).FirstOrDefault();

                    if (syncPeer != null)
                    {
                        CurrentSyncSource = syncPeer;
                        CurrentSyncSource.SyncToHeight(LocalHeight + 1, syncPeer.KnownHeight);

                        FireSyncStateChanged(true);
                    }
                    else
                    {
                        Logger.LogWarning("Could not find any peer to sync to.");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while reset for minority fork.");
            }
        }

        private void FireSyncStateChanged(bool newState)
        {
            Task.Run(() => MessageHub.Instance.Publish(new SyncStateChanged(newState)));
        }

        internal void SyncNext()
        {
            var oldSyncSource = CurrentSyncSource;
            CurrentSyncSource = null;

            // Try and find a peer with an anouncement that corresponds to the next block we need.
            foreach (var p in _peers.Where(p => p.AnyStashed && p != oldSyncSource))
            {
                if (p.SyncNextAnnouncement())
                {
                    CurrentSyncSource = p;

                    FireSyncStateChanged(true);
                    Logger.LogDebug($"Catching up with {p}.");

                    return;
                }
            }

            if (CurrentSyncSource != null)
            {
                Logger.LogError(
                    $"The current sync source {CurrentSyncSource} is not null even though sync should be finished.");
            }

            FireSyncStateChanged(false);

            Logger.LogDebug("Catched up all peers.");
        }

        /// <summary>
        /// This method start the server that listens for incoming
        /// connections and sets up the manager.
        /// </summary>
        public async Task Start()
        {
            // init the queue
            try
            {
                _lastBlocksReceived = new BoundedByteArrayQueue(MaxBlockHistory);
                _lastTxReceived = new BoundedByteArrayQueue(MaxTransactionHistory);

                LocalHeight = await _nodeService.GetCurrentBlockHeightAsync();

                Logger.LogInformation($"Network initialized at height {LocalHeight}.");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while initializing the network.");
                throw;
            }
        }

        public async Task Stop()
        {
            await _peerManager.Stop();
        }

        private void AnnounceBlock(IBlock block)
        {
            if (block?.Header == null)
            {
                Logger.LogError("Block or block header is null.");
                return;
            }

            if (_peers == null || !_peers.Any())
                return;

            try
            {
                Announce anc = new Announce
                {
                    Height = (int) block.Header.Height,
                    Id = ByteString.CopyFrom(block.GetHashBytes())
                };

                BroadcastMessage(AElfProtocolMsgType.Announcement, anc.ToByteArray());
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while announcing block.");
            }
        }

        #region Eventing

        private void OnPeerAdded(object sender, EventArgs eventArgs)
        {
            try
            {
                if (eventArgs is PeerEventArgs peer && peer.Peer != null)
                {
                    if (peer.Actiontype == PeerEventType.Added)
                    {
                        int peerHeight = peer.Peer.KnownHeight;

                        _peers.Add(peer.Peer);

                        peer.Peer.MessageReceived += HandleNewMessage;
                        peer.Peer.PeerDisconnected += ProcessClientDisconnection;
                        peer.Peer.RequestFailed += PeerOnRequestFailed;

                        // Sync if needed
                        lock (_syncLock)
                        {
                            if (CurrentSyncSource == null && LocalHeight < peerHeight)
                            {
                                CurrentSyncSource = peer.Peer;
                                CurrentSyncSource.SyncToHeight(LocalHeight + 1, peerHeight);

                                FireSyncStateChanged(true);
                            }
                        }
                    }
                    else
                    {
                        _peers.Remove(peer.Peer);

                        lock (_syncLock)
                        {
                            if (CurrentSyncSource != null && CurrentSyncSource.IsSyncingHistory)
                            {
                                IPeer nextHistSyncSource = _peers.FirstOrDefault(p => p.KnownHeight >= peer.Peer.SyncTarget);

                                if (nextHistSyncSource == null)
                                {
                                    Logger.LogWarning("No other peer to sync from.");
                                    return;
                                }

                                CurrentSyncSource = nextHistSyncSource;
                                nextHistSyncSource.SyncToHeight(LocalHeight+1, nextHistSyncSource.KnownHeight);
                            }
                            else
                            {
                                Logger.LogDebug($"Peer {peer.Peer} has been removed, trying to find another peer to sync.");
                                SyncNext();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogException(e, LogLevel.Error);
            }
        }

        private void PeerOnRequestFailed(object sender, EventArgs e)
        {
            if (sender is Peer p)
            {
                lock (_syncLock)
                {
                    if (CurrentSyncSource == null || CurrentSyncSource != p)
                        return;
                        
                    if (!CurrentSyncSource.IsSyncingHistory)
                    {
                        Logger.LogDebug($"About to reset {p} and sync to another.");
                        p.ResetSync();
                        SyncNext();
                    }
                    else
                    {
                        Logger.LogDebug($"Hist sync failed from {p} and sync history to another.");

                        IPeer next = _peers
                            .Where(peer => !peer.IsDisposed && peer != p && peer.KnownHeight > LocalHeight)
                            .OrderBy(peer => peer.KnownHeight)
                            .FirstOrDefault();

                        if (next != null)
                        {
                            next.SyncToHeight(LocalHeight+1, next.KnownHeight);
                            CurrentSyncSource = next;
                        }
                        else
                        {
                            Logger.LogWarning("Found no other to finish initial sync.");
                        }
                        
                        p.ResetSync();
                    }
                }
            }
            else
            {
                Logger.LogWarning("Peer was null.");
            }
        }

        /// <summary>
        /// Callback for when a Peer fires a <see cref="IPeer.PeerDisconnected"/> event. It unsubscribes
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
                peer.RequestFailed -= PeerOnRequestFailed;

                _peers.Remove(args.Peer);

                lock (_syncLock)
                {
                    if (CurrentSyncSource != null && CurrentSyncSource.IsSyncingHistory)
                    {
                        IPeer nextHistSyncSource = _peers.FirstOrDefault(p => p.KnownHeight >= args.Peer.SyncTarget);

                        if (nextHistSyncSource == null)
                        {
                            Logger.LogWarning("No other peer to sync from.");
                            return;
                        }

                        CurrentSyncSource = nextHistSyncSource;
                        nextHistSyncSource.SyncToHeight(LocalHeight+1, nextHistSyncSource.KnownHeight);
                    }
                    else
                    {
                        Logger.LogDebug($"Peer {args.Peer} has been removed, trying to find another peer to sync.");
                        SyncNext();
                    }
                }
            }
        }

        private void HandleNewMessage(object sender, EventArgs e)
        {
            if (e is PeerMessageReceivedArgs args)
            {
                if (args.Message.Type == (int) AElfProtocolMsgType.Block)
                {
                    // If we get a block deserialize it here to stop the request timer asap
                    var block = HandleBlockReception(args.Message.Payload, args.Peer);

                    if (block == null)
                        return;

                    args.Block = block;
                }
                else if (CurrentSyncSource != null && CurrentSyncSource.IsSyncingHistory &&
                         args.Message.Type == (int) AElfProtocolMsgType.NewTransaction)
                {
                    return;
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
                    Logger.LogError(e, "Error while processing incoming messages");
                }
            }
        }

        private async Task ProcessPeerMessage(PeerMessageReceivedArgs args)
        {
            if (args?.Peer == null)
            {
                Logger.LogWarning("Peer is invalid.");
                return;
            }

            if (args.Message?.Payload == null)
            {
                Logger.LogWarning($"Message from {args.Peer}, message/payload is null.");
                return;
            }

            AElfProtocolMsgType msgType = (AElfProtocolMsgType) args.Message.Type;

            switch (msgType)
            {
                case AElfProtocolMsgType.Announcement:
                    HandleAnnouncement(args.Message, args.Peer);
                    break;
                case AElfProtocolMsgType.Block:
                    MessageHub.Instance.Publish(new BlockReceived(args.Block));
                    break;
                case AElfProtocolMsgType.NewTransaction:
                    HandleNewTransaction(args.Message);
                    break;
                case AElfProtocolMsgType.Headers:
                    HandleHeaders(args.Message);
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

                Logger.LogDebug($"Send {blockHeaderList.Headers.Count} block headers start " +
                                $"from {blockHeaderList.Headers.FirstOrDefault()?.GetHash().ToHex()}, to node {args.Peer}.");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while during HandleBlockRequest.");
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
                    Logger.LogWarning($"Block not found {breq.Id?.ToByteArray().ToHex()}");
                    return;
                }

                Message req = NetRequestFactory.CreateMessage(AElfProtocolMsgType.Block, b.ToByteArray());

                if (args.Message.HasId)
                    req.Id = args.Message.Id;

                // Send response
                args.Peer.EnqueueOutgoing(req,
                    _ => { Logger.LogDebug($"Block sent {{ hash: {b.BlockHashToHex}, to: {args.Peer} }}"); });
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while during HandleBlockRequest.");
            }
        }

        private void HandleHeaders(Message msg)
        {
            try
            {
                BlockHeaderList blockHeaders = BlockHeaderList.Parser.ParseFrom(msg.Payload);
                MessageHub.Instance.Publish(new HeadersReceived(blockHeaders.Headers.ToList()));
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while handling header list.");
            }
        }

        private void HandleAnnouncement(Message msg, Peer peer)
        {
            try
            {
                Announce a = Announce.Parser.ParseFrom(msg.Payload);

                byte[] blockHash = a.Id.ToByteArray();

                peer.OnAnnouncementMessage(a); // todo - impr - move completely inside peer class.

                // If we already know about this block don't stash the announcement and return.
                if (_blockSynchronizer.GetBlockByHash(Hash.LoadByteArray(blockHash)) != null)
                {
                    Logger.LogDebug(
                        $"{peer} announced an already known block : {{ id: {blockHash.ToHex()}, height: {a.Height} }}");
                    return;
                }

                if (CurrentSyncSource != null && CurrentSyncSource.IsSyncingHistory &&
                    a.Height <= CurrentSyncSource.SyncTarget)
                {
                    Logger.LogTrace(
                        $"{peer} : ignoring announce {a.Height} because history sync will fetch (sync target {CurrentSyncSource.SyncTarget}).");
                    return;
                }

                if (UnlinkableHeaderIndex != 0)
                {
                    Logger.LogTrace($"{peer} : ignoring announce {a.Height} because we're syncing unlinkable.");
                    return;
                }

                lock (_syncLock)
                {
                    // Check that we're not missing any announcements
                    if (CurrentSyncSource == null && a.Height > LocalHeight+1 && _peers.All(p => !p.AnyStashed))
                    {
                        Logger.LogDebug($"Futur announcement {a.Height}.");
                        CurrentSyncSource = peer;
                        peer.SyncToHeight(LocalHeight+1, a.Height);
                    }
                    
                    // stash inside the lock because BlockAccepted will clear 
                    // announcements after the chain accepts
                    peer.StashAnnouncement(a);

                    if (CurrentSyncSource == null)
                    {
                        CurrentSyncSource = peer;
                        CurrentSyncSource.SyncNextAnnouncement();

                        FireSyncStateChanged(true);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while handling annoucement.");
            }
        }

        private void HandleNewTransaction(Message msg)
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
                Logger.LogError(e, "Error while handling new transaction reception.");
            }
        }

        private Block HandleBlockReception(byte[] serializedBlock, Peer peer)
        {
            try
            {
                Block block = Block.Parser.ParseFrom(serializedBlock);

                byte[] blockHash = block.GetHashBytes();

                peer.StopBlockTimer(block);

                if (_lastBlocksReceived.Contains(blockHash))
                {
                    Logger.LogWarning($"Block {blockHash.ToHex()} already in network cache.");
                    DoNext(block);
                    
                    return null;
                }

                _lastBlocksReceived.Enqueue(blockHash);

                return block;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while handling block reception");
            }

            return null;
        }

        #endregion

        /// <summary>
        /// This message broadcasts data to all of its peers. This creates and
        /// sends an object with the provided pay-load and message type.
        /// </summary>
        /// <param name="messageMsgType"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private void BroadcastMessage(AElfProtocolMsgType messageMsgType, byte[] payload)
        {
            if (_peers == null || !_peers.Any())
            {
                Logger.LogWarning("Cannot broadcast - no peers.");
                return;
            }

            try
            {
                Message message = NetRequestFactory.CreateMessage(messageMsgType, payload);

                foreach (var peer in _peers)
                    peer.EnqueueOutgoing(message);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while sending a message to the peers.");
            }
        }
    }
}