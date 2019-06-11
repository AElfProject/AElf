using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    namespace AElf.OS.Network.Handler
    {
        public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
        {
            public INetworkService NetworkService { get; set; }
            public PeerService.PeerServiceBase Service { get; set; }

            public Task HandleEventAsync(BlockAcceptedEvent eventData)
            {
                NetworkService.BroadcastAnnounceAsync(eventData.BlockHeader, eventData.HasFork);
                
                if (Service is GrpcServerService s)
                    s.HandleEventAsync(eventData);
                
                return Task.CompletedTask;
            }
        }
    }
}