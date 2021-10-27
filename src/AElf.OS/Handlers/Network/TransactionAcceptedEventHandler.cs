using System.Threading.Tasks;
using AElf.Kernel.TransactionPool;
using AElf.OS.Network.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class TransactionAcceptedEventHandler : ILocalEventHandler<TransactionAcceptedEvent>, ITransientDependency
    {
        public INetworkService NetworkService { get; set; }

        public Task HandleEventAsync(TransactionAcceptedEvent eventData)
        {
            _ = NetworkService.BroadcastTransactionAsync(eventData.Transaction);
            return Task.CompletedTask;
        }
    }
}