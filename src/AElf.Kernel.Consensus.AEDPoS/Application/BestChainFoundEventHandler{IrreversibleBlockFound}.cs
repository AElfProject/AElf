using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    /// <summary>
    /// Discover LIB from consensus contract then set LIB.
    /// </summary>
    internal class BestChainFoundEventHandlerForIrreversibleBlockFound : ILocalEventHandler<BestChainFoundEventData>,
        ITransientDependency
    {
        private readonly ITaskQueueManager _taskQueueManager;

        private readonly IBlockchainService _blockchainService;

        private readonly ISmartContractAddressService _smartContractAddressService;

        private readonly ContractEventDiscoveryService<IrreversibleBlockFound>
            _irreversibleBlockFoundEventDiscoveryService;

        public ILogger<BestChainFoundEventHandlerForIrreversibleBlockFound> Logger { get; set; }

        public BestChainFoundEventHandlerForIrreversibleBlockFound(ITaskQueueManager taskQueueManager,
            IBlockchainService blockchainService,
            ContractEventDiscoveryService<IrreversibleBlockFound> irreversibleBlockEventDiscoveryService,
            ISmartContractAddressService smartContractAddressService)
        {
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _irreversibleBlockFoundEventDiscoveryService = irreversibleBlockEventDiscoveryService;

            Logger = NullLogger<BestChainFoundEventHandlerForIrreversibleBlockFound>.Instance;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            Logger.LogDebug(
                $"Handle best chain found for lib in {nameof(BestChainFoundEventHandlerForIrreversibleBlockFound)}: " +
                $"BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");

            var consensusAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);

            try
            {
                await HandleIrreversibleBlockFoundEvent(eventData, consensusAddress);
            }
            catch (Exception e)
            {
                Logger.LogError(
                    $"Error while executing {nameof(HandleIrreversibleBlockFoundEvent)}.",
                    e);
                throw;
            }
        }

        private async Task HandleIrreversibleBlockFoundEvent(BestChainFoundEventData eventData,
            Address consensusAddress)
        {
            var chain = await _blockchainService.GetChainAsync();
            IBlockIndex blockIndex = null;

            foreach (var blockHash in eventData.ExecutedBlocks.AsEnumerable().Reverse())
            {
                var irreversibleBlockFound =
                    (await _irreversibleBlockFoundEventDiscoveryService.GetEventMessagesAsync(blockHash,
                        consensusAddress))
                    .FirstOrDefault();

                if (irreversibleBlockFound == null) continue;

                if (chain.LastIrreversibleBlockHeight >= irreversibleBlockFound.IrreversibleBlockHeight) continue;

                var libBlockHash = await _blockchainService.GetBlockHashByHeightAsync(chain,
                    irreversibleBlockFound.IrreversibleBlockHeight, blockHash);
                blockIndex = new BlockIndex(libBlockHash, irreversibleBlockFound.IrreversibleBlockHeight);
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
    }
}