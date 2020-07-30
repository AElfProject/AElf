using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Application;
using AElf.OS.Network.Extensions;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
            private readonly EvilTriggerOptions _evilTriggerOptions;

            public ILogger<BlockMinedEventHandler> Logger { get; set; }

            public BlockMinedEventHandler(INetworkService networkService, IBlockchainService blockchainService,
                ISyncStateService syncStateService, IOptionsMonitor<EvilTriggerOptions> evilTriggerOptions)
            {
                _networkService = networkService;
                _blockchainService = blockchainService;
                _syncStateService = syncStateService;
                _evilTriggerOptions = evilTriggerOptions.CurrentValue;

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
                    return;
                }

                var blockWithTransactions =
                    await _blockchainService.GetBlockWithTransactionsByHash(eventData.BlockHeader.GetHash());

                if (blockWithTransactions == null)
                {
                    Logger.LogWarning($"Could not find {eventData.BlockHeader.GetHash()}.");
                    return;
                }

                if (blockWithTransactions.Transactions.Count > 5 && _evilTriggerOptions.ChangeTransactionList &&
                    blockWithTransactions.Height % _evilTriggerOptions.EvilTriggerNumber == 0)
                {
                    var transaction = blockWithTransactions.Transactions;
                    blockWithTransactions.Transactions.RemoveAt(transaction.Count - 1);
                    Logger.LogWarning(
                        "EVIL TRIGGER - ChangeTransactionList - Remove last transaction from Transactions");
                }
                
                Logger.LogDebug(
                    $"Got full block hash {eventData.BlockHeader.GetHash()}, height {eventData.BlockHeader.Height}");

                var _ = _networkService.BroadcastBlockWithTransactionsAsync(blockWithTransactions);
            }
        }
    }
}