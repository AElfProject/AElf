using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Miner.Application
{
    public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
    {
        private readonly ILogEventListeningService<IBlockAcceptedLogEventProcessor> _logEventListeningService;

        public BlockAcceptedEventHandler(
            ILogEventListeningService<IBlockAcceptedLogEventProcessor> logEventListeningService)
        {
            _logEventListeningService = logEventListeningService;
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            await _logEventListeningService.ProcessAsync(new List<BlockExecutedSet> {eventData.BlockExecutedSet});
        }
    }
}