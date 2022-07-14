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
    Task<MessageResultDto> SetWorkerAsync(SetWorkerInput input);
}

public class MessageSendAppService : AElfAppService, IMessageSendAppService
{
    private readonly ISyncBlockStateProvider _syncBlockStateProvider;
    private readonly ISendMessageByDesignateHeightTaskManager _workerManager;
    private readonly IAutoObjectMappingProvider _mapperProvider;

    public MessageSendAppService(ISyncBlockStateProvider syncBlockStateProvider,
        IAutoObjectMappingProvider mapperProvider, ISendMessageByDesignateHeightTaskManager workerManager)
    {
        _syncBlockStateProvider = syncBlockStateProvider;
        _mapperProvider = mapperProvider;
        _workerManager = workerManager;
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

        await _syncBlockStateProvider.UpdateStateAsync(height - 1);
        msgRet.IsSuccess = true;
        msgRet.Status = "Ok";
        return msgRet;
    }

    public async Task<bool> StopAsync()
    {
        var currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        if (currentState.State is SyncState.Stopping or SyncState.Stopped)
        {
            return true;
        }

        await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Stopping);
        return true;
    }

    public async Task<MessageResultDto> StartAsync()
    {
        var msgRet = new MessageResultDto();
        var currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        if (currentState.State != SyncState.Stopped)
        {
            msgRet.IsSuccess = false;
            msgRet.Status = "It is required to stop sending block message first";
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

    public async Task<MessageResultDto> SetWorkerAsync(SetWorkerInput input)
    {
        var msgRet = new MessageResultDto();
        var currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        if (currentState.State != SyncState.Stopped)
        {
            msgRet.IsSuccess = false;
            msgRet.Status = "It is required to stop sending block message first";
            return msgRet;
        }

        if (input.Period is <= 0)
        {
            msgRet.IsSuccess = false;
            msgRet.Status = $"Invalid input, period should be greater than 0, actually is {input.Period}";
            return msgRet;
        }

        if (input.BlockCountPerPeriod is <= 0)
        {
            msgRet.IsSuccess = false;
            msgRet.Status = $"Invalid input, BlockCountPerPeriod should be greater than 0, actually is {input.BlockCountPerPeriod}";
            return msgRet;
        }
        
        if (input.ParallelCount is <= 0)
        {
            msgRet.IsSuccess = false;
            msgRet.Status = $"Invalid input, ParallelCount should be greater than 0, actually is {input.ParallelCount}";
            return msgRet;
        }
        
        _workerManager.SetWorker(input.Period, input.BlockCountPerPeriod, input.ParallelCount);
        msgRet.IsSuccess = true;
        msgRet.Status = "Ok";
        return msgRet;
    }
}