using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Network.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers;

public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
    ITransientDependency
{
    private readonly INetworkService _networkService;
    private readonly ISyncStateService _syncStateService;

    public NewIrreversibleBlockFoundEventHandler(INetworkService networkService, ISyncStateService syncStateService)
    {
        _networkService = networkService;
        _syncStateService = syncStateService;
    }

    public Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
    {
        if (_syncStateService.SyncState != SyncState.Finished) return Task.CompletedTask;

        var _ = _networkService.BroadcastLibAnnounceAsync(eventData.BlockHash, eventData.BlockHeight);
        return Task.CompletedTask;
    }
}