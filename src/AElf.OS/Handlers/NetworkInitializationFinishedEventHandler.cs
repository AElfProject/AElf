using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class NetworkInitializationFinishedEventHandler : ILocalEventHandler<NetworkInitializationFinishedEvent>, ITransientDependency
    {
        private readonly ISyncStateService _syncStateService;

        public NetworkInitializationFinishedEventHandler(ISyncStateService syncStateService)
        {
            _syncStateService = syncStateService;
        }
        
        public async Task HandleEventAsync(NetworkInitializationFinishedEvent eventData)
        {
            await _syncStateService.StartSyncAsync();
        }
    }
}