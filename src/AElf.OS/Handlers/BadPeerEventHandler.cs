using System.Threading.Tasks;
using AElf.OS.BlockSync.Events;
using AElf.OS.Network.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class BadPeerEventHandler : ILocalEventHandler<BlockValidationFailedEventData>,
        ILocalEventHandler<IncorrectIrreversibleBlockEventData>, 
        ITransientDependency
    {
        private readonly INetworkService _networkService;

        public BadPeerEventHandler(INetworkService networkService)
        {
            _networkService = networkService;
        }

        private async Task HandleBadPeerAsync(string peerPubkey)
        {
            await _networkService.RemovePeerByPubkeyAsync(peerPubkey);
        }

        public Task HandleEventAsync(BlockValidationFailedEventData eventData)
        {
            _ = HandleBadPeerAsync(eventData.BlockSenderPubkey);
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(IncorrectIrreversibleBlockEventData eventData)
        {
            _ = HandleBadPeerAsync(eventData.BlockSenderPubkey);
            return Task.CompletedTask;
        }
    }
}