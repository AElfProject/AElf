using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    /// <summary>
    /// Manages all active connections to peers.
    /// </summary>
    public class PeerPool : IPeerPool, ISingletonDependency
    {
        private readonly IBlackListedPeerProvider _blackListedPeerProvider;
        public ILogger<PeerPool> Logger { get; set; }

        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        public int PeerCount => Peers.Count;
        public Dictionary<string, ConcurrentDictionary<string, string>> GetHandshakingPeers()
            => HandshakingPeers.ToDictionary(p => p.Key, p => p.Value);

        protected readonly ConcurrentDictionary<string, IPeer> Peers;
        protected readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> HandshakingPeers;

        public PeerPool(IBlackListedPeerProvider blackListedPeerProvider)
        {
            _blackListedPeerProvider = blackListedPeerProvider;
            Peers = new ConcurrentDictionary<string, IPeer>();
            HandshakingPeers = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
            Logger = NullLogger<PeerPool>.Instance;
        }

        public bool IsFull()
        {
            var peerCount = Peers.Where(p => !p.Value.IsInvalid).ToList().Count;
            return NetworkOptions.MaxPeers != 0 && peerCount >= NetworkOptions.MaxPeers;
        }

        public bool IsPeerBlackListed(string host)
        {
            return _blackListedPeerProvider.IsIpBlackListed(host);
        }

        public bool IsOverIpLimit(string host)
        {
            if (NetworkOptions.MaxPeersPerIpAddress == 0 || host.Equals(IPAddress.Loopback.ToString()))
                return false;
                
            int initiatedHandshakes = 0;
            if (HandshakingPeers.TryGetValue(host, out var handshakes))
                initiatedHandshakes = handshakes.Count;
                
            int peersFromIpCount = GetPeersByHost(host).Count;
            if (peersFromIpCount + initiatedHandshakes >= NetworkOptions.MaxPeersPerIpAddress)
            {
                Logger.LogWarning($"Max peers from {host} exceeded, current count {peersFromIpCount} " +
                                  $"(max. per ip {NetworkOptions.MaxPeersPerIpAddress}).");

                return true;
            }

            return false;
        }

        public bool AddHandshakingPeer(string host, string pubkey)
        {
            if (IsPeerBlackListed(host))
            {
                Logger.LogDebug($"{host} - peer pool is blacklisted.");
                return false;
            }

            // check if we have room for a new peer
            if (IsFull() || IsOverIpLimit(host))
            {
                Logger.LogWarning($"{host} - peer pool is full.");
                return false;
            }

            bool added = true;
            HandshakingPeers.AddOrUpdate(host, new ConcurrentDictionary<string, string> { [pubkey] = pubkey },
                (key, handshakes) =>
                {
                    if (IsOverIpLimit(host))
                    {
                        added = false;
                        Logger.LogWarning($"{host} - peer pool is full.");
                        return handshakes;
                    }

                    if (!handshakes.TryAdd(pubkey, pubkey))
                    {
                        added = false;
                        Logger.LogDebug($"{host} - pubkey {pubkey} is already handshaking.");
                    }
                    
                    return handshakes;
                });

            return added;
        }

        public bool RemoveHandshakingPeer(string host, string pubkey)
        {
            bool removed = false;
            if (HandshakingPeers.TryGetValue(host, out var pubkeys))
            {
                removed = pubkeys.TryRemove(pubkey, out _);

                if (pubkeys.IsNullOrEmpty())
                    HandshakingPeers.TryRemove(host, out _);
            }

            return removed;
        }

        public List<IPeer> GetPeers(bool includeFailing = false)
        {
            var peers = Peers.Select(p => p.Value);

            if (!includeFailing)
                peers = peers.Where(p => p.IsReady);

            return peers.Select(p => p).ToList();
        }

        public IPeer FindPeerByEndpoint(DnsEndPoint endpoint)
        {
            return Peers
                .Where(p => p.Value.RemoteEndpoint.Equals(endpoint))
                .Select(p => p.Value)
                .FirstOrDefault();
        }

        public IPeer FindPeerByPublicKey(string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey))
                return null;

            Peers.TryGetValue(publicKey, out IPeer p);

            return p;
        }

        public List<IPeer> GetPeersByHost(string host)
        {
            return Peers
                .Where(p => p.Value.RemoteEndpoint.Host.Equals(host))
                .Select(p => p.Value)
                .ToList();
        }

        public IPeer RemovePeer(string publicKey)
        {
            Peers.TryRemove(publicKey, out IPeer removed);
            return removed;
        }

        public bool TryAddPeer(IPeer peer)
        {
            return Peers.TryAdd(peer.Info.Pubkey, peer);
        }
        
        public bool TryReplace(string pubKey, IPeer oldPeer, IPeer newPeer)
        {
            return Peers.TryUpdate(pubKey, newPeer, oldPeer);
        }
    }
}