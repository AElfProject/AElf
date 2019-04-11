using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Network.Application;
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