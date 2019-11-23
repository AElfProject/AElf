using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Miner.Application
{
    public class BestChainFoundEventHandlerForLogEventListening : ILocalEventHandler<BestChainFoundEventData>,
        ITransientDependency
    {
        private readonly ILogEventListeningService<IBestChainFoundLogEventHandler> _logEventListeningService;

        public BestChainFoundEventHandlerForLogEventListening(ILogEventListeningService<IBestChainFoundLogEventHandler> logEventListeningService)
        {
            _logEventListeningService = logEventListeningService;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            await _logEventListeningService.ApplyAsync(eventData.ExecutedBlocks);
        }
    }
}