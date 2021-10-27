using System.Collections.Generic;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
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
        ReconnectingPeer GetReconnectingPeer(string endpoint);
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

        public ReconnectingPeer GetReconnectingPeer(string endpoint)
        {
            return _reconnectionStateProvider.GetReconnectingPeer(endpoint);
        }

        public bool SchedulePeerForReconnection(string endpoint)
        {
            var nextTry = TimestampHelper.GetUtcNow().AddMilliseconds(NetworkOptions.PeerReconnectionPeriod);
                
            Logger.LogDebug($"Scheduling {endpoint} for reconnection at {nextTry}.");

            var reconnectingPeer = new ReconnectingPeer {
                Endpoint = endpoint, 
                NextAttempt = nextTry, 
                DisconnectionTime = TimestampHelper.GetUtcNow() 
            };

            if (!_reconnectionStateProvider.AddReconnectingPeer(endpoint, reconnectingPeer))
            {
                Logger.LogWarning($"Reconnection scheduling failed to {endpoint}.");
                return false;
            }

            return true;
        }

        public bool CancelReconnection(string endpoint)
        {
            if (!_reconnectionStateProvider.RemoveReconnectionPeer(endpoint))
            {
                Logger.LogWarning($"Could not find reconnection {endpoint}");
                return false;
            }
            
            Logger.LogDebug($"Successfully canceled reconnection {endpoint}");
            
            return true;
        }
    }
}