using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Network.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
        ITransientDependency
    {
        private readonly INetworkService _NetworkService;

        public NewIrreversibleBlockFoundEventHandler(INetworkService networkService)
        {
            _NetworkService = networkService;
        }

        public Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            var _ = _NetworkService.BroadcastLibAnnounceAsync(eventData.BlockHash, eventData.BlockHeight);
            return Task.CompletedTask;
        }
    }
}