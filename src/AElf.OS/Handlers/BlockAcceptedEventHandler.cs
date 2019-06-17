using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.BlockSync.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    namespace AElf.OS.Network.Handler
    {
        public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
        {
            public INetworkService NetworkService { get; set; }
            public ISyncStateService SyncStateService { get; set; }
            public ITaskQueueManager TaskQueueManager { get; set; }

            public Task HandleEventAsync(BlockAcceptedEvent eventData)
            {
                NetworkService.BroadcastAnnounceAsync(eventData.BlockHeader, eventData.HasFork);

                if (!SyncStateService.IsSyncFinished)
                {
                    TaskQueueManager.Enqueue(async () => {
                        await SyncStateService.UpdateSyncStateAsync();
                    }, OSConsts.InitialSyncQueueName);
                }
                
                return Task.CompletedTask;
            }
        }
    }
}