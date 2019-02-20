using System;
using AElf.Kernel.Consensus.Application;

namespace AElf.Kernel.Consensus.Scheduler.RxNet
{
    public class RxNetScheduler : IConsensusScheduler
    {
        private readonly IDisposable _observables;

        private readonly RxNetObserver _observer;

        public RxNetScheduler(RxNetObserver observer)
        {
            _observer = observer;
        }
        
        public void Launch(ConsensusCommand consensusCommand)
        {
            throw new NotImplementedException();
        }

        public void TryToStop()
        {
            _observables?.Dispose();
        }
    }
}