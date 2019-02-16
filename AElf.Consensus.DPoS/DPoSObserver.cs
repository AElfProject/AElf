using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using AElf.Kernel;
using AElf.Kernel.Events;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSObserver : IConsensusObserver
    {
        public IEventBus EventBus { get; set; }

        public DPoSObserver()
        {
            EventBus = NullLocalEventBus.Instance;
        }
        
        public IDisposable Subscribe(byte[] consensusCommand)
        {
            var command = DPoSCommand.Parser.ParseFrom(consensusCommand);
            
            return Observable.Timer(TimeSpan.FromMilliseconds(command.CountingMilliseconds))
                .Select(_ => command.ChainId).Subscribe(this);
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(int value)
        {
            EventBus.PublishAsync(new BlockMiningEventData(value));
        }
    }
}