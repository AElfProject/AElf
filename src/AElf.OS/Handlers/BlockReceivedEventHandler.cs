using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.BlockSync.Dto;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class BlockReceivedEventHandler : ILocalEventHandler<BlockReceivedEvent>, ITransientDependency
    {
        private readonly IBlockSyncService _blockSyncService;
        private readonly IBlockchainService _blockchainService;
        private readonly NetworkOptions _networkOptions;

        public BlockReceivedEventHandler(IBlockSyncService blockSyncService,
            IBlockchainService blockchainService,
            IOptionsSnapshot<NetworkOptions> networkOptions)
        {
            _blockSyncService = blockSyncService;
            _blockchainService = blockchainService;
            _networkOptions = networkOptions.Value;
        }

        public Task HandleEventAsync(BlockReceivedEvent eventData)
        {
            var _ = ProcessNewBlockAsync(eventData.BlockWithTransactions, eventData.SenderPubkey);
            return Task.CompletedTask;
        }

        private async Task ProcessNewBlockAsync(BlockWithTransactions blockWithTransactions, string senderPubkey)
        {
            var chain = await _blockchainService.GetChainAsync();

            await _blockSyncService.SyncByBlockAsync(chain, new SyncBlockDto
            {
                BlockWithTransactions = blockWithTransactions,
                BatchRequestBlockCount = _networkOptions.BlockIdRequestCount,
                SuggestedPeerPubkey = senderPubkey
            });
        }
    }
}