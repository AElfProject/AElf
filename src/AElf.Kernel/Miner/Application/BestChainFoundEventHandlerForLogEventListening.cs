using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Miner.Application
{
    public class BestChainFoundEventHandlerForLogEventListening : ILocalEventHandler<BestChainFoundEvent>,
        ITransientDependency
    {
        private readonly ILogEventListeningService _logEventListeningService;

        public BestChainFoundEventHandlerForLogEventListening(ILogEventListeningService logEventListeningService)
        {
            _logEventListeningService = logEventListeningService;
        }

        public async Task HandleEventAsync(BestChainFoundEvent eventData)
        {
            await _logEventListeningService.ApplyAsync(eventData.ExecutedBlocks);
        }
    }
}