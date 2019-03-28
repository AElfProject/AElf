using System;
using System.Reactive.Linq;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.EventMessages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.Scheduler.RxNet
{
    public class RxNetScheduler : IConsensusScheduler, IObserver<BlockMiningEventData>, ISingletonDependency
    {
        private IDisposable _observables;

        public RxNetScheduler()
        {
            LocalEventBus = NullLocalEventBus.Instance;

            Logger = NullLogger<RxNetScheduler>.Instance;
        }

        public ILocalEventBus LocalEventBus { get; set; }

        public ILogger<RxNetScheduler> Logger { get; set; }

        public void NewEvent(int countingMilliseconds, BlockMiningEventData blockMiningEventData)
        {
            _observables = Subscribe(countingMilliseconds, blockMiningEventData);
        }

        public void CancelCurrentEvent()
        {
            _observables?.Dispose();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        // This is the callback.
        public void OnNext(BlockMiningEventData value)
        {
            Logger.LogDebug($"Published block mining event. Current block height: {value.PreviousBlockHeight}");
            LocalEventBus.PublishAsync(value);
        }

        public IDisposable Subscribe(int countingMilliseconds, BlockMiningEventData blockMiningEventData)
        {
            Logger.LogDebug($"Will produce block after {countingMilliseconds} ms - " +
                            $"{DateTime.UtcNow.AddMilliseconds(countingMilliseconds):yyyy-MM-dd HH.mm.ss,fff}");

            return Observable.Timer(TimeSpan.FromMilliseconds(countingMilliseconds))
                .Select(_ => blockMiningEventData).Subscribe(this);
        }
    }
}