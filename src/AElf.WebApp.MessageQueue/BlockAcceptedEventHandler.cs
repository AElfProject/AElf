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
        private readonly List<BlockExecutedSet> _blockExecutedSets;
        private const int SkipCount = 10;
        private long _startSyncHeight;
        public ILogger<BlockAcceptedEventHandler> Logger { get; set; }

        public BlockAcceptedEventHandler(IBlockchainService blockchainService,
            IMessagePublishService messagePublishService)
        {
            _blockchainService = blockchainService;
            _messagePublishService = messagePublishService;
            Logger = NullLogger<BlockAcceptedEventHandler>.Instance;
            _blockExecutedSets = new List<BlockExecutedSet>();
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            if (_startSyncHeight == 0)
            {
                _startSyncHeight = eventData.Block.Height;
            }

            if (_blockExecutedSets.Count < SkipCount)
            {
                _blockExecutedSets.Add(eventData.BlockExecutedSet);
                return;
            }
          
            var chain = await _blockchainService.GetChainAsync();
            await _messagePublishService.PublishEventsAsync(chain.Id, _blockExecutedSets);
            _startSyncHeight = eventData.Block.Height + 1;
            _blockExecutedSets.Clear();
        }
    }
}