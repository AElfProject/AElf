using System;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using AElf.CSharp.Core.Extension;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class IrreversibleBlockFoundLogEventProcessor : LogEventProcessorBase,
        IBlocksExecutionSucceededLogEventProcessor
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITaskQueueManager _taskQueueManager;

        public ILogger<IrreversibleBlockFoundLogEventProcessor> Logger { get; set; }

        public IrreversibleBlockFoundLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            IBlockchainService blockchainService, ITaskQueueManager taskQueueManager)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockchainService = blockchainService;
            _taskQueueManager = taskQueueManager;

            Logger = NullLogger<IrreversibleBlockFoundLogEventProcessor>.Instance;
        }

        public override async Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
        {
            if (InterestedEvent != null) return InterestedEvent;
            var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
                chainContext, ConsensusSmartContractAddressNameProvider.StringName);
            if (smartContractAddressDto == null) return null;

            var interestedEvent =
                GetInterestedEvent<IrreversibleBlockFound>(smartContractAddressDto.SmartContractAddress.Address);
            if (!smartContractAddressDto.Irreversible) return interestedEvent;

            InterestedEvent = interestedEvent;
            return InterestedEvent;
        }

        protected override async Task ProcessLogEventAsync(Block block, LogEvent logEvent)
        {
            var irreversibleBlockFound = new IrreversibleBlockFound();
            irreversibleBlockFound.MergeFrom(logEvent);
            await ProcessLogEventAsync(block, irreversibleBlockFound);
        }

        private async Task ProcessLogEventAsync(Block block, IrreversibleBlockFound irreversibleBlockFound)
        {
            try
            {
                var chain = await _blockchainService.GetChainAsync();

                if (chain.LastIrreversibleBlockHeight > irreversibleBlockFound.IrreversibleBlockHeight)
                    return;

                var libBlockHash = await _blockchainService.GetBlockHashByHeightAsync(chain,
                    irreversibleBlockFound.IrreversibleBlockHeight, block.GetHash());
                if (libBlockHash == null) return;

                if (chain.LastIrreversibleBlockHeight == irreversibleBlockFound.IrreversibleBlockHeight) return;

                var blockIndex = new BlockIndex(libBlockHash, irreversibleBlockFound.IrreversibleBlockHeight);
                Logger.LogDebug($"About to set new lib height: {blockIndex.BlockHeight} " +
                                $"Event: {irreversibleBlockFound} " +
                                $"BlockIndex: {blockIndex.BlockHash} - {blockIndex.BlockHeight}");
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
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to resolve IrreversibleBlockFound event.");
                throw;
            }
        }
    }
}