using System;
using System.Reactive.Linq;
using AElf.Kernel.Consensus.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.Scheduler.RxNet
{
    // ReSharper disable once InconsistentNaming
    public class RxNetObserver : IObserver<BlockMiningEventData>
    {
        public ILocalEventBus EventBus { get; set; }

        public ILogger<RxNetObserver> Logger { get; set; }

        public RxNetObserver()
        {
            EventBus = NullLocalEventBus.Instance;

            Logger = NullLogger<RxNetObserver>.Instance;
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

        public void OnNext(BlockMiningEventData value)
        {
            Logger.LogInformation($"Published block mining event: {value}");
            EventBus.PublishAsync(value);
        }
    }
}