using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Miner.Application
{
    public class BlocksExecutionSucceededEventHandlerForLogEventListening : ILocalEventHandler<BlocksExecutionSucceededEvent>,
        ITransientDependency
    {
        private readonly ILogEventListeningService<IBlocksExecutionSucceededLogEventProcessor> _logEventListeningService;

        public BlocksExecutionSucceededEventHandlerForLogEventListening(ILogEventListeningService<IBlocksExecutionSucceededLogEventProcessor> logEventListeningService)
        {
            _logEventListeningService = logEventListeningService;
        }

        public async Task HandleEventAsync(BlocksExecutionSucceededEvent eventData)
        {
            await _logEventListeningService.ProcessAsync(eventData.ExecutedBlocks);
        }
    }
}