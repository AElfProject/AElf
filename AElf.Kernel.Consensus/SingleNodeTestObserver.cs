using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NLog;
using AElf.Common;
using AElf.Configuration.Config.Consensus;

namespace AElf.Kernel.Consensus
{
    public class SingleNodeTestObserver : IObserver<long>
    {
        private readonly ILogger _logger;
        
        private readonly Func<Task> _miningAndBroadcasting;

        public SingleNodeTestObserver(ILogger logger, params Func<Task>[] miningFunctions)
        {
            if (miningFunctions.Length != 1)
            {
                throw new ArgumentException("Incorrect functions count.", nameof(miningFunctions));
            }
            
            _logger = logger;

            _miningAndBroadcasting = miningFunctions[0];
        }

        public void OnCompleted()
        {
            _logger?.Trace($"{nameof(SingleNodeTestObserver)} completed.");
        }

        public void OnError(Exception error)
        {
            _logger?.Error(error, $"{nameof(SingleNodeTestObserver)} error.");
        }

        public void OnNext(long value)
        {
            _miningAndBroadcasting();
        }

        public IDisposable SubscribeSingleNodeTestProcess()
        {
            return Observable
                .Interval(TimeSpan.FromMilliseconds(ConsensusConfig.Instance.SingleNodeTestMiningInterval))
                .Subscribe(this);
        }

    }
}