using System;
using System.Reactive.Linq;
using AElf.Kernel.Consensus.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.Scheduler.RxNet
{
    // ReSharper disable once InconsistentNaming
    public class Observer
    {
        public ILocalEventBus EventBus { get; set; }

        public ILogger<Observer> Logger { get; set; }

        public Observer()
        {
            EventBus = NullLocalEventBus.Instance;

            Logger = NullLogger<Observer>.Instance;
        }

//        public IDisposable Subscribe(int countingMilliseconds, DPoSHint hint)
//        {
//            Logger.LogInformation($"Will produce block after {countingMilliseconds} ms.");
//
//            return Observable.Timer(TimeSpan.FromMilliseconds(countingMilliseconds))
//                .Select(_ => command.ChainId).Subscribe(this);
//
//        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(int value)
        {
            Logger.LogInformation($"Published block mining event, chain id: {value}");
            EventBus.PublishAsync(new BlockMiningEventData(value));
        }
    }
}