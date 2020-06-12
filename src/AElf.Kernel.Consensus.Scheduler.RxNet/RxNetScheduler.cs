﻿using System;
using System.Reactive.Linq;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Consensus.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.Scheduler.RxNet
{
    public class RxNetScheduler : IConsensusScheduler, IObserver<ConsensusRequestMiningEventData>, ISingletonDependency
    {
        private IDisposable _observables;

        public ILocalEventBus LocalEventBus { get; set; }

        public ILogger<RxNetScheduler> Logger { get; set; }

        public RxNetScheduler()
        {
            LocalEventBus = NullLocalEventBus.Instance;

            Logger = NullLogger<RxNetScheduler>.Instance;
        }

        public void NewEvent(long countingMilliseconds, ConsensusRequestMiningEventData consensusRequestMiningEventData)
        {
            _observables = Subscribe(countingMilliseconds, consensusRequestMiningEventData);
        }

        public void CancelCurrentEvent()
        {
            _observables?.Dispose();
        }

        public IDisposable Subscribe(long countingMilliseconds, ConsensusRequestMiningEventData consensusRequestMiningEventData)
        {
            Logger.LogDebug($"Will produce block after {countingMilliseconds} ms - " +
                            $"{TimestampHelper.GetUtcNow().AddMilliseconds(countingMilliseconds).ToDateTime():yyyy-MM-dd HH.mm.ss,fff}");

            return Observable.Timer(TimeSpan.FromMilliseconds(countingMilliseconds))
                .Select(_ => consensusRequestMiningEventData).Subscribe(this);
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        // This is the callback.
        public void OnNext(ConsensusRequestMiningEventData value)
        {
            Logger.LogDebug($"Published block mining event. Current block height: {value.PreviousBlockHeight}");
            LocalEventBus.PublishAsync(value);
        }
    }
}