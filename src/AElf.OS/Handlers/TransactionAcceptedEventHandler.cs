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

        public async Task HandleEventAsync(TransactionAcceptedEvent eventData)
        {
            // No need to wait for the result
            _ = NetworkService.BroadcastTransactionAsync(eventData.Transaction);
        }
    }
}