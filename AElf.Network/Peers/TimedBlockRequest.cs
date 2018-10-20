using System;
using System.Collections.Generic;
using System.Timers;
using AElf.ChainController;
using AElf.Common;
using AElf.Network.Connection;
using AElf.Network.Data;

namespace AElf.Network.Peers
{   
    public class TimedBlockRequest : ITimedRequest
    {
        public event EventHandler RequestTimedOut;

        private volatile bool _requestCanceled = false;

        public bool IsCanceled
        {
            get { return _requestCanceled; }
        }

        public double Timeout { get; }
        
        private readonly Timer _timeoutTimer;
        
        public int CurrentPeerRetries { get; set; }
        public List<IPeer> TriedPeers { get; } = new List<IPeer>();
        public IPeer CurrentPeer { get; private set; }

        public bool IsById
        {
            get { return _blockRequest.Id != null && _blockRequest.Id.Length > 0; }
        }

        private byte[] _id;
        public byte[] Id
        {
            get
            {
                if (_id == null)
                    return _id = _blockRequest?.Id?.ToByteArray();

                return _id;
            }
        }

        public int Height
        {
            get { return _blockRequest.Height; }
        }

        public Message Message
        {
            get { return _message; }
        }

        private readonly Message _message;
        private readonly BlockRequest _blockRequest;
        
        public TimedBlockRequest(Message message, BlockRequest blockRequest, double timeout) : this(timeout)
        {
            _message = message;
            _blockRequest = blockRequest;
        }
        
        private TimedBlockRequest(double timeout)
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
            
            _timeoutTimer.Stop();
            RequestTimedOut?.Invoke(this, EventArgs.Empty);
        }

        public void Start()
        {
            _timeoutTimer.Start();
        }

        public bool SetCurrentPeer(IPeer peer)
        {
            // test if not already canceled
            if (_requestCanceled)
                return false;

            // Update tried peer list: if first try or subsequent try but different peer
            if (CurrentPeer == null || (CurrentPeer != null && CurrentPeer == peer))
            {
                TriedPeers.Add(peer);
            }

            // If new peer reset tries otherwise increment
            if (CurrentPeer != null && CurrentPeer != peer)
                CurrentPeerRetries = 1;
            else
                CurrentPeerRetries++;

            CurrentPeer = peer;

            return true;
        }
        
        /// <summary>
        /// This method is used to cancel the request and cleanup. Once this
        /// has been called, you can't use this instance.
        /// </summary>
        public void Cancel()
        {
            _requestCanceled = true;
            _timeoutTimer.Stop();
        }

        public override string ToString()
        {
            string res = "{";

            if (IsById)
            {
                res += $" id: {Id.ToHex()}, ";
            }
            else
            {
                res += $" height: {Height}, ";
            }

            res += $"current-tries: {CurrentPeerRetries} }}";

            return res;
        }
    }
}