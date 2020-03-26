using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel
{
    public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
        ITransientDependency
    {
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IBlockchainStateService _blockchainStateService;
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionBlockIndexService _transactionBlockIndexService;
        private readonly IChainBlockLinkService _chainBlockLinkService;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        public ILogger<NewIrreversibleBlockFoundEventHandler> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }

        public NewIrreversibleBlockFoundEventHandler(ITaskQueueManager taskQueueManager,
            IBlockchainStateService blockchainStateService,
            IBlockchainService blockchainService,
            ITransactionBlockIndexService transactionBlockIndexService, 
            IChainBlockLinkService chainBlockLinkService,
            ISmartContractExecutiveService smartContractExecutiveService)
        {
            _taskQueueManager = taskQueueManager;
            _blockchainStateService = blockchainStateService;
            _blockchainService = blockchainService;
            _transactionBlockIndexService = transactionBlockIndexService;
            _chainBlockLinkService = chainBlockLinkService;
            _smartContractExecutiveService = smartContractExecutiveService;
            Logger = NullLogger<NewIrreversibleBlockFoundEventHandler>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            _taskQueueManager.Enqueue(async () =>
            {
                await _blockchainStateService.MergeBlockStateAsync(eventData.BlockHeight,
                    eventData.BlockHash);
                
                CleanChain(eventData.BlockHash, eventData.BlockHeight);
            }, KernelConstants.MergeBlockStateQueueName);

            return Task.CompletedTask;
        }

        private void CleanChain(Hash irreversibleBlockHash, long irreversibleBlockHeight)
        {
            _taskQueueManager.Enqueue(async () =>
            {
                // Clean BlockStateSet
                var discardedBlockHashes = _chainBlockLinkService.GetCachedChainBlockLinks()
                    .Where(b => b.Height <= irreversibleBlockHeight).Select(b => b.BlockHash).ToList();
                await _blockchainStateService.RemoveBlockStateSetsAsync(discardedBlockHashes);
                
                // Clean chain branch
                var chain = await _blockchainService.GetChainAsync();
                var discardedBranch = await _blockchainService.GetDiscardedBranchAsync(chain);

                _taskQueueManager.Enqueue(
                    async () =>
                    {
                        if (discardedBranch.BranchKeys.Count > 0 || discardedBranch.NotLinkedKeys.Count > 0)
                        {
                            await _blockchainService.CleanChainBranchAsync(discardedBranch);
                        }

                        await LocalEventBus.PublishAsync(new CleanBlockExecutedDataChangeHeightEventData
                        {
                            IrreversibleBlockHeight = irreversibleBlockHeight
                        });
                        _chainBlockLinkService.CleanCachedChainBlockLinks(irreversibleBlockHeight);
                    },
                    KernelConstants.UpdateChainQueueName);
                
                // Clean transaction block index cache
                await _transactionBlockIndexService.UpdateTransactionBlockIndicesByLibHeightAsync(irreversibleBlockHeight);
                
                // Clean idle executive
                _smartContractExecutiveService.CleanIdleExecutive();
            }, KernelConstants.ChainCleaningQueueName);
        }
    }
}