using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.EventMessages;
using FluentScheduler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.Scheduler.FluentScheduler
{
    public class FluentSchedulerScheduler : IConsensusScheduler, ISingletonDependency
    {
        public ILocalEventBus LocalEventBus { get; set; }

        public ILogger Logger { get; set; }

        public FluentSchedulerScheduler()
        {
            LocalEventBus = NullLocalEventBus.Instance;

            Logger = NullLogger<FluentSchedulerScheduler>.Instance;
        }
        
        public void Dispose()
        {
            JobManager.Stop();
        }

        public async Task<IDisposable> StartAsync(int chainId)
        {
            return this;
        }

        public async Task StopAsync()
        {
            JobManager.Stop();
        }

        public void NewEvent(int countingMilliseconds, BlockMiningEventData blockMiningEventData)
        {
            Console.WriteLine($"Will produce block after {countingMilliseconds} ms.");

            JobManager.UseUtcTime();

            var registry = new Registry();
            registry.Schedule(() => LocalEventBus.PublishAsync(blockMiningEventData))
                .ToRunOnceAt(DateTime.UtcNow.AddMilliseconds(countingMilliseconds));
            JobManager.Initialize(registry);
        }

        public void CancelCurrentEvent()
        {
            if (JobManager.RunningSchedules.Any())
            {
                JobManager.Stop();
            }
        }
    }
}