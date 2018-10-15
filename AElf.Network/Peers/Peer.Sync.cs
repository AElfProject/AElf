using System;
using AElf.Kernel;
using System.Timers;
using AElf.Network.Connection;
using AElf.Network.Data;
using Google.Protobuf;
using NServiceKit.Common;

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
    
    public partial class Peer
    {
        public event EventHandler SyncFinished;
        
        private int _peerHeight = 0;

        private int _syncTarget = 0;
        private int _requestedHeight = 0;

        private bool _isSyncing = false;
        
        public int KnownHeight
        {
            get { return _peerHeight; }
        }
        
        private void OnBlockReceived(Block block)
        {
            TimeoutRequest p;
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
            RequestBlock(start);
            
            // Update currently requested height.
            _requestedHeight = start;
        }

        public void OnAnnoucementMessage(Announce a)
        {
            _peerHeight = a.Height;
            _logger?.Trace($"[{this}] peer height increased : {_peerHeight}.");
        }
        
        public void OnNewBlockAccepted(Block block)
        {
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
                        RequestBlock(next);
                    }
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

        private void RequestBlock(int index)
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
    }
}