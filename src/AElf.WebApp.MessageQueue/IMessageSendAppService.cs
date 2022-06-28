using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Dtos;
using AElf.WebApp.MessageQueue.Enum;
using AElf.WebApp.MessageQueue.Provider;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.MessageQueue;

public interface IMessageSendAppService : IApplicationService
{
    Task<bool> UpdateAsync(long height);
    Task<bool> StopAsync();
    Task<bool> StartAsync();
    Task<SyncInformationDto> GetAsync();
}

public class MessageSendAppService : IMessageSendAppService
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
        await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Stopped);
        return true;
    }

    public async Task<bool> StartAsync()
    {
        await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Prepared);
        return true;
    }

    public async Task<SyncInformationDto> GetAsync()
    {
        var currentState = _syncBlockStateProvider.GetCurrentState();
        return _mapperProvider.Map<SyncInformation, SyncInformationDto>(currentState);
    }
}