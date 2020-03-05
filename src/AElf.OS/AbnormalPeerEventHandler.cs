using System.Threading.Tasks;
using AElf.OS.BlockSync.Events;
using AElf.OS.Network.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS
{
    public class AbnormalPeerEventHandler : ILocalEventHandler<AbnormalPeerFoundEventData>, ITransientDependency
    {
        private readonly INetworkService _networkService;

        public ILogger<AbnormalPeerEventHandler> Logger { get; set; }

        public AbnormalPeerEventHandler(INetworkService networkService)
        {
            Logger = NullLogger<AbnormalPeerEventHandler>.Instance;

            _networkService = networkService;
        }

        public async Task HandleEventAsync(AbnormalPeerFoundEventData eventData)
        {
            Logger.LogWarning(
                $"Remove abnormal peer: {eventData.PeerPubkey}, block hash: {eventData.BlockHash}, block height: {eventData.BlockHeight}");

            await _networkService.RemovePeerByPubkeyAsync(eventData.PeerPubkey, true);
        }
    }
}