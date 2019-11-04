using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class IrreversibleBlockFoundLogEventHandler : ILogEventHandler
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly TaskQueueManager _taskQueueManager;
        private LogEvent _interestedEvent;

        public ILogger<IrreversibleBlockFoundLogEventHandler> Logger { get; set; }

        public IrreversibleBlockFoundLogEventHandler(ISmartContractAddressService smartContractAddressService,
            IBlockchainService blockchainService, TaskQueueManager taskQueueManager)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockchainService = blockchainService;
            _taskQueueManager = taskQueueManager;

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

        public async Task Handle(Block block, TransactionResult result, LogEvent log)
        {
            var chain = await _blockchainService.GetChainAsync();
            var irreversibleBlockFound = new IrreversibleBlockFound();
            irreversibleBlockFound.MergeFrom(log);
            if (chain.LastIrreversibleBlockHeight >= irreversibleBlockFound.IrreversibleBlockHeight)
                return;
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
    }
}