using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.EventMessages;
using FluentScheduler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.Scheduler.FluentScheduler
{
    public class FluentSchedulerScheduler : IConsensusScheduler
    {
        private ILocalEventBus EventBus { get; set; }

        private ILogger Logger { get; set; }

        public FluentSchedulerScheduler()
        {
            EventBus = NullLocalEventBus.Instance;
            
            Logger = NullLogger.Instance;
        }
        
        public void Dispose()
        {
            JobManager.Stop();
        }

        public async Task<IDisposable> StartAsync(int chainId)
        {
            var registry = new Registry();
            registry.Schedule(() => Logger.LogInformation("Starting FluentScheduler Scheduler."));
            JobManager.InitializeWithoutStarting(registry);
            return await Task.FromResult(this);
        }

        public async Task StopAsync()
        {
            JobManager.Stop();
            await Task.CompletedTask;
        }

        public void NewEvent(int countingMilliseconds, BlockMiningEventData blockMiningEventData)
        {
            JobManager.AddJob(() => EventBus.PublishAsync(blockMiningEventData),
                s => s.ToRunOnceIn(countingMilliseconds).Milliseconds());
        }

        public void CancelCurrentEvent()
        {
            throw new NotImplementedException();
        }
    }
}