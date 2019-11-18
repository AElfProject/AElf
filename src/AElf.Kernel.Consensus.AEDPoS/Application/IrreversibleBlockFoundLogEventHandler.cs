using System;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
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
        private readonly IChainBlockLinkService _chainBlockLinkService;
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
            ITaskQueueManager taskQueueManager, IBlockchainService blockchainService, IForkCacheService forkCacheService, 
            IChainBlockLinkService chainBlockLinkService)
        {
            _smartContractAddressService = smartContractAddressService;
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            _forkCacheService = forkCacheService;
            _chainBlockLinkService = chainBlockLinkService;

            Logger = NullLogger<IrreversibleBlockFoundLogEventHandler>.Instance;
        }

        public async Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new IrreversibleBlockFound();
            eventData.MergeFrom(logEvent);

            await ProcessLogEventAsync(block, eventData);
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
            var link = _chainBlockLinkService.GetCachedChainBlockLink(irreversibleBlockHash);
            var lastLibHeight = chain.LastIrreversibleBlockHeight;
            while (link != null && link.Height > lastLibHeight)
            {
                _forkCacheService.SetIrreversible(link.BlockHash);
                link = _chainBlockLinkService.GetCachedChainBlockLink(link.PreviousBlockHash);
            }
        }
    }
}