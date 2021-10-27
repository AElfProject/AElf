using AElf.WebApp.Application.Chain.Dto;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.Application.Chain
{
    public interface ITaskQueueStatusAppService : IApplicationService
    {
        List<TaskQueueInfoDto> GetTaskQueueStatusAsync();
    }

    [ControllerName("BlockChain")]
    public class TaskQueueStatusAppService : ITaskQueueStatusAppService
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
}