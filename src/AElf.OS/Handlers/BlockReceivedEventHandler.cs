using System.Threading.Tasks;
using AElf.OS.BlockSync.Application;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class BlockReceivedEventHandler : ILocalEventHandler<BlockReceivedEvent>, ITransientDependency
    {
        private readonly IBlockSyncAttachService _blockSyncAttachService;

        public ILogger<BlockReceivedEventHandler> Logger { get; set; }

        public BlockReceivedEventHandler(IBlockSyncAttachService blockSyncAttachService)
        {
            _blockSyncAttachService = blockSyncAttachService;
        }

        public Task HandleEventAsync(BlockReceivedEvent eventData)
        {
            _blockSyncAttachService.EnqueueAttachBlockWithTransactionsJob(eventData.BlockWithTransactions);
            return Task.CompletedTask;
        }
    }
}