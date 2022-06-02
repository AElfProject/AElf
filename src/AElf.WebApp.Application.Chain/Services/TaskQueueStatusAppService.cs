using AElf.WebApp.Application.Chain.Dto;
using System.Collections.Generic;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.Application.Chain;

public interface ITaskQueueStatusAppService
{
    List<TaskQueueInfoDto> GetTaskQueueStatusAsync();
}

public class TaskQueueStatusAppService : AElfAppService, ITaskQueueStatusAppService
{
    private readonly ITaskQueueManager _taskQueueManager;
    private readonly IObjectMapper<ChainApplicationWebAppAElfModule> _objectMapper;

    public TaskQueueStatusAppService(ITaskQueueManager taskQueueManager,
        IObjectMapper<ChainApplicationWebAppAElfModule> objectMapper)
    {
        _taskQueueManager = taskQueueManager;
        _objectMapper = objectMapper;
    }

    public List<TaskQueueInfoDto> GetTaskQueueStatusAsync()
    {
        var taskQueueStatus = _taskQueueManager.GetQueueStatus();
        return _objectMapper.Map<List<TaskQueueInfo>, List<TaskQueueInfoDto>>(taskQueueStatus);
    }
}