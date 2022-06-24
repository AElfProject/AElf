using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.WebApp.MessageQueue.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.WebApp.MessageQueue
{
    public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IMessagePublishService _messagePublishService;
        public ILogger<BlockAcceptedEventHandler> Logger { get; set; }

        public BlockAcceptedEventHandler(IBlockchainService blockchainService,
            IMessagePublishService messagePublishService)
        {
            _blockchainService = blockchainService;
            _messagePublishService = messagePublishService;
            Logger = NullLogger<BlockAcceptedEventHandler>.Instance;
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            var chain = await _blockchainService.GetChainAsync();
            await _messagePublishService.PublishEventsAsync(chain.Id,
                new List<BlockExecutedSet> { eventData.BlockExecutedSet });
        }
    }
}