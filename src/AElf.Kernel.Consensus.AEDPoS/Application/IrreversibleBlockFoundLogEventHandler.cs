using System;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class IrreversibleBlockFoundLogEventHandler : ILogEventHandler, ISingletonDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IBlockchainService _blockchainService;

        private LogEvent _interestedEvent;

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address =
                    _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                        .Name);

                _interestedEvent = new IrreversibleBlockFound().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public ILogger<IrreversibleBlockFoundLogEventHandler> Logger { get; set; }

        public IrreversibleBlockFoundLogEventHandler(ISmartContractAddressService smartContractAddressService,
            ITaskQueueManager taskQueueManager, IBlockchainService blockchainService)
        {
            _smartContractAddressService = smartContractAddressService;
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;

            Logger = NullLogger<IrreversibleBlockFoundLogEventHandler>.Instance;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new IrreversibleBlockFound();
            eventData.MergeFrom(logEvent);

            var _ = ProcessLogEventAsync(block, eventData);

            return Task.CompletedTask;
        }

        private async Task ProcessLogEventAsync(Block block, IrreversibleBlockFound irreversibleBlockFound)
        {
            try
            {
                var chain = await _blockchainService.GetChainAsync();

                var libBlockHash = await _blockchainService.GetBlockHashByHeightAsync(chain,
                    irreversibleBlockFound.IrreversibleBlockHeight, block.GetHash());
                var blockIndex = new BlockIndex(libBlockHash, irreversibleBlockFound.IrreversibleBlockHeight);

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
            catch (Exception e)
            {
                Logger.LogError("Failed to resolve IrreversibleBlockFound event.", e);
                throw;
            }
        }
    }
}