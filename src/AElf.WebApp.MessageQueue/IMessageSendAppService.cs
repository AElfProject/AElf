using System.Threading.Tasks;
using AElf.WebApp.Application;
using AElf.WebApp.MessageQueue.Dtos;
using AElf.WebApp.MessageQueue.Enum;
using AElf.WebApp.MessageQueue.Provider;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.MessageQueue;

public interface IMessageSendAppService
{
    Task<bool> UpdateAsync(long height);
    Task<bool> StopAsync();
    Task<bool> StartAsync();
    Task<SyncInformationDto> GetAsync();
}

public class MessageSendAppService : AElfAppService, IMessageSendAppService
{
    private readonly ISyncBlockStateProvider _syncBlockStateProvider;
    private readonly IAutoObjectMappingProvider _mapperProvider;

    public MessageSendAppService(ISyncBlockStateProvider syncBlockStateProvider,
        IAutoObjectMappingProvider mapperProvider)
    {
        _syncBlockStateProvider = syncBlockStateProvider;
        _mapperProvider = mapperProvider;
    }

    public async Task<bool> UpdateAsync(long height)
    {
        if (height < 0)
        {
            return false;
        }
        
        await _syncBlockStateProvider.UpdateStateAsync(height - 1, SyncState.Stopped);
        return true;
    }

    public async Task<bool> StopAsync()
    {
        var currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        if (currentState.State == SyncState.Stopped)
        {
            return true;
        }
        await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Stopped);
        return true;
    }

    public async Task<bool> StartAsync()
    {
        var currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        if (currentState.State != SyncState.Stopped)
            return false;
        await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Prepared);
        return true;
    }

    public async Task<SyncInformationDto> GetAsync()
    {
        var currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        return _mapperProvider.Map<SyncInformation, SyncInformationDto>(currentState);
    }
}