using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.WebApp.MessageQueue.Entities;
using AElf.WebApp.MessageQueue.Helpers;
using AElf.WebApp.MessageQueue.Provider;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue.Services
{
    public interface ISendEventAsyncService
    {
        Task ProcessEventFilters(List<EventFilterEntity> eventFilter, CancellationToken ctsToken);
    }

    public class SendEventAsyncService : ISendEventAsyncService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IEventFiltersProvider _eventFiltersProvider;
        private readonly IMessageEventBusService _messageEventBusService;
        private readonly IEventFilterAsyncMessageGenerator _eventFilterAsyncMessageGenerator;
        private readonly ILogger<SendEventAsyncService> _logger;
        private readonly int _blockCount;

        public SendEventAsyncService(ILogger<SendEventAsyncService> logger, IBlockchainService blockchainService,
            IEventFiltersProvider eventFiltersProvider,
            IMessageEventBusService messageEventBusService,
            IEventFilterAsyncMessageGenerator eventFilterAsyncMessageGenerator)
        {
            _blockchainService = blockchainService;
            _eventFiltersProvider = eventFiltersProvider;
            _messageEventBusService = messageEventBusService;
            _eventFilterAsyncMessageGenerator = eventFilterAsyncMessageGenerator;
            _blockCount = 10;
            _logger = logger;
        }

        public async Task ProcessEventFilters(List<EventFilterEntity> eventFilters, CancellationToken ctsToken)
        {
            var startHeight = eventFilters.First().CurrentHeight + 1;
            var blocks = await GetBlockByHeight(startHeight, _blockCount);
            if (!blocks.Any())
            {
                return;
            }

            var eventFilterSets = EventFilterSetHelper.TransferToEventFilterSet(eventFilters);
            var eventFilterDic = eventFilters.ToDictionary(x => x.Id, x => x);
            var chainId = _blockchainService.GetChainId();
            foreach (var block in blocks)
            {
                var currentHeight = block.Height;
                if (ctsToken.IsCancellationRequested)
                {
                    return;
                }

                var messageDic = await _eventFilterAsyncMessageGenerator.GetEventMessageByBlockAsync(chainId,
                    new List<Block> { block }, eventFilterDic, eventFilterSets, ctsToken);
                if (messageDic == null)
                {
                    return;
                }

                if (!eventFilters.Any())
                {
                    continue;
                }

                foreach (var messageKp in messageDic)
                {
                    var eventFilter = eventFilterDic[messageKp.Key];
                    var topic = eventFilter.GetTopic();
                    await _messageEventBusService.PublishMessageAsync(topic, messageKp.Value);
                    await _eventFiltersProvider.UpdateEventFiltersHeightAsync(eventFilter,
                        currentHeight);
                }
            }
        }

        private async Task<List<Block>> GetBlockByHeight(long height, int count)
        {
            var chain = await _blockchainService.GetChainAsync();
            var hash = await _blockchainService.GetBlockHashByHeightAsync(chain, height, chain.LongestChainHash);
            return await _blockchainService.GetBlocksInLongestChainBranchAsync(hash, count);
        }
    }
}