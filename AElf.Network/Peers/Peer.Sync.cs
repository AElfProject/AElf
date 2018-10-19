using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Common;
using AElf.Network.Connection;
using AElf.Network.Data;
using Google.Protobuf;

namespace AElf.Network.Peers
{
    public class ValidatingBlock
    {
        public int BlockNum { get; set; }
        public byte[] BlockId { get; set; }
        
        public bool IsValidating { get; set; }
        public bool IsRequesting { get; set; }

        public ValidatingBlock(byte[] blockId, bool isRequesting, bool isValidating)
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

        private object _blockLock = new object();
        private List<ValidatingBlock> _blocks { get; set; }

        private object _blockReqLock = new object();
        private List<TimedRequest> _blockRequests;
            
        public event EventHandler SyncFinished;
        
        private int _peerHeight = 0;

        private int _syncTarget = 0;
        private int _requestedHeight = 0;

        private bool _isSyncing = false;
        
        public int KnownHeight
        {
            get { return _peerHeight; }
        }

        public bool AnySyncing()
        {
            lock (_blockLock)
            {
                return _blocks.Any(b => b.IsRequesting);
            }
        }

        /// <summary>
        /// Effectively triggers a sync session with this peer. The target height is specified
        /// as a parameter so the target is not necessarily this peers current height.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="target"></param>
        public void Sync(int start, int target)
        {
            // set sync session target
            _syncTarget = target;
            
            _isSyncing = true;
            
            // start requesting 
            RequestBlockByIndex(start);
            
            // Update currently requested height.
            _requestedHeight = start;
        }

        public void OnAnnouncementMessage(Announce a)
        {
            if (a?.Id == null)
            {
                _logger?.Error($"[{this}] announcement or its id is null.");
                return;
            }
            
            try
            {
                byte[] blockId = a.Id.ToByteArray();

                lock (_blockLock)
                {
                    _blocks.Add(new ValidatingBlock(blockId, true, false));
                }
            
                RequestBlockById(blockId);

                if (a.Height <= _peerHeight)
                {
                    // todo just log for now, but this is probably an error protocol.
                    _logger?.Warn($"[{this}] current know heigth: {_peerHeight} announcement height {a.Height}.");
                }
            
                _peerHeight = a.Height;
            
                _logger?.Trace($"[{this}] peer height increased: {_peerHeight}.");
            }
            catch (Exception e)
            {
                _logger?.Error(e, $"[{this}] error processing announcement.");
            }
        }

        public void OnBlockReceived(Block block)
        {
            byte[] blockHash = block.GetHashBytes();
            
            ValidatingBlock vBlock;
            lock (_blockLock)
            {
                vBlock = _blocks.Where(b => b.IsRequesting).FirstOrDefault(b => b.BlockId.BytesEqual(blockHash));
            }

            if (vBlock != null)
            {
                vBlock.IsRequesting = false;
                vBlock.IsValidating = true;
            }

            // todo handle this for "by height" reqs
            lock (_blockReqLock)
            {
                TimedRequest req = _blockRequests.FirstOrDefault(b => b.IsById && b.Id.BytesEqual(blockHash));

                if (req != null)
                {
                    req.Stop();
                    _blockRequests.Remove(req);
                }
            }
        }
        
        public void OnNewBlockAccepted(IBlock block)
        {
            byte[] blockHash = block.GetHashBytes();
            
            // if we're syncing and one of the block we requested has been 
            // accepted, we request the next.
            if (_isSyncing)
            {
                int blockHeight = (int)block.Header.Index;
                if (blockHeight >= _requestedHeight)
                {
                    if (blockHeight >= _syncTarget)
                    {
                        _logger?.Trace($"[{this}] sync finished at {_syncTarget}.");
                        
                        EndSync();
                    }
                    else
                    {
                        int next = blockHeight + 1;
                        
                        // request next 
                        RequestBlockByIndex(next);
                    }
                }
            }
            else
            {
                lock (_blockLock)
                {
                    if (_blocks.Count == 0)
                        return;
                
                    _blocks.RemoveAll(b => b.BlockId.BytesEqual(blockHash));
                }
            }
        }

        private void EndSync()
        {
            _syncTarget = 0;
            _requestedHeight = 0;

            _isSyncing = false;
            
            SyncFinished?.Invoke(this, EventArgs.Empty);
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
            
            //TimedBlockRequest request = new TimedBlockRequest(br, RequestTimeout);
            //request.RequestTimedOut += TimedRequestOnRequestTimedOut;
            
            EnqueueOutgoing(message);
            
            _logger?.Trace($"[{this}] requested block at index {index}.");
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
            
            TimedRequest request = new TimedRequest(br, RequestTimeout);
            request.RequestTimedOut += TimedRequestOnRequestTimedOut;

            lock (_blockReqLock)
            {
                _blockRequests.Add(request);
            }
            
            EnqueueOutgoing(message, request);
            
            _logger?.Trace($"[{this}] requested block with id {id.ToHex()}.");
        }

        private void TimedRequestOnRequestTimedOut(object sender, EventArgs e)
        {
            if (sender is TimedRequest req)
            {
                _logger?.Warn("Failed to get block with " + 
                              (req.IsById ? 
                                  ("hash " + req.Id.ToHex()) 
                                  : 
                                  ("height" + req.Height)) );
            }
        }
    }
}