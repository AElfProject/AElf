using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Dtos;
using AElf.WebApp.MessageQueue.Entities;
using AElf.WebApp.MessageQueue.Enums;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace AElf.WebApp.MessageQueue.Provider
{
    public interface IEventFiltersProvider
    {
        Task InitializeEventFiltersAsync();
        Guid? Add(AddEventFilterInput input);
        bool Update(UpdateEventFilterInput input);
        bool Delete(DeleteEventFilterInput input);
        bool SetState(Guid id, bool isStop);
        List<EventFilterEntity> GetEventFilters(Guid? id = null);
        (List<EventFilterEntity>, List<EventFilterEntity>) GetGroupedEventFilters(long currentHeight);
        Task SyncEventFiltersAsync();
        Task UpdateEventFiltersHeightAsync(EventFilterEntity filter, long latestHeight);
    }

    public class EventFiltersProvider : IEventFiltersProvider, ISingletonDependency
    {
        private const string ValidFilterIdsKey = "ValidFilterIdsKey";

        private readonly IDistributedCache<List<Guid>> _currentValidEventFilterIdsCache;
        private readonly IDistributedCache<EventFilterEntity, Guid> _currentValidEventFilterCache;

        private readonly List<EventFilterEntity> _currentValidEventFilters;
        private readonly ConcurrentDictionary<Guid, EventFilterToChange> _eventFiltersToChange;

        private readonly IGuidGenerator _guidGenerator;
        public EventFiltersProvider(
            IDistributedCache<EventFilterEntity, Guid> currentValidEventFilterCache,
            IDistributedCache<List<Guid>> currentValidEventFilterIdsCache, IGuidGenerator guidGenerator)
        {
            _currentValidEventFilterCache = currentValidEventFilterCache;
            _currentValidEventFilterIdsCache = currentValidEventFilterIdsCache;
            _guidGenerator = guidGenerator;
            _currentValidEventFilters = new List<EventFilterEntity>();
            _eventFiltersToChange = new ConcurrentDictionary<Guid, EventFilterToChange>();
        }

        public async Task InitializeEventFiltersAsync()
        {
            _currentValidEventFilters.Clear();
            var validEventFiltersFromCache = await InitializeValidEventFilterAsync();
            _currentValidEventFilters.AddRange(validEventFiltersFromCache);
        }
        
        public Guid? Add(AddEventFilterInput input)
        {
            var newId = _guidGenerator.Create();
            var newEventFilter = new EventFilterToChange(newId)
            {
                Status = EventFilterStatus.Stopped,
                CurrentHeight = 0,
                OperateType = EventFilterOperate.Add,
                EventDetails = input.EventDetails,
                FromHeight = input.FromHeight,
                ToHeight = input.ToHeight
            };
            var isSuccess = _eventFiltersToChange.TryAdd(newId, newEventFilter);
            if (isSuccess)
            {
                return newId;
            }

            return null;
        }

        public bool Update(UpdateEventFilterInput input)
        {
            if (_currentValidEventFilters.All(x => x.Id != input.Id))
            {
                return false;
            }
            
            if (!_eventFiltersToChange.TryGetValue(input.Id, out _))
                return _eventFiltersToChange.TryAdd(input.Id, new EventFilterToChange(input.Id)
                {
                    Status = EventFilterStatus.Stopped,
                    CurrentHeight = 0,
                    OperateType = EventFilterOperate.Update,
                    EventDetails = input.EventDetails,
                    FromHeight = input.FromHeight,
                    ToHeight = input.ToHeight
                });
            if (!_eventFiltersToChange.TryRemove(input.Id, out var target))
            {
                return false;
            }

            target.EventDetails = input.EventDetails;
            target.FromHeight = input.FromHeight;
            target.ToHeight = input.ToHeight;
            return _eventFiltersToChange.TryAdd(input.Id, target);
        }

        public bool Delete(DeleteEventFilterInput input)
        {
            if (_eventFiltersToChange.TryAdd(input.Id, new EventFilterToChange(input.Id)
                {
                    OperateType = EventFilterOperate.Delete
                }))
            {
                return true;
            }

            // todo add log
            return false;
        }

        public bool SetState(Guid id, bool isStop)
        {
            if (_currentValidEventFilters.All(x => x.Id != id))
            {
                return false;
            }

            var eventFiltersToChange = isStop
                ? new EventFilterToChange(id)
                {
                    OperateType = EventFilterOperate.Stop
                }
                : new EventFilterToChange(id)
                {
                    OperateType = EventFilterOperate.Prepared
                };
            return _eventFiltersToChange.TryAdd(id, eventFiltersToChange);
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

        public List<EventFilterEntity> GetEventFilters(Guid? id)
        {
            if (!id.HasValue)
            {
                return _currentValidEventFilters.ToList();
            }

            var target = _currentValidEventFilters.FirstOrDefault(x => x.Id == id.Value);
            return target != null ? new List<EventFilterEntity> { target } : null;
        }

        public (List<EventFilterEntity>, List<EventFilterEntity>) GetGroupedEventFilters(
            long blockStartHeight)
        {
            var currentHeight = blockStartHeight - 1;
            var allEventFilters = GetEventFilters(null);
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
            var eventFiltersToChange = _eventFiltersToChange.Select(x => x.Value).ToList();
            _eventFiltersToChange.Clear();
            var groupedByOperateType = eventFiltersToChange.GroupBy(x => x.OperateType);
            var addedEventFilters = new List<EventFilterEntity>();
            var updatedEventFilters = new List<EventFilterToChange>();
            var deleteIds = new List<Guid>();
            foreach (var groupedChangedEventFilters in groupedByOperateType)
            {
                switch (groupedChangedEventFilters.Key)
                {
                    case EventFilterOperate.Add:
                        addedEventFilters.AddRange(groupedChangedEventFilters.Select(x => new EventFilterEntity(x)).ToList());
                        continue;
                    case EventFilterOperate.Update:
                    case EventFilterOperate.Stop:
                    case EventFilterOperate.Prepared:
                        updatedEventFilters.AddRange(groupedChangedEventFilters.ToList());
                        continue;
                    case EventFilterOperate.Delete:
                        deleteIds.AddRange(groupedChangedEventFilters.Select(x => x.Id));
                        continue;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var hasDeleteOperation = await DeleteAsync(deleteIds);
            var hasAddOperation = await AddAsync(addedEventFilters);
            await UpdateAsync(updatedEventFilters);

            if (!hasAddOperation && !hasDeleteOperation)
            {
                return;
            }

            var newValidIds = _currentValidEventFilters.Select(x => x.Id).ToList();
            await _currentValidEventFilterIdsCache.SetAsync(ValidFilterIdsKey, newValidIds);

            async Task<bool> DeleteAsync(List<Guid> ids)
            {
                if (!ids.Any())
                    return false;
                await _currentValidEventFilterCache.RemoveManyAsync(ids);
                _currentValidEventFilters.RemoveAll(x => ids.Contains(x.Id));
                return true;
            }
            
            async Task<bool> AddAsync(List<EventFilterEntity> newFilters)
            {
                if (newFilters.Any())
                {
                    return false;
                }
                _currentValidEventFilters.AddRange(newFilters);
                foreach (var newFilter in newFilters)
                {
                    await _currentValidEventFilterCache.SetAsync(newFilter.Id, newFilter);
                }

                return true;
            }

            async Task UpdateAsync(List<EventFilterToChange> updatedFilters)
            {
                foreach (var updatedEventFilter in updatedFilters)
                {
                    var existedEventFilter =
                        _currentValidEventFilters.SingleOrDefault(x => x.Id == updatedEventFilter.Id);
                    if (existedEventFilter == null)
                    {
                        continue; // todo add log
                    }

                    if (updatedEventFilter.OperateType == EventFilterOperate.Update)
                    {
                        existedEventFilter.FromEventFilterToChange(updatedEventFilter);
                    }
                    else if(updatedEventFilter.OperateType == EventFilterOperate.Prepared)
                    {
                        existedEventFilter.Status = EventFilterStatus.Prepared;
                    }
                    else
                    {
                        existedEventFilter.Status = EventFilterStatus.Stopped;
                    }
                    
                    await _currentValidEventFilterCache.SetAsync(existedEventFilter.Id, existedEventFilter);
                }
            }
        }

        public async Task UpdateEventFiltersHeightAsync(EventFilterEntity filter, long latestHeight)
        {
            filter.CurrentHeight = latestHeight;
            await _currentValidEventFilterCache.SetAsync(filter.Id, filter);
        }
    }
}