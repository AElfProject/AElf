using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.TransactionPool.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    /// <summary>
    /// TODO: add unit test
    /// Discover LIB from consensus contract then set LIB.
    /// </summary>
    internal class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly ITaskQueueManager _taskQueueManager;

        private readonly IBlockchainService _blockchainService;

        private readonly ITransactionInclusivenessProvider _transactionInclusivenessProvider;

        private readonly ContractEventDiscoveryService<IrreversibleBlockHeightUnacceptable>
            _unacceptableLibHeightEventDiscoveryService;

        private readonly ContractEventDiscoveryService<IrreversibleBlockFound>
            _irreversibleBlockFoundEventDiscoveryService;

        public ILogger<BestChainFoundEventHandler> Logger { get; set; }

        public BestChainFoundEventHandler(ITaskQueueManager taskQueueManager, IBlockchainService blockchainService,
            ITransactionInclusivenessProvider transactionInclusivenessProvider,
            ContractEventDiscoveryService<IrreversibleBlockFound> irreversibleBlockEventDiscoveryService,
            ContractEventDiscoveryService<IrreversibleBlockHeightUnacceptable>
                unacceptableLibHeightEventDiscoveryService)
        {
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            _transactionInclusivenessProvider = transactionInclusivenessProvider;
            _unacceptableLibHeightEventDiscoveryService = unacceptableLibHeightEventDiscoveryService;
            _irreversibleBlockFoundEventDiscoveryService = irreversibleBlockEventDiscoveryService;

            Logger = NullLogger<BestChainFoundEventHandler>.Instance;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            Logger.LogDebug(
                $"Handle best chain found for lib: BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");

            var chain = await _blockchainService.GetChainAsync();
            IBlockIndex blockIndex = null;
            foreach (var blockHash in eventData.ExecutedBlocks.AsEnumerable().Reverse())
            {
                var irreversibleBlockFound =
                    (await _irreversibleBlockFoundEventDiscoveryService.GetEventMessagesAsync(blockHash))
                    .FirstOrDefault();

                if (irreversibleBlockFound == null) continue;

                if (chain.LastIrreversibleBlockHeight >= irreversibleBlockFound.IrreversibleBlockHeight) continue;

                var libBlockHash = await _blockchainService.GetBlockHashByHeightAsync(chain,
                    irreversibleBlockFound.IrreversibleBlockHeight, blockHash);
                blockIndex = new BlockIndex(libBlockHash, irreversibleBlockFound.IrreversibleBlockHeight);
            }

            var distanceToLib =
                (await _unacceptableLibHeightEventDiscoveryService.GetEventMessagesAsync(eventData.BlockHash))
                .FirstOrDefault();

            if (distanceToLib != null && distanceToLib.DistanceToIrreversibleBlockHeight > 0)
            {
                Logger.LogDebug($"Distance to lib height: {distanceToLib.DistanceToIrreversibleBlockHeight}");
                _transactionInclusivenessProvider.IsTransactionPackable = false;
            }

            if (blockIndex != null)
            {
                _transactionInclusivenessProvider.IsTransactionPackable = true;
                _taskQueueManager.Enqueue(
                    async () =>
                    {
                        var currentChain = await _blockchainService.GetChainAsync();
                        if (currentChain.LastIrreversibleBlockHeight < blockIndex.Height)
                        {
                            await _blockchainService.SetIrreversibleBlockAsync(currentChain, blockIndex.Height,
                                blockIndex.Hash);
                        }
                    }, KernelConstants.UpdateChainQueueName);
            }

            Logger.LogDebug(
                $"Finish handle best chain found for lib : BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");
        }
    }
}