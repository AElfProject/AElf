using System.Collections.Generic;
using System.Linq;
using AElf.WebApp.Application.Chain.Dto;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface ITaskQueueStatusAppService : IApplicationService
    {
        List<TaskQueueInfoDto> GetTaskQueueStatusAsync();
    }

    public class TaskQueueStatusAppService :ITaskQueueStatusAppService
    {
        private readonly ITaskQueueManager _taskQueueManager;
        
        
        public TaskQueueStatusAppService(ITaskQueueManager taskQueueManager)
        {
            _taskQueueManager = taskQueueManager;
        }
        
        
        public List<TaskQueueInfoDto> GetTaskQueueStatusAsync()
        {
            var taskQueueStatus = _taskQueueManager.GetQueueStatus();
            return taskQueueStatus.Select(taskQueueState => new TaskQueueInfoDto
            {
                Name = taskQueueState.Name,
                Size = taskQueueState.Size
            }).ToList();
        }
    }
}