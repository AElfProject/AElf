using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class BlockReceivedEventHandler : ILocalEventHandler<BlockReceivedEvent>, ITransientDependency
    {
        private readonly IBlockSyncService _blockSyncService;
        private readonly IBlockSyncValidationService _blockSyncValidationService;
        private readonly IBlockchainService _blockchainService;

        public ILogger<BlockReceivedEventHandler> Logger { get; set; }

        public BlockReceivedEventHandler(IBlockSyncService blockSyncService,
            IBlockSyncValidationService blockSyncValidationService,
            IBlockchainService blockchainService)
        {
            _blockSyncService = blockSyncService;
            _blockSyncValidationService = blockSyncValidationService;
            _blockchainService = blockchainService;
        }

        public Task HandleEventAsync(BlockReceivedEvent eventData)
        {
            var _ = ProcessNewBlockAsync(eventData.BlockWithTransactions);
            return Task.CompletedTask;
        }

        private async Task ProcessNewBlockAsync(BlockWithTransactions blockWithTransactions)
        {
            Logger.LogDebug($"Start full block sync job, block: {blockWithTransactions}.");

            var chain = await _blockchainService.GetChainAsync();

            if (!await _blockSyncValidationService.ValidateBlockAsync(chain, blockWithTransactions))
            {
                return;
            }

            await _blockSyncService.SyncByBlockAsync(blockWithTransactions);
        }
    }
}