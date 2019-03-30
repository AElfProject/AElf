using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using AElf.Types.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable InconsistentNaming
    public class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly IIrreversibleBlockDiscoveryService _irreversibleBlockDiscoveryService;

        public BestChainFoundEventHandler(IIrreversibleBlockDiscoveryService irreversibleBlockDiscoveryService)
        {
            _irreversibleBlockDiscoveryService = irreversibleBlockDiscoveryService;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            await _irreversibleBlockDiscoveryService.DiscoverAndSetIrreversibleAsync(eventData.ExecutedBlocks);
        }
    }
}