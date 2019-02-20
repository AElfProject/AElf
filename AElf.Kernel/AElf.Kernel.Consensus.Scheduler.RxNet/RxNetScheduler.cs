using System;
using AElf.Kernel.Consensus.Application;

namespace AElf.Kernel.Consensus.Scheduler.RxNet
{
    public class RxNetScheduler : IConsensusScheduler
    {
        private readonly IDisposable _observables;
        
        public void ScheduleWithCommand(ConsensusCommand consensusCommand)
        {
            throw new NotImplementedException();
        }

        public void TryToStop()
        {
            throw new NotImplementedException();
        }
    }
}