using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    namespace AElf.OS.Network.Handler
    {
        public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
        {
            public INetworkService NetworkService { get; set; }

            public async Task HandleEventAsync(BlockAcceptedEvent eventData)
            {
                await NetworkService.BroadcastAnnounceAsync(eventData.BlockHeader);
            }
        }
    }
}