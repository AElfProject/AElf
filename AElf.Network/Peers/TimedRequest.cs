using System;
using System.Timers;
using AElf.Network.Data;

namespace AElf.Network.Peers
{
    public class TimedRequest : ITimedRequest
    {
        public event EventHandler RequestTimedOut;
        
        private volatile bool _requestCanceled;
        
        public double Timeout { get; }
        
        private readonly Timer _timeoutTimer;

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

        private readonly BlockRequest _blockRequest;
        
        public TimedRequest(BlockRequest blockRequest, double timeout) : this(timeout)
        {
            _blockRequest = blockRequest;
        }
        
        private TimedRequest(double timeout)
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

        public void Start()
        {
            _timeoutTimer.Start();
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
}