using AElf.OS.Network.Infrastructure;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Application
{
    public interface IReconnectionService
    {
        bool SchedulePeerForReconnection(string endpoint, Timestamp nextTry);
        bool RemovePeer(string endpoint);
    }

    public class ReconnectionService : IReconnectionService
    {
        public ILogger<ReconnectionService> Logger { get; set; }
        
        private readonly IPeerReconnectionStateProvider _reconnectionStateProvider;
        
        public ReconnectionService(IPeerReconnectionStateProvider reconnectionStateProvider)
        {
            _reconnectionStateProvider = reconnectionStateProvider;
        }
        
        public bool SchedulePeerForReconnection(string endpoint, Timestamp nextTry)
        {
            return _reconnectionStateProvider.AddReconnectingPeer(endpoint, 
                new ReconnectingPeer {Endpoint = endpoint, NextAttempt = nextTry });
        }

        public bool RemovePeer(string endpoint)
        {
            if (!_reconnectionStateProvider.RemoveReconnectionPeer(endpoint))
            {
                Logger.LogDebug($"Could not find {endpoint}");
                return false;
            }
            
            Logger.LogDebug($"Successfully removed {endpoint}");
            
            return true;
        }
    }
}