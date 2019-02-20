using System;
using System.Threading.Tasks;
using AElf.Kernel.Consensus.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.Scheduler.RxNet
{
    public class RxNetScheduler : IConsensusScheduler, ISingletonDependency
    {
        private IDisposable _observables;

        private readonly RxNetObserver _observer;

        public RxNetScheduler(RxNetObserver observer)
        {
            _observer = observer;
        }
        
        public void NewEvent(int countingMilliseconds, BlockMiningEventData blockMiningEventData)
        {
            _observables = _observer.Subscribe(countingMilliseconds, blockMiningEventData);
        }
        
        public void Dispose()
        {
            _observables?.Dispose();
        }

        public async Task<IDisposable> StartAsync(int chainId)
        {
            return this;
        }

        public Task StopAsync()
        {
            _observables?.Dispose();
            return Task.CompletedTask;
        }
    }
}