using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Entities;
using AElf.WebApp.MessageQueue.Enums;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue.Provider
{
    public interface IEventFiltersProvider
    {
        Task InitializeEventFiltersAsync();
        Task<List<EventFilterEntity>> GetAllEventFilterAsync();
        Task<(List<EventFilterEntity>, List<EventFilterEntity>)> GetGroupedEventFilterAsync(long currentHeight);
        Task SyncEventFiltersAsync();
        Task UpdateEventFiltersHeightAsync(EventFilterEntity filter, long latestHeight);
    }

    public class EventFiltersProvider : IEventFiltersProvider, ISingletonDependency
    {
        public const string ValidFilterIdsKey = "ValidFilterIdsKey";
        public const string ChangedFilterIdsKey = "ChangedFilterIdsKey";
        
        private readonly IDistributedCache<List<Guid>> _changedEventFilterIdsCache;
        private readonly IDistributedCache<EventFilterChangedEntity, Guid> _changedEventFilterCache;

        private readonly IDistributedCache<List<Guid>> _currentValidEventFilterIdsCache;
        private readonly IDistributedCache<EventFilterEntity, Guid> _currentValidEventFilterCache;

        private readonly List<EventFilterEntity> _currentValidEventFilters;
        private readonly List<EventFilterChangedEntity> _changedEventFilters;
        public EventFiltersProvider(IDistributedCache<EventFilterChangedEntity, Guid> changedEventFilterCache,
            IDistributedCache<EventFilterEntity, Guid> currentValidEventFilterCache,
            IDistributedCache<List<Guid>> changedEventFilterIdsCache,
            IDistributedCache<List<Guid>> currentValidEventFilterIdsCache)
        {
            _changedEventFilterCache = changedEventFilterCache;
            _currentValidEventFilterCache = currentValidEventFilterCache;
            _changedEventFilterIdsCache = changedEventFilterIdsCache;
            _currentValidEventFilterIdsCache = currentValidEventFilterIdsCache;
            _currentValidEventFilters = new List<EventFilterEntity>();
            _changedEventFilters = new List<EventFilterChangedEntity>();
        }

        public async Task InitializeEventFiltersAsync()
        {
            _currentValidEventFilters.Clear();
            var validEventFiltersFromCache = await InitializeValidEventFilterAsync();
            
            _currentValidEventFilters.AddRange(validEventFiltersFromCache);
            
            _changedEventFilters.Clear();
            var changedEventFilterFromCache = await InitializeChangedEventFilterAsync();
            _changedEventFilters.AddRange(changedEventFilterFromCache);
        }

        private async Task<List<EventFilterEntity>> InitializeValidEventFilterAsync()
        {
            var validIds = await _currentValidEventFilterIdsCache.GetAsync(ValidFilterIdsKey);
            if (validIds == null || !validIds.Any())
            {
                return null;
            }

            var validEventFilters = await _currentValidEventFilterCache.GetManyAsync(validIds);

            return validEventFilters.Select(validEventFilter => validEventFilter.Value).ToList();
        }

        private async Task<List<EventFilterChangedEntity>> InitializeChangedEventFilterAsync()
        {
            var changedFilterIds = await _changedEventFilterIdsCache.GetAsync(ChangedFilterIdsKey);
            if (changedFilterIds == null || !changedFilterIds.Any())
            {
                return null;
            }

            var changedEventFilters = await _changedEventFilterCache.GetManyAsync(changedFilterIds);

            return changedEventFilters.Select(eventFilter => eventFilter.Value).ToList();
        }

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

        public async Task SyncEventFiltersAsync()
        {
            var groupedByOperateType = _changedEventFilters.GroupBy(x => x.OperateType);
            var addedEventFilters = new List<EventFilterEntity>();
            var updatedEventFilters = new List<EventFilterChangedEntity>();
            var deleteIds = new List<Guid>();
            foreach (var changedEventFilters in groupedByOperateType)
            {
                switch (@changedEventFilters.Key)
                {
                    case EventFilterOperate.Add:
                        addedEventFilters.AddRange(changedEventFilters.Select(x => new EventFilterEntity(x)).ToList());
                        continue;
                    case EventFilterOperate.Update:
                        updatedEventFilters = changedEventFilters.ToList();
                        continue;
                    case EventFilterOperate.Delete:
                        deleteIds.AddRange(changedEventFilters.Select(x => x.Id));
                        continue;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            await DeleteAsync(deleteIds);
            await AddAsync(addedEventFilters);
            await UpdateAsync(updatedEventFilters);

            async Task DeleteAsync(List<Guid> ids)
            {
                if (!ids.Any())
                    return;
                await _currentValidEventFilterCache.RemoveManyAsync(ids);

                var currentValidIds = _currentValidEventFilters.Where(x => !ids.Contains(x.Id)).Select(x => x.Id).ToList();
                await _currentValidEventFilterIdsCache.SetAsync(ValidFilterIdsKey, currentValidIds);
            }
            
            async Task AddAsync(List<EventFilterEntity> newFilters)
            {
                _currentValidEventFilters.AddRange(newFilters);
                foreach (var newFilter in newFilters)
                {
                    await _currentValidEventFilterCache.SetAsync(newFilter.Id, newFilter);
                }
                
                var currentValidIds = _currentValidEventFilters.Select(x => x.Id).ToList();
                await _currentValidEventFilterIdsCache.SetAsync(ValidFilterIdsKey, currentValidIds);
            }
            
            async Task UpdateAsync(List<EventFilterChangedEntity> updatedFilters)
            {
                foreach (var updatedEventFilter in updatedFilters)
                {
                    var existedEventFilter =
                        _currentValidEventFilters.SingleOrDefault(x => x.Id == updatedEventFilter.Id);
                    existedEventFilter.FromEventFilterChanged(updatedEventFilter);
                    await _currentValidEventFilterCache.SetAsync(existedEventFilter.Id, existedEventFilter);
                }
            }

            async Task ClearChangedInfo()
            {
                await _changedEventFilterIdsCache.SetAsync(ChangedFilterIdsKey, null);
                var toDeleteIds = _changedEventFilters.Select(x => x.Id).ToList();
                _changedEventFilters.Clear();
                await _changedEventFilterCache.RemoveManyAsync(toDeleteIds);
            }
        }

        public Task UpdateEventFiltersHeightAsync(EventFilterEntity filter, long latestHeight)
        {
            throw new System.NotImplementedException();
        }
    }
}