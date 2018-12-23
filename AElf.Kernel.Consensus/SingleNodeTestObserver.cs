using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Consensus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus
{
    public class SingleNodeTestObserver : IObserver<long>
    {
        public ILogger<SingleNodeTestObserver> Logger {get;set;}
        
        private readonly Func<Task> _miningAndBroadcasting;

        public SingleNodeTestObserver(ILogger logger, params Func<Task>[] miningFunctions)
        {
            if (miningFunctions.Length != 1)
            {
                throw new ArgumentException("Incorrect functions count.", nameof(miningFunctions));
            }
            
            Logger = NullLogger<SingleNodeTestObserver>.Instance;

            _miningAndBroadcasting = miningFunctions[0];
        }

        public void OnCompleted()
        {
            Logger.LogTrace($"{nameof(SingleNodeTestObserver)} completed.");
        }

        public void OnError(Exception error)
        {
            Logger.LogError(error, $"{nameof(SingleNodeTestObserver)} error.");
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