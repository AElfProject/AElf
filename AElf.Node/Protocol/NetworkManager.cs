using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.ChainController.TxMemPool;
using AElf.Common;
using AElf.Common.Attributes;
using AElf.Common.Collections;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Network;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Eventing;
using AElf.Network.Peers;
using AElf.Node.EventMessages;
using AElf.Node.Protocol.Events;
using Easy.MessageHub;
using Google.Protobuf;
using NLog;
using Org.BouncyCastle.Crypto.Engines;

[assembly:InternalsVisibleTo("AElf.Network.Tests")]
namespace AElf.Node.Protocol
{
    [LoggerName(nameof(NetworkManager))]
    public class NetworkManager : INetworkManager
    {
        #region Settings

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
        public event EventHandler RequestFailed;
        public event EventHandler BlockReceived;
        public event EventHandler TransactionsReceived;

        private readonly ITxPoolService _transactionPoolService;
        private readonly IPeerManager _peerManager;
        private readonly IChainService _chainService;
        private readonly ILogger _logger;
        
        private readonly List<IPeer> _peers = new List<IPeer>();

        private readonly Object _pendingRequestsLock = new Object();
        private readonly List<TimeoutRequest> _pendingRequests;

        private BoundedByteArrayQueue _lastBlocksReceived;
        private BoundedByteArrayQueue _lastTxReceived;

        private readonly BlockingPriorityQueue<PeerMessageReceivedArgs> _incomingJobs;
        
        private IPeer CurrentSyncSource { get; set; }
        private int _localHeight = 0;

        private bool _isSyncing = false;
        
        private Hash _chainId;
        
        private List<byte[]> _minedBlocks = new List<byte[]>();

        public NetworkManager(ITxPoolService transactionPoolService, IPeerManager peerManager, IChainService chainService, ILogger logger)
        {
            _incomingJobs = new BlockingPriorityQueue<PeerMessageReceivedArgs>();
            _pendingRequests = new List<TimeoutRequest>();

            _transactionPoolService = transactionPoolService;
            _peerManager = peerManager;
            _chainService = chainService;
            _logger = logger;
            
            _chainId = new Hash { Value = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId)) };
            
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
                    
                    // _logger?.Trace($"[event] tx added to the pool {txHash?.ToHex()}.");
                });
            
            MessageHub.Instance.Subscribe<BlockAddedToSet>(inBlock => 
                {
                    if (inBlock?.Block == null)
                    {
                        _logger?.Warn("[event] Block null.");
                        return;
                    }

                    byte[] blockHash = inBlock.Block.GetHash().DumpByteArray();

                    if (blockHash != null)
                        _lastBlocksReceived.Enqueue(blockHash);
                    
                    AnnounceBlock((Block)inBlock.Block);
                    
                    _minedBlocks.Add(blockHash);
                    
                    _logger?.Trace($"Block produced, announcing \"{blockHash.ToHex()}\" to peers. Block height: [{inBlock.Block.Header.Index}].");
                    
                    _localHeight++;
                });
            
            MessageHub.Instance.Subscribe<BlockExecuted>(inBlock => 
            {
                if (inBlock?.Block == null)
                {
                    _logger?.Warn("[event] Block null.");
                    return;
                }

                byte[] blockHash = inBlock.Block.GetHash().DumpByteArray();

                if (blockHash != null)
                    _lastBlocksReceived.Enqueue(blockHash);
                    
                _logger?.Trace($"Block executed, announcing \"{blockHash.ToHex()}\" to peers. Block height: [{inBlock.Block.Header.Index}].");
                
                _localHeight++;
            });
            
            MessageHub.Instance.Subscribe<BlockAccepted>(inBlock => 
            {
                if (inBlock?.Block == null)
                {
                    _logger?.Warn("[event] Block null.");
                    return;
                }

                var blockHash = inBlock.Block.GetHash().DumpByteArray();

                if (blockHash != null)
                    _lastBlocksReceived.Enqueue(blockHash);
                    
                _logger?.Trace($"Block accepted, announcing \"{blockHash.ToHex()}\" to peers. Block height: [{inBlock.Block.Header.Index}].");
                
                _localHeight++;

                //CurrentSyncSource?.OnNewBlockAccepted(inBlock.Block);

                bool syncFinished = true;
                foreach (var peer in _peers)
                {
                    peer.OnNewBlockAccepted(inBlock.Block);

                    if (peer.AnySyncing())
                    {
                        syncFinished = false;
                    }
                }

                AnnounceBlock(inBlock.Block);

                if (syncFinished)
                    SetSyncState(false);
            });
        }

        private void SetSyncState(bool newState)
        {
            if (_isSyncing == newState)
                return;
            
            _isSyncing = newState;
            MessageHub.Instance.Publish(new SyncStateChanged(newState));
            
            _logger?.Trace($"Sync state changed {_isSyncing}.");
        }
        
        /// <summary>
        /// This method start the server that listens for incoming
        /// connections and sets up the manager.
        /// </summary>
        public void Start()
        {
            // init the queue
            _lastBlocksReceived = new BoundedByteArrayQueue(MaxBlockHistory);
            _lastTxReceived = new BoundedByteArrayQueue(MaxTransactionHistory);
                
            _localHeight = (int) _chainService.GetBlockChain(_chainId).GetCurrentBlockHeightAsync().Result;
            
            _logger?.Trace($"Network initialized at height {_localHeight}.");
            
            _peerManager.Start();
            
            Task.Run(() => StartProcessingIncoming()).ConfigureAwait(false);
        }
        
        private void AnnounceBlock(IBlock block)
        {
            Announce anc = new Announce();
            anc.Height = (int)block.Header.Index;
            anc.Id = ByteString.CopyFrom(block.GetHashBytes());

            BroadcastMessage(AElfProtocolMsgType.Announcement, anc.ToByteArray());
        }
        
        #region Eventing

        private void OnPeerAdded(object sender, EventArgs eventArgs)
        {
            if (eventArgs is PeerEventArgs peer && peer.Peer != null && peer.Actiontype == PeerEventType.Added)
            {
                int peerHeight = peer.Peer.KnownHeight;
                
                // If we haven't sync the historical blocks, start a sync session
                if (CurrentSyncSource == null && _localHeight < peerHeight)
                    StartSync(peer.Peer, _localHeight+1, peerHeight);
                    
                _peers.Add(peer.Peer);

                peer.Peer.SyncFinished += PeerOnSyncFinished ;
                peer.Peer.MessageReceived += HandleNewMessage;
                peer.Peer.PeerDisconnected += ProcessClientDisconnection;
            }
        }

        private void PeerOnSyncFinished(object sender, EventArgs e)
        {
            bool syncFinished = true;
            foreach (var peer in _peers)
            {
                if (peer.AnySyncing())
                {
                    syncFinished = false;
                }
            }
            
            if (syncFinished)
                SetSyncState(false);
        }

        private void StartSync(IPeer peer, int start, int target)
        {
            CurrentSyncSource = peer;
            peer.Sync(start, target);
                    
            _logger?.Trace($"Sync started from peer {CurrentSyncSource}, from {start} to {target}.");
                    
            SetSyncState(true);
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
                //peer.SyncFinished -= PeerOnSyncFinished;
                
                _peers.Remove(args.Peer);
            }
        }
        
        private void HandleNewMessage(object sender, EventArgs e)
        {
            if (e is PeerMessageReceivedArgs args)
            {
                _incomingJobs.Enqueue(args, 0);
            }
        }

        #endregion
        
        #region Message processing

        private void StartProcessingIncoming()
        {
            while (true)
            {
                try
                {
                    PeerMessageReceivedArgs msg = _incomingJobs.Take();
                    ProcessPeerMessage(msg);
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Error while processing incoming messages");
                }
            }
        }
        
        private void ProcessPeerMessage(PeerMessageReceivedArgs args)
        {
            if (args?.Peer == null || args.Message == null)
            {
                _logger.Warn("Invalid message from peer.");
                return;
            }
            
            AElfProtocolMsgType msgType = (AElfProtocolMsgType) args.Message.Type;
            
            switch (msgType)
            {
                case AElfProtocolMsgType.Announcement:
                    HandleAnnoucement(msgType, args.Message, args.Peer);
                    break;
                // New blocks and requested blocks will be added to the sync
                // Subscribe to the BlockReceived event.
                case AElfProtocolMsgType.NewBlock:
                case AElfProtocolMsgType.Block:
                    HandleBlockReception(msgType, args.Message, args.Peer);
                    break;
                // Transactions requested from the sync.
                case AElfProtocolMsgType.Transactions:
                    HandleTransactionsMessage(msgType, args.Message, args.Peer);
                    break;
                // New transaction issue from a broadcast.
                case AElfProtocolMsgType.NewTransaction:
                    HandleNewTransaction(msgType, args.Message, args.Peer);
                    break;
            }
            
            // Re-fire the event for higher levels if needed.
            BubbleMessageReceivedEvent(args);
        }
        
        private void BubbleMessageReceivedEvent(PeerMessageReceivedArgs args)
        {
            MessageReceived?.Invoke(this, new NetMessageReceivedEventArgs(args.Message, args));
        }

        private void HandleTransactionsMessage(AElfProtocolMsgType msgType, Message msg, Peer peer)
        {
            try
            {
                if (msg.HasId)
                    GetAndClearRequest(msg);
                
                TransactionList txList = TransactionList.Parser.ParseFrom(msg.Payload);
                // The sync should subscribe to this and add to pool
                //TransactionsReceived?.Invoke(this, new TransactionsReceivedEventArgs(txList, peer, msgType));
                
                // todo launch tx event
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while deserializing transaction list.");
            }
        }
        
        private void HandleAnnoucement(AElfProtocolMsgType msgType, Message msg, Peer peer)
        {
            try
            {
                Announce a = Announce.Parser.ParseFrom(msg.Payload);

                byte[] blockHash = a.Id.ToByteArray();
                IBlock bbh = _chainService.GetBlockByHash(new Hash { Value = ByteString.CopyFrom(blockHash) });
                
                _logger?.Debug($"{peer} annouced {blockHash.ToHex()} [{a.Height}] " + (bbh == null ? "(unknown)" : "(known)"));

                if (bbh == null && _minedBlocks.Any(m => m.BytesEqual(blockHash)))
                    ;

                if (bbh != null)
                    return;
                
                SetSyncState(true);
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

                // Add to the pool; if valid, rebroadcast.
                var addResult = _transactionPoolService.AddTxAsync(tx).GetAwaiter().GetResult();
                
                if (addResult == TxValidation.TxInsertionAndBroadcastingError.Success)
                {
                    _logger?.Debug($"Transaction (new) with hash {txHash.ToHex()} added to the pool.");

                    MessageHub.Instance.Publish(new TxReceived(tx));
                    
                    //foreach (var p in _peers.Where(p => !p.Equals(peer)))
                    //    p.EnqueueOutgoing(msg);
                }
                else
                {
                    _logger?.Debug($"New transaction {tx.GetHash()} from {peer} not added to the pool: {addResult}");
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while handling new transaction reception");
            }
        }

        private void HandleBlockReception(AElfProtocolMsgType msgType, Message msg, Peer peer)
        {
            try
            {
                Block block = Block.Parser.ParseFrom(msg.Payload);
            
                byte[] blockHash = block.GetHashBytes();

                if (_lastBlocksReceived.Contains(blockHash))
                    return;
                
                _lastBlocksReceived.Enqueue(blockHash);

                peer.OnBlockReceived(block);
                              
                MessageHub.Instance.Publish(new BlockReceived(block));
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while handling block reception");
            }
        }

        #endregion
        
        public void QueueTransactionRequest(List<byte[]> transactionHashes, IPeer hint)
        {
            try
            {
                IPeer selectedPeer = hint ?? _peers.FirstOrDefault();
            
                if(selectedPeer == null)
                    return;
            
                // Create the message
                TxRequest br = new TxRequest();
                br.TxHashes.Add(transactionHashes.Select(h => ByteString.CopyFrom(h)).ToList());
                var msg = NetRequestFactory.CreateMessage(AElfProtocolMsgType.TxRequest, br.ToByteArray());
                
                // Identification
                msg.HasId = true;
                msg.Id = Guid.NewGuid().ToByteArray();
            
                // Select peer for request
                TimeoutRequest request = new TimeoutRequest(transactionHashes, msg, RequestTimeout);
                request.MaxRetryCount = RequestMaxRetry;
            
                lock (_pendingRequestsLock)
                {
                    _pendingRequests.Add(request);
                }
            
                request.RequestTimedOut += RequestOnRequestTimedOut;
                request.TryPeer(selectedPeer);
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while requesting transactions.");
            }
        }
        
//        public void QueueBlockRequestByIndex(int index)
//        {
//            if (index <= 0)
//            {
//                _logger?.Warn($"Cannot request block because height {index} is not valid.");
//                return;
//            }
//            
//            try
//            {
//                IPeer selectedPeer = _peers.FirstOrDefault();
//            
//                if(selectedPeer == null)
//                    return;
//                
//                // Create the request object
//                BlockRequest br = new BlockRequest { Height = index };
//                Message message = NetRequestFactory.CreateMessage(AElfProtocolMsgType.RequestBlock, br.ToByteArray());
//
//                if (message.Payload == null)
//                {
//                    _logger?.Warn($"Request for block at height {index} failed because payload is null.");
//                    return;   
//                }
//                
//                // Select peer for request
//                TimeoutRequest request = new TimeoutRequest(index, message, RequestTimeout);
//                request.MaxRetryCount = RequestMaxRetry;
//                
//                lock (_pendingRequestsLock)
//                {
//                    _pendingRequests.Add(request);
//                }
//
//                request.TryPeer(selectedPeer);
//            }
//            catch (Exception e)
//            {
//                _logger?.Error(e, $"Error while requesting block for index {index}.");
//            }
//        }

        /// <summary>
        /// Callback called when the requests internal timer has executed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RequestOnRequestTimedOut(object sender, EventArgs eventArgs)
        {
            if (sender == null)
            {
                _logger?.Warn("Request timeout - sender null.");
                return;
            }

            if (sender is TimeoutRequest req)
            {
                _logger?.Trace("Request timeout : " + req.IsBlockRequest + $", with {req.Peer} and timeout : {TimeSpan.FromMilliseconds(req.Timeout)}.");
                
                if (req.IsTxRequest && req.TransactionHashes != null && req.TransactionHashes.Any())
                {
                    _logger?.Trace("Hashes : [" + string.Join(", ", req.TransactionHashes.Select(kvp => kvp.ToHex())) + "]");
                }
                
                if (req.HasReachedMaxRetry)
                {
                    lock (_pendingRequestsLock)
                    {
                        _pendingRequests.Remove(req);
                    }
                    
                    req.RequestTimedOut -= RequestOnRequestTimedOut;
                    FireRequestFailed(req);
                    return;
                }
                
                IPeer nextPeer = _peers.FirstOrDefault(p => !p.Equals(req.Peer));
                
                if (nextPeer != null)
                {
                    _logger?.Trace("Trying another peer : " + req.RequestMessage.RequestLogString + $", next : {nextPeer}.");
                    req.TryPeer(nextPeer);
                }
            }
            else
            {
                _logger?.Trace("Request timeout - sender wrong type.");
            }
        }

        private void FireRequestFailed(TimeoutRequest req)
        {
            RequestFailedEventArgs reqFailedEventArgs = new RequestFailedEventArgs
            {
                RequestMessage = req.RequestMessage,
                TriedPeers = req.TriedPeers.ToList()
            };

            _logger?.Warn("Request failed : " + req.RequestMessage.RequestLogString + $" after {req.TriedPeers.Count} tries. Max tries : {req.MaxRetryCount}.");
                    
            RequestFailed?.Invoke(this, reqFailedEventArgs);
        }
        
        internal TimeoutRequest GetAndClearRequest(Message msg)
        {
            if (msg == null)
            {
                _logger?.Warn("Handle message : peer or message null.");
                return null;
            }
            
            try
            {
                TimeoutRequest request;
                
                lock (_pendingRequestsLock)
                {
                    request = _pendingRequests.FirstOrDefault(r => r.Id.BytesEqual(msg.Id));
                }

                if (request != null)
                {
                    request.RequestTimedOut -= RequestOnRequestTimedOut;
                    request.Stop();
                    
                    lock (_pendingRequestsLock)
                    {
                        _pendingRequests.Remove(request);
                    }
                    
                    if (request.IsTxRequest && request.TransactionHashes != null && request.TransactionHashes.Any())
                    {
                        _logger?.Debug("Matched : [" + string.Join(", ", request.TransactionHashes.Select(kvp => kvp.ToHex()).ToList()) + "]");
                    }
                }
                else
                {
                    _logger?.Warn($"Request not found. Index : {msg.Id.ToHex()}.");
                }

                return request;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Exception while handling request message.");
                return null;
            }
        }

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
                    catch (Exception e) { }
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while sending a message to the peers.");
            }

            return count;
        }

        public int GetPendingRequestCount()
        {
            return _pendingRequests.Count;
        }
    }
}