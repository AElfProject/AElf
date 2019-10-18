using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.TransactionPool.Application;
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

        private readonly ITransactionInclusivenessProvider _transactionInclusivenessProvider;

        public ILogger<BestChainFoundEventHandler> Logger { get; set; }

        public BestChainFoundEventHandler(
            IIrreversibleBlockRelatedEventsDiscoveryService irreversibleBlockRelatedEventsDiscoveryService,
            ITaskQueueManager taskQueueManager, IBlockchainService blockchainService,
            ITransactionInclusivenessProvider transactionInclusivenessProvider)
        {
            _irreversibleBlockRelatedEventsDiscoveryService = irreversibleBlockRelatedEventsDiscoveryService;
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            _transactionInclusivenessProvider = transactionInclusivenessProvider;

            Logger = NullLogger<BestChainFoundEventHandler>.Instance;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            Logger.LogDebug(
                $"Handle best chain found for lib: BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");

            var chain = await _blockchainService.GetChainAsync();
            var index = await _irreversibleBlockRelatedEventsDiscoveryService.GetLastIrreversibleBlockIndexAsync(chain,
                eventData.ExecutedBlocks);

            var unacceptableDistanceToLib = await _irreversibleBlockRelatedEventsDiscoveryService
                .GetUnacceptableDistanceToLastIrreversibleBlockHeightAsync(eventData.BlockHash);

            if (unacceptableDistanceToLib > 0)
            {
                Logger.LogDebug($"Unacceptable distance to lib height: {unacceptableDistanceToLib}");
                _transactionInclusivenessProvider.IsTransactionPackable = false;
            }
            else
            {
                _transactionInclusivenessProvider.IsTransactionPackable = true;
            }

            if (index != null)
            {
                _taskQueueManager.Enqueue(
                    async () =>
                    {
                        var currentChain = await _blockchainService.GetChainAsync();
                        if (currentChain.LastIrreversibleBlockHeight < index.BlockHeight)
                        {
                            await _blockchainService.SetIrreversibleBlockAsync(currentChain, index.BlockHeight, index.BlockHash);
                        }
                    }, KernelConstants.UpdateChainQueueName);
            }

            Logger.LogDebug(
                $"Finish handle best chain found for lib : BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");
        }
    }
}