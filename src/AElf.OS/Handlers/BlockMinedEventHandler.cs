using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Network.Application;
using AElf.OS.Network.Extensions;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    namespace AElf.OS.Network.Handler
    {
        public class BlockMinedEventHandler : ILocalEventHandler<BlockMinedEventData>, ITransientDependency
        {
            public INetworkService NetworkService { get; set; }
            public IBlockchainService BlockchainService { get; set; }
            
            public ILogger<BlockMinedEventHandler> Logger { get; set; }

            public async Task HandleEventAsync(BlockMinedEventData eventData)
            {
                if (eventData?.BlockHeader == null)
                {
                    Logger.LogWarning("Block header is null, cannot broadcast.");
                    return;
                }
                
                var blockWithTransactions = await BlockchainService.GetBlockWithTransactionsByHash(eventData.BlockHeader.GetHash());
                
                if (blockWithTransactions == null)
                {
                    Logger.LogWarning($"Could not find {eventData.BlockHeader.GetHash()}.");
                    return;
                }
                
                var _ = NetworkService.BroadcastBlockWithTransactionsAsync(blockWithTransactions);
            }
        }
    }
}