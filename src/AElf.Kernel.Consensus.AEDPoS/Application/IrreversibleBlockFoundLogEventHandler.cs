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
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IChainBlockLinkService _chainBlockLinkService;
        private readonly IForkCacheService _forkCacheService;

        private LogEvent _interestedEvent;

        public ILogger<IrreversibleBlockFoundLogEventHandler> Logger { get; set; }

        public IrreversibleBlockFoundLogEventHandler(ISmartContractAddressService smartContractAddressService,
            IBlockchainService blockchainService, ITaskQueueManager taskQueueManager, IForkCacheService forkCacheService, 
            IChainBlockLinkService chainBlockLinkService)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockchainService = blockchainService;
            _taskQueueManager = taskQueueManager;
            _forkCacheService = forkCacheService;
            _chainBlockLinkService = chainBlockLinkService;

            Logger = NullLogger<IrreversibleBlockFoundLogEventHandler>.Instance;
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

        public async Task HandleAsync(Block block, TransactionResult result, LogEvent log)
        {
            var irreversibleBlockFound = new IrreversibleBlockFound();
            irreversibleBlockFound.MergeFrom(log);
            await ProcessLogEventAsync(block, irreversibleBlockFound);
        }

        private async Task ProcessLogEventAsync(Block block, IrreversibleBlockFound irreversibleBlockFound)
        {
            try
            {
                var chain = await _blockchainService.GetChainAsync();

                if (chain.LastIrreversibleBlockHeight >= irreversibleBlockFound.IrreversibleBlockHeight)
                    return;
                var libBlockHash = await _blockchainService.GetBlockHashByHeightAsync(chain,
                    irreversibleBlockFound.IrreversibleBlockHeight, block.GetHash());
                if (libBlockHash == null) return;
                var blockIndex = new BlockIndex(libBlockHash, irreversibleBlockFound.IrreversibleBlockHeight);
                ProcessForkCache(chain, blockIndex.BlockHash);
                
                Logger.LogDebug($"About to set new lib height: {blockIndex.BlockHeight}\n" +
                                $"Event: {irreversibleBlockFound}\n" +
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
                Logger.LogError($"Failed to resolve IrreversibleBlockFound event.\n{e.Message}\n{e.StackTrace}");
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