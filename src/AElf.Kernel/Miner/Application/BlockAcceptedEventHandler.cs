using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Miner.Application
{
    public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
    {
        private readonly ILogEventListeningService<IBlockAcceptedLogEventHandler> _logEventListeningService;

        public BlockAcceptedEventHandler(ILogEventListeningService<IBlockAcceptedLogEventHandler> logEventListeningService)
        {
            _logEventListeningService = logEventListeningService;
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            await _logEventListeningService.ApplyAsync(new[] {eventData.Block});
        }
    }
}