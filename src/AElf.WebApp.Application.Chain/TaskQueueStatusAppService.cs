using AElf.WebApp.Application.Chain.Dto;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Volo.Abp.Application.Services;

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
        private readonly IMapper _mapper;

        public TaskQueueStatusAppService(ITaskQueueManager taskQueueManager, IMapper mapper)
        {
            _taskQueueManager = taskQueueManager;
            _mapper = mapper;
        }

        public List<TaskQueueInfoDto> GetTaskQueueStatusAsync()
        {
            var taskQueueStatus = _taskQueueManager.GetQueueStatus();
            return _mapper.Map<List<TaskQueueInfo>, List<TaskQueueInfoDto>>(taskQueueStatus);
        }
    }
}