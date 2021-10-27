using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        int PeerCount { get; }

        bool IsFull();
        bool IsPeerBlackListed(string host);
        bool IsOverIpLimit(string host);

        bool AddHandshakingPeer(string host, string pubkey);
        bool RemoveHandshakingPeer(string host, string pubkey);
        Dictionary<string, ConcurrentDictionary<string, string>> GetHandshakingPeers();

        List<IPeer> GetPeers(bool includeFailing = false);

        IPeer FindPeerByEndpoint(DnsEndPoint peerEndpoint);
        IPeer FindPeerByPublicKey(string remotePubKey);

        List<IPeer> GetPeersByHost(string host);

        bool TryReplace(string pubkey, IPeer oldPeer, IPeer newPeer);
        bool TryAddPeer(IPeer peer);
        IPeer RemovePeer(string publicKey);
    }
}