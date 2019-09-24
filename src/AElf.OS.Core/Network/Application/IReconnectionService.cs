using AElf.OS.Network.Infrastructure;
using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.Network.Application
{
    public interface IReconnectionService
    {
        bool SchedulePeerForReconnection(string endpoint, Timestamp nextTry);
    }

    public class ReconnectionService : IReconnectionService
    {
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
    }
}