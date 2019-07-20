using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using Secp256k1Net;
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