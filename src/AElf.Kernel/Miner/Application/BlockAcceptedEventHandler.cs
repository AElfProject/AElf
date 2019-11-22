using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Miner.Application
{
    public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
    {
        private readonly IBlockAcceptedLogEventListeningService _logEventListeningService;

        public BlockAcceptedEventHandler(IBlockAcceptedLogEventListeningService logEventListeningService)
        {
            _logEventListeningService = logEventListeningService;
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            await _logEventListeningService.ApplyAsync(new[] {eventData.Block});
        }
    }
}