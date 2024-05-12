using System.Collections.Generic;
using AElf.Kernel;
using AElf.WebApp.Application.Chain.Dto;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.Application.Chain;

public interface ITaskQueueStatusAppService
{
    List<TaskQueueInfoDto> GetTaskQueueStatusAsync();
}
[Ump]
public class TaskQueueStatusAppService : AElfAppService, ITaskQueueStatusAppService
{
    private readonly IObjectMapper<ChainApplicationWebAppAElfModule> _objectMapper;
    private readonly ITaskQueueManager _taskQueueManager;

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