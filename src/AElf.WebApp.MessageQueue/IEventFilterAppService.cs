using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Dtos;
using AElf.WebApp.MessageQueue.Entities;
using AElf.WebApp.MessageQueue.Provider;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.MessageQueue
{
    public interface IEventFilterAppService : IApplicationService
    {
        Task<Guid?> AddEventFilterAsync(AddEventFilterInput input);
        Task<bool> DeleteEventFilterAsync(DeleteEventFilterInput input);
        Task<bool> UpdateEventFilterAsync(UpdateEventFilterInput input);
        Task<List<EventFilterEntity>> GetEventFilterAsync(GetEventFilterInput input);
        Task<bool> SetStateAsync(SetStateInput input);
    }

    public class EventFilterAppService : IEventFilterAppService
    {
        private readonly IEventFiltersProvider _eventFiltersProvider;

        public EventFilterAppService(IEventFiltersProvider eventFiltersProvider)
        {
            _eventFiltersProvider = eventFiltersProvider;
        }

        public Task<Guid?> AddEventFilterAsync(AddEventFilterInput input)
        {
            var newId = _eventFiltersProvider.Add(input);
            return Task.FromResult(newId);
        }

        public Task<bool> DeleteEventFilterAsync(DeleteEventFilterInput input)
        {
            return Task.FromResult(_eventFiltersProvider.Delete(input));
        }

        public Task<bool> UpdateEventFilterAsync(UpdateEventFilterInput input)
        {
            return Task.FromResult(_eventFiltersProvider.Update(input));
        }

        public Task<List<EventFilterEntity>> GetEventFilterAsync(GetEventFilterInput input)
        {
            return Task.FromResult(_eventFiltersProvider.GetEventFilters(input.Id));
        }
        
        public Task<bool> SetStateAsync(SetStateInput input)
        {
            return Task.FromResult(_eventFiltersProvider.SetState(input.Id, input.IsStop));
        }
    }
}