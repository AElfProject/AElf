using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Miner.Application
{
    public class BestChainFoundEventHandlerForLogEventListening : ILocalEventHandler<BestChainFoundEventData>,
        ITransientDependency
    {
        private readonly ILogEventListeningService<IBestChainFoundLogEventProcessor> _logEventListeningService;

        public BestChainFoundEventHandlerForLogEventListening(
            ILogEventListeningService<IBestChainFoundLogEventProcessor> logEventListeningService)
        {
            _logEventListeningService = logEventListeningService;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            await _logEventListeningService.ProcessAsync(eventData.BlockExecutedSets.Select(p => p.Block).ToList());
        }
    }
}