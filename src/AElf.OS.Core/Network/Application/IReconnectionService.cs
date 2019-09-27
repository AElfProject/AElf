using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.OS.Network.Application
{
    public interface IReconnectionService
    {
        bool SchedulePeerForReconnection(string endpoint);
        bool CancelReconnection(string endpoint);
        List<ReconnectingPeer> GetPeersReadyForReconnection(Timestamp maxTime);
        bool RemoveReconnectionPeer(string peerEndpoint);
    }

    public class ReconnectionService : IReconnectionService
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
        
        public ILogger<ReconnectionService> Logger { get; set; }
        
        private readonly IPeerReconnectionStateProvider _reconnectionStateProvider;
        
        public ReconnectionService(IPeerReconnectionStateProvider reconnectionStateProvider)
        {
            _reconnectionStateProvider = reconnectionStateProvider;
        }

        public List<ReconnectingPeer> GetPeersReadyForReconnection(Timestamp maxTime)
        {
            return _reconnectionStateProvider.GetPeersReadyForReconnection(maxTime);
        }

        public bool RemoveReconnectionPeer(string peerEndpoint)
        {
            return _reconnectionStateProvider.RemoveReconnectionPeer(peerEndpoint);
        }
        
        public bool SchedulePeerForReconnection(string endpoint)
        {
            var nextTry = TimestampHelper.GetUtcNow().AddMilliseconds(NetworkOptions.PeerReconnectionPeriod + 1000);
                
            Logger.LogDebug($"Scheduling {endpoint} for reconnection at {nextTry}.");

            var reconnectingPeer = new ReconnectingPeer {Endpoint = endpoint, NextAttempt = nextTry};

            if (!_reconnectionStateProvider.AddReconnectingPeer(endpoint, reconnectingPeer))
            {
                Logger.LogDebug($"Reconnection scheduling failed to {endpoint}.");
                return false;
            }

            return true;
        }

        public bool CancelReconnection(string endpoint)
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