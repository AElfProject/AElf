using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Network.Connection;
using AElf.Network.Data;
using Easy.MessageHub;
using Google.Protobuf;

namespace AElf.Network.Peers
{
    public class PendingBlock
    {
        public int BlockNum { get; set; }
        public byte[] BlockId { get; set; }
        
        public bool IsValidating { get; set; }
        public bool IsRequesting { get; set; }

        public PendingBlock(byte[] blockId, bool isRequesting, bool isValidating)
        {
            BlockId = blockId;
            IsValidating = isValidating;
            IsRequesting = isRequesting;
        }
    }
    
    public partial class Peer
    {
        public const int DefaultRequestTimeout = 2000;
        
        public int RequestTimeout { get; set; } = DefaultRequestTimeout;
        public int MaxRequestRetries { get; set; } = 2;

        private const int GenesisHeight = 1;

        private object _blockLock = new object();
        //private readonly List<PendingBlock> _blocks;

        private object _blockReqLock = new object();
        private List<TimedBlockRequest> _blockRequests;

        private List<Announce> _announcements;
        
        private int _peerHeight = 0;

        /// <summary>
        /// When syncing history blocks this is the target height.
        /// </summary>
        private int _syncTarget = 0;
        
        /// <summary>
        /// When syncing history blocks this the currently requested height. 
        /// </summary>
        private int _requestedHeight = 0;

        /// <summary>
        /// True if syncing to height.
        /// </summary>
        public bool IsSyncingHistory => _syncTarget != 0;

        /// <summary>
        /// When syncing an annoucements, this is the current one.
        /// </summary>
        private Announce _syncedAnnouncement;

        /// <summary>
        /// True if syncing an annoucement.
        /// </summary>
        public bool IsSyncingAnnounced => _syncedAnnouncement != null;
        
        /// <summary>
        /// Property that is true if we're currently syncing blocks from this peer.
        /// 
        /// </summary>
        public bool IsSyncing => IsSyncingHistory || IsSyncingAnnounced;
        
        public int KnownHeight => _peerHeight; 
        public int CurrentlyRequestedHeight => _requestedHeight;
        public bool AnyStashed => _announcements.Any();

        /// <summary>
        /// Effectively triggers a sync session with this peer. The target height is specified
        /// as a parameter.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="target"></param>
        public void SyncToHeight(int start, int target)
        {

            if (start <= GenesisHeight)
            {
                throw new InvalidOperationException("Cannot sync genesis height or lower.");
            }
            
            if (IsSyncing)
            {
                throw new InvalidOperationException("The peer is already syncing, " +
                                                    "this method should only be used to trigger an initial sync.");
            }

            // set sync state
            _syncTarget = target;
            _requestedHeight = start;
            
            // request 
            RequestBlockByIndex(_requestedHeight);
            
            MessageHub.Instance.Publish(new ReceivingHistoryBlocksChanged(true));
        }

        /// <summary>
        /// This method will request the next block based on the current value of <see cref="_currentBlock"/>.
        /// If target was reached, the state is reset and the method returns false.
        /// </summary>
        /// <returns>Returns weither or no this call has completed the sync.</returns>
        public bool SyncNextHistory()
        {
            if (_requestedHeight == _syncTarget)
            {
                _syncTarget = 0;
                _requestedHeight = 0;
                return false;
            }

            _requestedHeight++;
            RequestBlockByIndex(_requestedHeight);

            return true;
        }

        public void StashAnnouncement(Announce announce)
        {
            if (announce?.Id == null)
                throw new ArgumentNullException($"{nameof(announce)} or its ID is null.");
            
            _announcements.Add(announce);
        }

        /// <summary>
        /// Will trigger the request of the next annoucement. If there's no more announcements to sync
        /// this method return false.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the this method is called with no currently synced announcement
        /// and an empty cache the method throws.</exception>
        public bool SyncNextAnnouncement()
        {
            if (!IsSyncingAnnounced && !_announcements.Any())
                throw new InvalidOperationException($"Call to {nameof(SyncNextAnnouncement)} with no stashed annoucements.");

            if (!_announcements.Any())
            {
                _syncedAnnouncement = null;
                return false;
            }

            _syncedAnnouncement = _announcements.OrderBy(a => a.Height).First();
            _announcements.Remove(_syncedAnnouncement);
                
            RequestBlockById(_syncedAnnouncement.Id.ToByteArray());
            
            return true;
        }
        
        /*********************** OLD ***********************/
        
        // todo height update from announcement should be done somewhere else

        /// <summary>
        /// This method is used to update the height of the current peer.
        /// </summary>
        /// <param name="a"></param>
        public void OnAnnouncementMessage(Announce a)
        {
            if (a?.Id == null)
            {
                _logger?.Error($"[{this}] announcement or its id is null.");
                return;
            }
            
            try
            {
//                byte[] blockId = a.Id.ToByteArray();
//
//                lock (_blockLock)
//                {
//                    _blocks.Add(new PendingBlock(blockId, true, false));
//                }
//            
//                RequestBlockById(blockId);

                if (a.Height <= _peerHeight)
                {
                    // todo just log for now, but this is probably a protocol error.
                    _logger?.Warn($"[{this}] current know heigth: {_peerHeight} announcement height {a.Height}.");
                }
            
                _peerHeight = a.Height;
            
                _logger?.Trace($"[{this}] height increased: {_peerHeight}.");
            }
            catch (Exception e)
            {
                _logger?.Error(e, $"[{this}] error processing announcement.");
            }
        }

        /// <summary>
        /// This method is used to stop the timer for a block request.
        /// </summary>
        /// <param name="block"></param>
        public void OnBlockReceived(Block block)
        {
            byte[] blockHash = block.GetHashBytes();
            int blockHeight = (int) block.Header.Index;

            _logger.Info($"Receiving block {block.BlockHashToHex} from {this} at height {blockHeight}.");

            lock (_blockReqLock)
            {
                TimedBlockRequest req = _blockRequests.FirstOrDefault(b => (b.IsById && b.Id.BytesEqual(blockHash)) || (!b.IsById && b.Height == blockHeight));

                if (req != null)
                {
                    req.Cancel();
                    _blockRequests.Remove(req);
                }
            }

//            PendingBlock vBlock;
//            lock (_blockLock)
//            {
//                vBlock = _blocks.Where(b => b.IsRequesting).FirstOrDefault(b => b.BlockId.BytesEqual(blockHash));
//            }

//            if (vBlock != null)
//            {
//                vBlock.IsRequesting = false;
//                vBlock.IsValidating = true;
//            }
        }
        
        // TODO old block accepted, now block executed, logic moved higher level
//        public void OnNewBlockAccepted(IBlock block)
//        {
//            byte[] blockHash = block.GetHashBytes();
//            
//            // if we're syncing and one of the block we requested has been 
//            // accepted, we request the next.
//            if (_isSyncing)
//            {
//                int blockHeight = (int)block.Header.Index;
//                if (blockHeight >= _requestedHeight)
//                {
//                    if (blockHeight >= _syncTarget)
//                    {
//                        _logger?.Info($"[{this}] sync finished at {_syncTarget}.");
//                        
//                        EndSync();
//                    }
//                    else
//                    {
//                        int next = blockHeight + 1;
//                        
//                        // request next 
//                        RequestBlockByIndex(next);    
//                    }
//                }
//            }
//            else
//            {
//                lock (_blockLock)
//                {
//                    if (_blocks.Count == 0)
//                        return;
//                
//                    _blocks.RemoveAll(b => b.BlockId.BytesEqual(blockHash));
//                }
//            }
//        }
//
//        private void EndSync()
//        {
//            _syncTarget = 0;
//            _requestedHeight = 0;
//
//            _isSyncing = false;
//            
//            SyncFinished?.Invoke(this, EventArgs.Empty);
//            
//            MessageHub.Instance.Publish(new ReceivingHistoryBlocksChanged(false));
//        }
        
        public void RequestHeaders(int headerIndex, int headerRequestCount)
        {
            BlockHeaderRequest hReq = new BlockHeaderRequest { Height = headerIndex - 1, Count = headerRequestCount };
            Message message = NetRequestFactory.CreateMessage(AElfProtocolMsgType.HeaderRequest, hReq.ToByteArray());

            EnqueueOutgoing(message);
        }

        private void RequestBlockByIndex(int index)
        {
            // Create the request object
            BlockRequest br = new BlockRequest { Height = index };
            Message message = NetRequestFactory.CreateMessage(AElfProtocolMsgType.RequestBlock, br.ToByteArray());

            if (message.Payload == null)
            {
                _logger?.Warn($"[{this}] request for block at height {index} failed because payload is null.");
                return;   
            }
            
            SendTimedRequest(message, br);
        }
        
        private void RequestBlockById(byte[] id)
        {
            // Create the request object
            BlockRequest br = new BlockRequest { Id = ByteString.CopyFrom(id) };
            Message message = NetRequestFactory.CreateMessage(AElfProtocolMsgType.RequestBlock, br.ToByteArray());

            if (message.Payload == null)
            {
                _logger?.Warn($"[{this}] request for block with id {id.ToHex()} failed because payload is null.");
                return;
            }

            SendTimedRequest(message, br);
        }

        private void SendTimedRequest(Message message, BlockRequest br)
        {
            TimedBlockRequest blockRequest = new TimedBlockRequest(message, br, RequestTimeout);
            blockRequest.SetCurrentPeer(this);
            blockRequest.RequestTimedOut += TimedRequestOnRequestTimedOut;

            lock (_blockReqLock)
            {
                _blockRequests.Add(blockRequest);
            }
            
            EnqueueOutgoing(message, (_) =>
            {
                blockRequest?.Start();
                _logger?.Trace($"[{this}] Block request sent {{ hash: {blockRequest.Id.ToHex()} }}");
            });
            
            _logger?.Trace($"[{this}] block request enqueued {blockRequest}.");
        }

        private void TimedRequestOnRequestTimedOut(object sender, EventArgs e)
        {
            if (sender is TimedBlockRequest req)
            {
                _logger?.Warn($"[{this}] failed timed request {req}");
                
                if (req.CurrentPeerRetries < MaxRequestRetries)
                {
                    if (!req.SetCurrentPeer(this))
                    {
                        return;
                    }
                    
                    _logger?.Debug($"[{this}] try again {req}.");
                    
                    EnqueueOutgoing(req.Message, (_) =>
                    {
                        // last check for cancelation
                        if (req == null || req.IsCanceled)
                            return;
                        
                        req.Start();
                        _logger?.Trace($"[{this}] Block request sent {{ hash: {req.Id.ToHex()} }}");
                    });
                }
                else
                {
                    lock (_blockReqLock)
                    {
                        _blockRequests.RemoveAll(b => (b.IsById && b.Id.BytesEqual(req.Id)) || (!b.IsById && b.Height == req.Height));
                    }
                    
                    _logger?.Warn($"[{this}] request failed {req}.");
                    
                    req.RequestTimedOut -= TimedRequestOnRequestTimedOut;
                }
            }
        }
    }
}