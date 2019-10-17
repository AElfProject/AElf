using System.Threading.Tasks;
using AElf.OS.BlockSync.Events;
using AElf.OS.Network.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class BadPeerEventHandler : ILocalEventHandler<BadPeerFoundEventData>, ITransientDependency
    {
        private readonly INetworkService _networkService;

        public ILogger<BadPeerEventHandler> Logger { get; set; }

        public BadPeerEventHandler(INetworkService networkService)
        {
            Logger = NullLogger<BadPeerEventHandler>.Instance;

            _networkService = networkService;
        }

        public Task HandleEventAsync(BadPeerFoundEventData eventData)
        {
            Logger.LogWarning(
                $"Remove bad peer: {eventData.PeerPubkey}, block hash: {eventData.BlockHash}, block height: {eventData.BlockHeight}");

            _ = _networkService.RemovePeerByPubkeyAsync(eventData.PeerPubkey, true);
            return Task.CompletedTask;
        }
    }
}