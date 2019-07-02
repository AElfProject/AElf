using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Network.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    namespace AElf.OS.Network.Handler
    {
        public class BlockMinedEventHandler : ILocalEventHandler<BlockMinedEventData>, ITransientDependency
        {
            public INetworkService NetworkService { get; set; }

            public Task HandleEventAsync(BlockMinedEventData eventData)
            {
                NetworkService.BroadcastAnnounceAsync(eventData.BlockHeader, eventData.HasFork);
                return Task.CompletedTask;
            }
        }
    }
}