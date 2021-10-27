using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class NetworkInitializedEventHandler : ILocalEventHandler<NetworkInitializedEvent>, ITransientDependency
    {
        private readonly ISyncStateService _syncStateService;

        public NetworkInitializedEventHandler(ISyncStateService syncStateService)
        {
            _syncStateService = syncStateService;
        }
        
        public async Task HandleEventAsync(NetworkInitializedEvent eventData)
        {
            await _syncStateService.StartSyncAsync();
        }
    }
}