using System;
using System.Reactive.Linq;
using AElf.Kernel.Consensus.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.DPoS.Infrastructure
{
    // ReSharper disable once InconsistentNaming
    public class DPoSObserver : IConsensusObserver
    {
        public ILocalEventBus EventBus { get; set; }

        public ILogger<DPoSObserver> Logger { get; set; }

        public DPoSObserver()
        {
            EventBus = NullLocalEventBus.Instance;

            Logger = NullLogger<DPoSObserver>.Instance;
        }

        public IDisposable Subscribe(byte[] consensusCommand)
        {
            var command = DPoSCommand.Parser.ParseFrom(consensusCommand);

            Logger.LogInformation($"Will produce block after {command.CountingMilliseconds} ms.");

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
            Logger.LogInformation($"Published block mining event, chain id: {value}");
            EventBus.PublishAsync(new BlockMiningEventData(value));
        }
    }
}