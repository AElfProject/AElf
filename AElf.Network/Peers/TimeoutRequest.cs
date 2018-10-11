using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using AElf.Common;
using AElf.Network.Connection;

namespace AElf.Network.Peers
{
    public class TimeoutRequest
    {
        public const int DefaultMaxRetry = 2;
        
        public event EventHandler RequestTimedOut;
        
        private volatile bool _requestCanceled;

        public bool IsCanceled 
        {
            get { return _requestCanceled; }
        }

        public bool HasTimedOut { get; private set; } = true;

        private readonly Timer _timeoutTimer;
        
        public IPeer Peer { get; private set; }
        public Message RequestMessage { get; }

        public byte[] Id
        {
            get { return RequestMessage?.Id; }
        }

        public bool IsBlockRequest
        {
            get { return TransactionHashes == null; }
        }
        
        public bool IsTxRequest
        {
            get { return TransactionHashes != null; }
        }
        
        public List<byte[]> TransactionHashes { get; private set; }
        public int BlockIndex { get; private set; }
        
        public readonly List<IPeer> TriedPeers = new List<IPeer>();

        public int MaxRetryCount { get; set; } = DefaultMaxRetry;
        public int RetryCount { get; private set; } = 0;

        public bool HasReachedMaxRetry
        {
            get { return RetryCount >= MaxRetryCount; }
        }
        
        public double Timeout { get; }

        private TimeoutRequest(Message msg, double timeout)
        {
            RequestMessage = msg;
            
            _timeoutTimer = new Timer();
            _timeoutTimer.Interval = timeout;
            _timeoutTimer.Elapsed += TimerTimeoutElapsed;
            _timeoutTimer.AutoReset = false;

            Timeout = timeout;
        }
        
        public TimeoutRequest(List<byte[]> transactionHashes, Message msg, double timeout)
            : this(msg, timeout)
        {
            TransactionHashes = transactionHashes;
        }
        
        public TimeoutRequest(int index, Message msg, double timeout)
            : this(msg, timeout)
        {
            BlockIndex = index;
        }

        public void TryPeer(IPeer peer)
        {
            Peer = peer;
            
            if (Peer == null)
                throw new InvalidOperationException($"Peer cannot be null." );
            
            if (RequestMessage == null)
                throw new InvalidOperationException($"RequestMessage cannot be null." );
            
            if (!HasTimedOut)
                throw new InvalidOperationException($"Cannot switch peer before timeout.");
            
            if (HasReachedMaxRetry)
                throw new InvalidOperationException($"Cannot retry : max retry count reached.");
            
            TriedPeers.Add(peer);
            Peer.EnqueueOutgoing(RequestMessage);
            
            _timeoutTimer.Start();
            HasTimedOut = false;
            RetryCount++;
        }

        private void TimerTimeoutElapsed(object sender, ElapsedEventArgs e)
        {
            if (_requestCanceled) 
                return;

            // set this to true so the request can be used with another 
            // peer if needed.
            HasTimedOut = true;
            
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

        #region Transaction request

        public byte[] ContainsTransaction(byte[] txHash)
        {
            return TransactionHashes.FirstOrDefault(t => t.BytesEqual(txHash));
        }

        #endregion
    }
}