using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using System.Timers;
using AElf.Common;
using AElf.Network.Connection;
using AElf.Network.Data;
using Google.Protobuf;

namespace AElf.Network.Peers
{
    public class BlockTimeoutRequest
    {   
        public event EventHandler RequestTimedOut;
        
        private volatile bool _requestCanceled;
        
        public double Timeout { get; }
        
        private readonly Timer _timeoutTimer;

        public bool IsCanceled 
        {
            get { return _requestCanceled; }
        }
        
        public byte[] Id { get; private set; }
        public int Height { get; private set; }
        
        public BlockTimeoutRequest(byte[] id)
        {
            Id = id;
        }

        public BlockTimeoutRequest(int height)
        {
            Height = height;
        }
        
        private BlockTimeoutRequest(double timeout)
        {
            _timeoutTimer = new Timer();
            _timeoutTimer.Interval = timeout;
            _timeoutTimer.Elapsed += TimerTimeoutElapsed;
            _timeoutTimer.AutoReset = false;

            Timeout = timeout;
        }
        
        private void TimerTimeoutElapsed(object sender, ElapsedEventArgs e)
        {
            if (_requestCanceled) 
                return;
            
            Stop();
            RequestTimedOut?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// This method is used to cancel the request and cleanup. Once this
        /// has been called, you can't use this instance.
        /// </summary>
        public void Stop()
        {
            _timeoutTimer.Stop();
        }
    }

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
        private List<ValidatingBlock> _blocks { get; set; } 
            
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
            return _blocks.Any(b => b.IsRequesting);
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
            byte[] blockId = a.Id.ToByteArray();
            _blocks.Add(new ValidatingBlock(blockId, true, false));
            
            RequestBlockById(blockId);
            
            _peerHeight = a.Height;
            
            _logger?.Trace($"[{this}] peer height increased : {_peerHeight}.");
        }

        public void OnBlockReceived(Block block)
        {
            byte[] blockHash = block.GetHashBytes();
            
            ValidatingBlock vBlock =
                _blocks.Where(b => b.IsRequesting).FirstOrDefault(b => b.BlockId.BytesEqual(blockHash));

            if (vBlock != null)
            {
                vBlock.IsRequesting = false;
                vBlock.IsValidating = true;
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
                if (_blocks.Count == 0)
                    return;
                
                ValidatingBlock vBlock = _blocks.Where(b => b.IsRequesting).FirstOrDefault(b => b.BlockId.BytesEqual(blockHash));
                _blocks.Remove(vBlock);
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
                _logger?.Warn($"Request for block at height {index} failed because payload is null.");
                return;   
            }
            
            EnqueueOutgoing(message);
            
            _logger?.Trace($"[{this}] sync {_isSyncing}, requested block at index {index}.");
        }
        
        private void RequestBlockById(byte[] id)
        {
            // Create the request object
            BlockRequest br = new BlockRequest { Id = ByteString.CopyFrom(id) };
            Message message = NetRequestFactory.CreateMessage(AElfProtocolMsgType.RequestBlock, br.ToByteArray());

            if (message.Payload == null)
            {
                _logger?.Warn($"Request for block at height {id.ToHex()} failed because payload is null.");
                return;
            }
            
            EnqueueOutgoing(message);
            
            _logger?.Trace($"[{this}] sync {_isSyncing}, requested block at index {id.ToHex()}.");
        }
    }
}