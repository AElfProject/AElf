using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Dtos;
using AElf.WebApp.MessageQueue.Entities;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.MessageQueue
{
    public interface IEventFilterAppService : IApplicationService
    {
        Task AddEventFilterAsync(AddEventFilterInput input);
        Task DeleteEventFilterAsync(DeleteEventFilterInput input);
        Task UpdateEventFilterAsync(UpdateEventFilterInput input);
        Task<List<EventFilterEntity>> GetEventFilterAsync(GetEventFilterInput input);
    }

    public class EventFilterAppService : IEventFilterAppService
    {
        public Task AddEventFilterAsync(AddEventFilterInput input)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteEventFilterAsync(DeleteEventFilterInput input)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateEventFilterAsync(UpdateEventFilterInput input)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<EventFilterEntity>> GetEventFilterAsync(GetEventFilterInput input)
        {
            throw new System.NotImplementedException();
        }
    }
}