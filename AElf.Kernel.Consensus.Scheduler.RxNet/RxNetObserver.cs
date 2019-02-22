/*using System;
using System.Reactive.Linq;
using AElf.Kernel.EventMessages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.Scheduler.RxNet
{
    // ReSharper disable once InconsistentNaming
    public class RxNetObserver
    {
        public ILocalEventBus LocalEventBus { get; set; }

        public ILogger<RxNetObserver> Logger { get; set; }

        public RxNetObserver()
        {
            LocalEventBus = NullLocalEventBus.Instance;

            Logger = NullLogger<RxNetObserver>.Instance;
        }

        public IDisposable Subscribe(int countingMilliseconds, BlockMiningEventData blockMiningEventData)
        {
            //Logger.LogDebug($"Will produce block after {countingMilliseconds} ms.");

            return Observable.Timer(TimeSpan.FromMilliseconds(countingMilliseconds))
                .Select(_ => blockMiningEventData).Subscribe(this);
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
            //Logger.LogDebug($"Published block mining event: {value}");
            LocalEventBus.PublishAsync(value);
        }
    }
}*/