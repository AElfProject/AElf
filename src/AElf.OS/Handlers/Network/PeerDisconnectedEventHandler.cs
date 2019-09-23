using System.Threading.Tasks;
using AElf.OS.Network.Events;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class PeerDisconnectedEventHandler : ILocalEventHandler<PeerDisconnectedEventData>
    {
        public async Task HandleEventAsync(PeerConnectedEventData eventData)
        {
            // todo add job to provider
        }
    }
}