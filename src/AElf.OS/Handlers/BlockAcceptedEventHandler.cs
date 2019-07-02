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
            private readonly INetworkService _networkService;
            private readonly ISyncStateService _syncStateService;
            private readonly ITaskQueueManager _taskQueueManager;

            public BlockAcceptedEventHandler(INetworkService networkService, ISyncStateService syncStateService, 
                ITaskQueueManager taskQueueManager)
            {
                _taskQueueManager = taskQueueManager;
                _networkService = networkService;
                _syncStateService = syncStateService;
            }

            public Task HandleEventAsync(BlockAcceptedEvent eventData)
            {
                if (_syncStateService.SyncState == SyncState.Finished)
                {
                    // if sync is finished we announce the block
                    _networkService.BroadcastAnnounceAsync(eventData.BlockHeader, eventData.HasFork);
                }
                else if (_syncStateService.SyncState == SyncState.Syncing)
                {
                    // if syncing and the block is higher the current target, try and update.
                    if (_syncStateService.GetCurrentSyncTarget() <= eventData.BlockHeader.Height)
                    {
                        _taskQueueManager.Enqueue(async () => {
                            await _syncStateService.UpdateSyncStateAsync();
                        }, OSConstants.InitialSyncQueueName);
                    }
                }
                
                return Task.CompletedTask;
            }
        }
    }
}