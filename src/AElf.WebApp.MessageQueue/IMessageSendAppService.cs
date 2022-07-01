using System.Threading.Tasks;
using AElf.WebApp.Application;
using AElf.WebApp.MessageQueue.Dtos;
using AElf.WebApp.MessageQueue.Enum;
using AElf.WebApp.MessageQueue.Provider;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.MessageQueue;

public interface IMessageSendAppService
{
    Task<MessageResultDto> UpdateAsync(long height);
    Task<bool> StopAsync();
    Task<MessageResultDto> StartAsync();
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

    public async Task<MessageResultDto> UpdateAsync(long height)
    {
        var msgRet = new MessageResultDto();
        if (height < 1)
        {
            msgRet.IsSuccess = false;
            msgRet.Status = "Height should be greater than 1";
            return msgRet;
        }

        var currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        if (currentState.State != SyncState.Stopped)
        {
            msgRet.IsSuccess = false;
            msgRet.Status = "It is required to stop block message sending first";
            return msgRet;
        }

        await _syncBlockStateProvider.UpdateStateAsync(height - 1, SyncState.Stopped);

        msgRet.IsSuccess = true;
        msgRet.Status = "Ok";
        return msgRet;
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

    public async Task<MessageResultDto> StartAsync()
    {
        var msgRet = new MessageResultDto();
        var currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        if (currentState.State != SyncState.Stopped)
        {
            msgRet.IsSuccess = false;
            msgRet.Status = "It is required to stop block message sending first";
            return msgRet;
        }

        await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Prepared);
        msgRet.IsSuccess = true;
        msgRet.Status = "Ok";
        return msgRet;
    }

    public async Task<SyncInformationDto> GetAsync()
    {
        var currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        return _mapperProvider.Map<SyncInformation, SyncInformationDto>(currentState);
    }
}