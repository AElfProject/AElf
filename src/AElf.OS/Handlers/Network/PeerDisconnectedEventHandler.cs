using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.Sdk.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class PeerDisconnectedEventHandler : ILocalEventHandler<PeerDisconnectedEventData>, ITransientDependency
    {
        private readonly IReconnectionService _reconnectionService;
        private readonly NetworkOptions _networkOptions;
        
        public ILogger<PeerDisconnectedEventHandler> Logger { get; set; }

        public PeerDisconnectedEventHandler(IOptionsSnapshot<NetworkOptions> networkOptions, IReconnectionService reconnectionService)
        {
            _reconnectionService = reconnectionService;
            _networkOptions = networkOptions.Value;
        }
        
        public Task HandleEventAsync(PeerDisconnectedEventData eventData)
        {
            string endpoint = eventData.NodeInfo?.Endpoint;

            if (eventData.IsInbound && (_networkOptions.BootNodes == null || !_networkOptions.BootNodes.Any() || !_networkOptions.BootNodes.Contains(endpoint)))
            {
                Logger.LogDebug($"Completely dropping {endpoint} (inbound: {eventData.IsInbound}).");
                return Task.CompletedTask;
            }

            var nextTry = TimestampHelper.GetUtcNow().AddMilliseconds(_networkOptions.PeerReconnectionPeriod + 1000);
                
            Logger.LogDebug($"Scheduling {endpoint} for reconnection at {nextTry}.");

            if (!_reconnectionService.SchedulePeerForReconnection(endpoint, nextTry))
                Logger.LogDebug($"Reconnection scheduling failed to {endpoint}.");
            
            return Task.CompletedTask;
        }
    }
}