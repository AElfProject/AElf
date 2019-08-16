using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    /// <summary>
    /// TODO: add unit test
    /// Discover LIB from consensus contract then set LIB.
    /// </summary>
    public class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly ITaskQueueManager _taskQueueManager;

        private readonly IIrreversibleBlockRelatedEventsDiscoveryService
            _irreversibleBlockRelatedEventsDiscoveryService;

        private readonly IBlockchainService _blockchainService;

        private readonly IIsPackageNormalTransactionProvider _isPackageNormalTransactionProvider;

        public ILogger<BestChainFoundEventHandler> Logger { get; set; }

        public BestChainFoundEventHandler(
            IIrreversibleBlockRelatedEventsDiscoveryService irreversibleBlockRelatedEventsDiscoveryService,
            ITaskQueueManager taskQueueManager, IBlockchainService blockchainService,
            IIsPackageNormalTransactionProvider isPackageNormalTransactionProvider)
        {
            _irreversibleBlockRelatedEventsDiscoveryService = irreversibleBlockRelatedEventsDiscoveryService;
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            _isPackageNormalTransactionProvider = isPackageNormalTransactionProvider;

            Logger = NullLogger<BestChainFoundEventHandler>.Instance;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            Logger.LogDebug(
                $"Handle best chain found for lib: BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");

            var chain = await _blockchainService.GetChainAsync();
            var index = await _irreversibleBlockRelatedEventsDiscoveryService.GetLastIrreversibleBlockIndexAsync(chain,
                eventData.ExecutedBlocks);

            var distanceToLib = await _irreversibleBlockRelatedEventsDiscoveryService
                .GetUnacceptableDistanceToLastIrreversibleBlockHeightAsync(eventData.BlockHash);

            if (distanceToLib > 1024)
            {
                _isPackageNormalTransactionProvider.IsPackage = false;
            }
            
            if (index != null)
            {
                _taskQueueManager.Enqueue(
                    async () =>
                    {
                        var currentChain = await _blockchainService.GetChainAsync();
                        if (currentChain.LastIrreversibleBlockHeight < index.Height)
                        {
                            await _blockchainService.SetIrreversibleBlockAsync(currentChain, index.Height, index.Hash);
                        }
                    }, KernelConstants.UpdateChainQueueName);
                _isPackageNormalTransactionProvider.IsPackage = true;
            }

            Logger.LogDebug(
                $"Finish handle best chain found for lib : BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");
        }
    }
}