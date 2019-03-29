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
        private readonly IBlockchainService _blockchainService;
        private readonly IIrreversibleBlockDiscoveryService _irreversibleBlockDiscoveryService;
        public ILogger<BestChainFoundEventHandler> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }

        public BestChainFoundEventHandler(IBlockchainService blockchainService,
            IIrreversibleBlockDiscoveryService irreversibleBlockDiscoveryService)
        {
            _irreversibleBlockDiscoveryService = irreversibleBlockDiscoveryService;
            _blockchainService = blockchainService;
            LocalEventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<BestChainFoundEventHandler>.Instance;
        }


        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            var libHeights = await _irreversibleBlockDiscoveryService.DiscoverFromBlocksAsync(eventData.ExecutedBlocks);
            var chain = await _blockchainService.GetChainAsync();
            foreach (var height in libHeights)
            {
                var blockHash = await _blockchainService.GetBlockHashByHeightAsync(chain, height, chain.BestChainHash);

                await _blockchainService.SetIrreversibleBlockAsync(chain, height, blockHash);
            }
        }
    }
}