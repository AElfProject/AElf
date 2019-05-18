using System;
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

            public Task HandleEventAsync(BlockAcceptedEvent eventData)
            {
                NetworkService.BroadcastAnnounceAsync(eventData.BlockHeader);
                return Task.CompletedTask;
            }
        }
    }
}