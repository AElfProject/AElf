using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Application;
using AElf.OS.Network.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    namespace AElf.OS.Network.Handler
    {
        public class BlockMinedEventHandler : ILocalEventHandler<BlockMinedEventData>, ITransientDependency
        {
            private readonly INetworkService _networkService;
            private readonly IBlockchainService _blockchainService;
            private readonly ISyncStateService _syncStateService;

            public ILogger<BlockMinedEventHandler> Logger { get; set; }

            public BlockMinedEventHandler(INetworkService networkService, IBlockchainService blockchainService,
                ISyncStateService syncStateService)
            {
                _networkService = networkService;
                _blockchainService = blockchainService;
                _syncStateService = syncStateService;
                
                Logger = NullLogger<BlockMinedEventHandler>.Instance;
            }

            public async Task HandleEventAsync(BlockMinedEventData eventData)
            {
                if (_syncStateService.SyncState != SyncState.Finished)
                {
                    return;
                }

                if (eventData?.BlockHeader == null)
                {
                    Logger.LogWarning("Block header is null, cannot broadcast.");
                    return;
                }

                var blockWithTransactions =
                    await _blockchainService.GetBlockWithTransactionsByHash(eventData.BlockHeader.GetHash());

                if (blockWithTransactions == null)
                {
                    Logger.LogWarning($"Could not find {eventData.BlockHeader.GetHash()}.");
                    return;
                }

                Logger.LogTrace(
                    $"Got full block hash {eventData.BlockHeader.GetHash()}, height {eventData.BlockHeader.Height}");

                var _ = _networkService.BroadcastBlockWithTransactionsAsync(blockWithTransactions);
            }
        }
    }
}