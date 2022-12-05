using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure;

public class ReconnectingPeer
{
    public string Endpoint { get; set; }
    public Timestamp NextAttempt { get; set; }
    public int RetryCount { get; set; }
    public Timestamp DisconnectionTime { get; set; }
}

public interface IPeerReconnectionStateProvider
{
    bool AddReconnectingPeer(string peerEndpoint, ReconnectingPeer reconnectingPeer);
    List<ReconnectingPeer> GetPeersReadyForReconnection(Timestamp maxTime);
    ReconnectingPeer GetReconnectingPeer(string peerEndpoint);
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

    public ReconnectingPeer GetReconnectingPeer(string peerEndpoint)
    {
        _reconnectingPeers.TryGetValue(peerEndpoint, out var reconnectingPeer);
        return reconnectingPeer;
    }

    public List<ReconnectingPeer> GetPeersReadyForReconnection(Timestamp maxTime)
    {
        if (maxTime == null)
            return _reconnectingPeers.Values.ToList();

        return _reconnectingPeers.Values.Where(rp => rp.NextAttempt < maxTime).ToList();
    }
}