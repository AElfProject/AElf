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
        public ILogger<NewIrreversibleBlockFoundEventHandler> Logger { get; set; }

        public NewIrreversibleBlockFoundEventHandler(ITaskQueueManager taskQueueManager,
            IBlockchainStateMergingService blockchainStateMergingService)
        {
            _taskQueueManager = taskQueueManager;
            _blockchainStateMergingService = blockchainStateMergingService;
            Logger = NullLogger<NewIrreversibleBlockFoundEventHandler>.Instance;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            _taskQueueManager.GetQueue(KernelConsts.MergeBlockStateQueueName).Enqueue(async () =>
            {
                await _blockchainStateMergingService.MergeBlockStateAsync(eventData.BlockHeight,
                    eventData.BlockHash);
            });
        }
    }
}