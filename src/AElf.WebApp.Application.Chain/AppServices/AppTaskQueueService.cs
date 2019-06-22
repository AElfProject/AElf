using System;
using System.Collections.Generic;
using System.Text;
using AElf.WebApp.Application.Chain.Dto;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain.AppServices
{
    public interface IAppTaskQueueService : IApplicationService
    {
        /// <summary>
        /// Gets the task queue status asynchronous.
        /// </summary>
        /// <returns></returns>
        List<TaskQueueInfoDto> GetTaskQueueStatusAsync();
    }

    public sealed class AppTaskQueueService : IAppTaskQueueService
    {
        private readonly ITaskQueueManager _taskQueueManager;


        /// <summary>
        /// Initializes a new instance of the <see cref="AppTaskQueueService"/> class.
        /// </summary>
        /// <param name="taskQueueManager">The task queue manager.</param>
        public AppTaskQueueService(ITaskQueueManager taskQueueManager)
        {
            _taskQueueManager = taskQueueManager;
        }

        /// <summary>
        /// Gets the task queue status asynchronous.
        /// </summary>
        /// <returns></returns>
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