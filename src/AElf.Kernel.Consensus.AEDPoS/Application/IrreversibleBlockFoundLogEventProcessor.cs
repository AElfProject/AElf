using System;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Txn.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class IrreversibleBlockFoundLogEventProcessor : IBestChainFoundLogEventProcessor
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly TransactionPackingOptions _transactionPackingOptions;
        private LogEvent _interestedEvent;

        public ILogger<IrreversibleBlockFoundLogEventProcessor> Logger { get; set; }

        public IrreversibleBlockFoundLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            IBlockchainService blockchainService, ITaskQueueManager taskQueueManager,
            IOptionsMonitor<TransactionPackingOptions> transactionPackingOptions)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockchainService = blockchainService;
            _taskQueueManager = taskQueueManager;
            _transactionPackingOptions = transactionPackingOptions.CurrentValue;

            Logger = NullLogger<IrreversibleBlockFoundLogEventProcessor>.Instance;
        }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null) return _interestedEvent;
                var address =
                    _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                        .Name);
                _interestedEvent = new IrreversibleBlockFound().ToLogEvent(address);
                return _interestedEvent;
            }
        }

        public Task ProcessAsync(Block block, TransactionResult result, LogEvent log)
        {
            var irreversibleBlockFound = new IrreversibleBlockFound();
            irreversibleBlockFound.MergeFrom(log);
            var _ = ProcessLogEventAsync(block, irreversibleBlockFound);
            return Task.CompletedTask;
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

                // enable transaction packing
                _transactionPackingOptions.IsTransactionPackable = true;
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