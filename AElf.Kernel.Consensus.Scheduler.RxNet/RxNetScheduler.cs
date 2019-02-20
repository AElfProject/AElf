using System;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;

namespace AElf.Kernel.Consensus.Scheduler.RxNet
{
    public class RxNetScheduler : IConsensusScheduler
    {
        private IDisposable _observables;

        private readonly RxNetObserver _observer;

        public RxNetScheduler(RxNetObserver observer)
        {
            _observer = observer;
        }
        
        public void Launch(int countingMilliseconds, int chainId, Hash preBlockHash, ulong preBlockHeight)
        {
            _observables = _observer.Subscribe(countingMilliseconds, chainId, preBlockHash, preBlockHeight);
        }

        public void TryToStop()
        {
            _observables?.Dispose();
        }
    }
}