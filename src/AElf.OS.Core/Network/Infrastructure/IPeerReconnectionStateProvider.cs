using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public class ReconnectingPeer
    {
        public string Endpoint { get; set; }
        public Timestamp NextAttempt { get; set; }
    }

    public interface IPeerReconnectionStateProvider
    {
        bool AddReconnectingPeer(string peerEndpoint, ReconnectingPeer reconnectingPeer);
        List<ReconnectingPeer> GetPeersReadyForReconnection(Timestamp maxTime);
        bool RemoveReconnectionPeer(string peerEndpoint);
    }

    public class PeerReconnectionStateProvider : IPeerReconnectionStateProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, ReconnectingPeer> _reconnectingPeers;

        public PeerReconnectionStateProvider()
        {
            _reconnectingPeers = new ConcurrentDictionary<string, ReconnectingPeer>();
        }
        
        public bool AddReconnectingPeer(string peerEndpoint, ReconnectingPeer reconnectingPeer)
        {
            return _reconnectingPeers.TryAdd(peerEndpoint, reconnectingPeer);
        }

        public bool RemoveReconnectionPeer(string peerEndpoint)
        {
            return _reconnectingPeers.TryRemove(peerEndpoint, out _);
        }

        public List<ReconnectingPeer> GetPeersReadyForReconnection(Timestamp maxTime)
        {
            return _reconnectingPeers.Values.Where(rp => rp.NextAttempt.ToDateTime() < maxTime.ToDateTime()).ToList();
        }
    }
}