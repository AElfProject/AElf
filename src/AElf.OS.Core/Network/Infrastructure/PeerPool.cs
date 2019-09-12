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
        public ILogger<PeerPool> Logger { get; set; }

        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        public int PeerCount => Peers.Count;
        public Dictionary<IPAddress, ConcurrentDictionary<string, string>> GetHandshakingPeers()
            => HandshakingPeers.ToDictionary(p => p.Key, p => p.Value);

        protected readonly ConcurrentDictionary<string, IPeer> Peers;
        protected readonly ConcurrentDictionary<IPAddress, ConcurrentDictionary<string, string>> HandshakingPeers;

        public PeerPool()
        {
            Peers = new ConcurrentDictionary<string, IPeer>();
            HandshakingPeers = new ConcurrentDictionary<IPAddress, ConcurrentDictionary<string, string>>();
            Logger = NullLogger<PeerPool>.Instance;
        }

        public bool IsFull()
        {
            var peerCount = Peers.Where(p => !p.Value.IsInvalid).ToList().Count;
            return NetworkOptions.MaxPeers != 0 && peerCount >= NetworkOptions.MaxPeers;
        }

        public bool AddHandshakingPeer(IPAddress ipAddress, string pubkey)
        {
            if (IsFull())
            {
                Logger.LogWarning("Peer pool is full.");
                return false;
            }

            // check if the we've reached the maximum number of connections from this IP
            if (IsOverIpLimit(ipAddress))
                return false;
            
            bool added = true;
            HandshakingPeers.AddOrUpdate(ipAddress, new ConcurrentDictionary<string, string> { [pubkey] = pubkey },
                (key, handshakes) =>
                {
                    if (IsOverIpLimit(ipAddress))
                    {
                        added = false;
                        return handshakes;
                    }

                    handshakes.TryAdd(pubkey, pubkey);
                    
                    return handshakes;
                });

            return added;
        }

        private bool IsOverIpLimit(IPAddress ipAddress)
        {
            if (NetworkOptions.MaxPeersPerIpAddress == 0 || ipAddress.Equals(IPAddress.Loopback))
                return false;
                
            int initiatedHandshakes = 0;
            if (HandshakingPeers.TryGetValue(ipAddress, out var handshakes))
                initiatedHandshakes = handshakes.Count;
                
            int peersFromIpCount = GetPeersByIpAddress(ipAddress).Count;
            if (peersFromIpCount + initiatedHandshakes >= NetworkOptions.MaxPeersPerIpAddress)
            {
                Logger.LogWarning($"Max peers from {ipAddress} exceeded, current count {peersFromIpCount} " +
                                  $"(max. per ip {NetworkOptions.MaxPeersPerIpAddress}).");

                return true;
            }

            return false;
        }

        public bool RemoveHandshakingPeer(IPAddress address, string pubkey)
        {
            bool removed = false;
            if (HandshakingPeers.TryGetValue(address, out var pubkeys))
            {
                removed = pubkeys.TryRemove(pubkey, out _);

                if (pubkeys.IsNullOrEmpty())
                    CleanHandshakes(address);
            }

            return removed;
        }

        private void CleanHandshakes(IPAddress ipAddress)
        {
            HandshakingPeers.TryRemove(ipAddress, out _);
        }

        public List<IPeer> GetPeers(bool includeFailing = false)
        {
            var peers = Peers.Select(p => p.Value);

            if (!includeFailing)
                peers = peers.Where(p => p.IsReady);

            return peers.Select(p => p).ToList();
        }

        public IPeer FindPeerByEndpoint(IPEndPoint endpoint)
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

        public List<IPeer> GetPeersByIpAddress(IPAddress ipAddress)
        {
            return Peers
                .Where(p => p.Value.RemoteEndpoint.Address.Equals(ipAddress))
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
            // clear invalid peer
            var invalidPeers = Peers.Where(p => p.Value.IsInvalid).ToList();

            foreach (var invalidPeer in invalidPeers)
            {
                var removedPeer = RemovePeer(invalidPeer.Key);
                removedPeer?.DisconnectAsync(false);
            }

            return Peers.TryAdd(peer.Info.Pubkey, peer);
        }
    }
}