using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.WebApp.MessageQueue.Entities;
using AElf.WebApp.MessageQueue.Helpers;
using AElf.WebApp.MessageQueue.Provider;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue.Services
{
    public interface IMessagePublishService
    {
        Task PublishEventsAsync(int chainId, List<BlockExecutedSet> blockExecutedSets);
    }

    public class MessagePublishService : IMessagePublishService, ITransientDependency
    {
        private readonly IEventFiltersProvider _eventFiltersProvider;
        private readonly IEventSendTaskManager _eventSendTaskManager;
        private readonly IMessageEventBusService _messageEventBusService;
        private readonly IEventFilterSyncMessageGenerator _messageGenerator;

        public MessagePublishService(IEventFiltersProvider eventFiltersProvider,
            IEventSendTaskManager eventSendTaskManager,
            IMessageEventBusService messageEventBusService, IEventFilterSyncMessageGenerator messageGenerator)
        {
            _eventFiltersProvider = eventFiltersProvider;
            _eventSendTaskManager = eventSendTaskManager;
            _messageEventBusService = messageEventBusService;
            _messageGenerator = messageGenerator;
        }

        public async Task PublishEventsAsync(int chainId, List<BlockExecutedSet> blockExecutedSets)
        {
            await _eventSendTaskManager.StopAllAsync();
            var currentHeight = blockExecutedSets.First().Height - 1;
            await _eventFiltersProvider.SyncEventFiltersAsync();
            var (asyncEventFilters, syncEventFilters) =
                _eventFiltersProvider.GetGroupedEventFilters(currentHeight);
            _eventSendTaskManager.Start(asyncEventFilters);
            await SyncSendMsgAsync(chainId, blockExecutedSets, syncEventFilters);
        }

        private async Task SyncSendMsgAsync(int chainId, List<BlockExecutedSet> blockExecutedSets,
            List<EventFilterEntity> syncEventFilters)
        {
            var lastHeight = blockExecutedSets.Last().Height;
            var msgDic =
                await _messageGenerator.GetEventMessageByBlockAsync(chainId, blockExecutedSets, syncEventFilters);
            foreach (var eventFilter in syncEventFilters)
            {
                if (!msgDic.TryGetValue(eventFilter.Id, out var msg))
                {
                    continue;
                }

                await _messageEventBusService.PublishMessageAsync(eventFilter.GetTopic(), msg);
                await _eventFiltersProvider.UpdateEventFiltersHeightAsync(eventFilter, lastHeight);
            }
        }
    }
}