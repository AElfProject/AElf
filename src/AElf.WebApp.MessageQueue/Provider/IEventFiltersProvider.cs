using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Entities;
using AElf.WebApp.MessageQueue.Enums;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue.Provider
{
    public interface IEventFiltersProvider
    {
        Task<List<EventFilterEntity>> GetAllEventFilterAsync();
        Task<(List<EventFilterEntity>, List<EventFilterEntity>)> GetGroupedEventFilterAsync(long currentHeight);
        Task SyncEventFiltersAsync();
        Task UpdateEventFiltersHeightAsync(EventFilterEntity filter, long latestHeight);

    }

    public class EventFiltersProvider : IEventFiltersProvider, ISingletonDependency
    {
        public Task<List<EventFilterEntity>> GetAllEventFilterAsync()
        {
            throw new System.NotImplementedException();
        }

        public async Task<(List<EventFilterEntity>, List<EventFilterEntity>)> GetGroupedEventFilterAsync(
            long blockStartHeight)
        {
            var currentHeight = blockStartHeight - 1;
            var allEventFilters = new List<EventFilterEntity>();
            var syncEventFilters = new List<EventFilterEntity>();
            var asyncEventFilters = new List<EventFilterEntity>();
            foreach (var eventFilter in allEventFilters)
            {
                if (eventFilter.Status == EventFilterStatus.Stopped)
                    continue; // todo add logs
                if (eventFilter.FromHeight > currentHeight)
                    continue;
                if (eventFilter.ToHeight > 0 && eventFilter.ToHeight < currentHeight)
                {
                    continue;
                }
                if (eventFilter.CurrentHeight > currentHeight)
                    continue;
                if (eventFilter.CurrentHeight < currentHeight)
                {
                    eventFilter.Status = EventFilterStatus.AsyncRunning;
                    asyncEventFilters.Add(eventFilter);
                }
                else
                {
                    eventFilter.Status = EventFilterStatus.SyncRunning;
                    syncEventFilters.Add(eventFilter);
                }
            }

            return (asyncEventFilters, syncEventFilters);
        }

        public Task SyncEventFiltersAsync()
        {
            return Task.CompletedTask;
        }

        public Task UpdateEventFiltersHeightAsync(EventFilterEntity filter, long latestHeight)
        {
            throw new System.NotImplementedException();
        }
    }
}