using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel
{
    public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
        ITransientDependency
    {
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IBlockchainStateMergingService _blockchainStateMergingService;
        private readonly IBlockchainService _blockchainService;
        public ILogger<NewIrreversibleBlockFoundEventHandler> Logger { get; set; }

        public NewIrreversibleBlockFoundEventHandler(ITaskQueueManager taskQueueManager,
            IBlockchainStateMergingService blockchainStateMergingService,
            IBlockchainService blockchainService)
        {
            _taskQueueManager = taskQueueManager;
            _blockchainStateMergingService = blockchainStateMergingService;
            _blockchainService = blockchainService;
            Logger = NullLogger<NewIrreversibleBlockFoundEventHandler>.Instance;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            _taskQueueManager.Enqueue(async () =>
            {
                await _blockchainStateMergingService.MergeBlockStateAsync(eventData.BlockHeight,
                    eventData.BlockHash);
            }, KernelConstants.MergeBlockStateQueueName);

            _taskQueueManager.Enqueue(async () =>
            {
                var chain = await _blockchainService.GetChainAsync();
                await _blockchainService.CleanChainBranchAsync(chain);
            }, KernelConstants.CleanChainBranchQueueName);
        }
    }
}