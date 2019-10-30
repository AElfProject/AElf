using System;
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

        private readonly ISmartContractAddressService _smartContractAddressService;

        private readonly ContractEventDiscoveryService<IrreversibleBlockFound>
            _irreversibleBlockFoundEventDiscoveryService;

        public ILogger<BestChainFoundEventHandler> Logger { get; set; }

        public BestChainFoundEventHandler(ITaskQueueManager taskQueueManager, IBlockchainService blockchainService,
            ITransactionInclusivenessProvider transactionInclusivenessProvider,
            ContractEventDiscoveryService<IrreversibleBlockFound> irreversibleBlockEventDiscoveryService,
            ContractEventDiscoveryService<IrreversibleBlockHeightUnacceptable>
                unacceptableLibHeightEventDiscoveryService,
            ISmartContractAddressService smartContractAddressService)
        {
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            _transactionInclusivenessProvider = transactionInclusivenessProvider;
            _unacceptableLibHeightEventDiscoveryService = unacceptableLibHeightEventDiscoveryService;
            _smartContractAddressService = smartContractAddressService;
            _irreversibleBlockFoundEventDiscoveryService = irreversibleBlockEventDiscoveryService;

            Logger = NullLogger<BestChainFoundEventHandler>.Instance;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            Logger.LogDebug(
                $"Handle best chain found for lib: BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");

            var consensusAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            try
            {
                var chain = await _blockchainService.GetChainAsync();
                IBlockIndex blockIndex = null;

                foreach (var blockHash in eventData.ExecutedBlocks.AsEnumerable().Reverse())
                {
                    var irreversibleBlockFound =
                        (await _irreversibleBlockFoundEventDiscoveryService.GetEventMessagesAsync(blockHash, consensusAddress))
                        .FirstOrDefault();

                    if (irreversibleBlockFound == null) continue;

                    if (chain.LastIrreversibleBlockHeight >= irreversibleBlockFound.IrreversibleBlockHeight) continue;

                    var libBlockHash = await _blockchainService.GetBlockHashByHeightAsync(chain,
                        irreversibleBlockFound.IrreversibleBlockHeight, blockHash);
                    blockIndex = new BlockIndex(libBlockHash, irreversibleBlockFound.IrreversibleBlockHeight);
                }

                var distanceToLib =
                    (await _unacceptableLibHeightEventDiscoveryService.GetEventMessagesAsync(eventData.BlockHash, consensusAddress))
                    .FirstOrDefault();
                if (distanceToLib != null && distanceToLib.DistanceToIrreversibleBlockHeight > 0)
                {
                    Logger.LogDebug($"Distance to lib height: {distanceToLib.DistanceToIrreversibleBlockHeight}");
                    _transactionInclusivenessProvider.IsTransactionPackable = false;
                }
                else
                {
                    _transactionInclusivenessProvider.IsTransactionPackable = true;
                }

                if (blockIndex != null)
                {
                    Logger.LogDebug($"About to set new lib height: {blockIndex.BlockHeight}");
                    _taskQueueManager.Enqueue(
                        async () =>
                        {
                            var currentChain = await _blockchainService.GetChainAsync();
                            if (currentChain.LastIrreversibleBlockHeight < blockIndex.BlockHeight)
                            {
                                await _blockchainService.SetIrreversibleBlockAsync(currentChain, blockIndex.BlockHeight,
                                    blockIndex.BlockHash);
                            }
                        }, KernelConstants.UpdateChainQueueName);
                }

                Logger.LogDebug(
                    $"Finish handle best chain found for lib : BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");
            }
            catch (Exception e)
            {
                Logger.LogError(
                    "Failed to resolve IrreversibleBlockFound or IrreversibleBlockHeightUnacceptable event.", e);
                throw;
            }
        }
    }
}