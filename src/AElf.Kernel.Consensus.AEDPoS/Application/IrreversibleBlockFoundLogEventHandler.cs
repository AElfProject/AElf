using System;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Miner.Application;
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
        private readonly ICachedBlockProvider _cachedBlockProvider;
        private readonly IForkCacheService _forkCacheService;

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
            ITaskQueueManager taskQueueManager, IBlockchainService blockchainService, 
            ICachedBlockProvider cachedBlockProvider, IForkCacheService forkCacheService)
        {
            _smartContractAddressService = smartContractAddressService;
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            _cachedBlockProvider = cachedBlockProvider;
            _forkCacheService = forkCacheService;

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
                ProcessForkCache(chain, blockIndex.BlockHash);
                    
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

        private void ProcessForkCache(Chain chain, Hash irreversibleBlockHash)
        {
            var block = _cachedBlockProvider.GetBlock(irreversibleBlockHash);
            var lastLibHeight = chain.LastIrreversibleBlockHeight;
            while (block != null && block.Height > lastLibHeight)
            {
                _forkCacheService.SetIrreversible(block.BlockHash);
                block = _cachedBlockProvider.GetBlock(block.PreviousBlockHash);
            }
        }
    }
}