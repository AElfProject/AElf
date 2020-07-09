using AElf.WebApp.Application.Chain.Dto;
using AutoMapper;

namespace AElf.WebApp.Application.Chain
{
    public class TaskQueueInfoProfile : Profile
    {
        public TaskQueueInfoProfile()
        {
            CreateMap<TaskQueueInfo, TaskQueueInfoDto>();
        }
    }
}