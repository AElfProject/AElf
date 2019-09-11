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

        protected readonly ConcurrentDictionary<string, IPeer> Peers;
        protected readonly ConcurrentDictionary<string, List<Handshake>> HandshakingPeers;

        public PeerPool()
        {
            Peers = new ConcurrentDictionary<string, IPeer>();
            HandshakingPeers = new ConcurrentDictionary<string, List<Handshake>>();
            Logger = NullLogger<PeerPool>.Instance;
        }

        public bool IsFull()
        {
            var peerCount = Peers.Where(p => !p.Value.IsInvalid).ToList().Count;
            return NetworkOptions.MaxPeers != 0 && peerCount >= NetworkOptions.MaxPeers;
        }

        public bool AddHandshakingPeer(IPEndPoint endpoint, Handshake handshake)
        {
            // check if the we've reached the maximum number of connections from this IP
            if (NetworkOptions.MaxPeersPerIpAddress != 0 && !endpoint.Address.Equals(IPAddress.Loopback))
            {
                int initiatedHandshakes = 0;
                if (HandshakingPeers.TryGetValue(endpoint.Address.ToString(), out List<Handshake> handshakes))
                    initiatedHandshakes = handshakes.Count;
                
                int peerFromIp = GetPeersByIpAddress(endpoint.Address).Count;
                if (peerFromIp + initiatedHandshakes >= NetworkOptions.MaxPeersPerIpAddress)
                {
                    Logger.LogWarning($"Max peers from {endpoint.Address} exceeded, current count {peerFromIp} " +
                                      $"(max. per ip {NetworkOptions.MaxPeersPerIpAddress}).");

                    return false;
                }
            }

            HandshakingPeers.AddOrUpdate(endpoint.Address.ToString(), new List<Handshake> { handshake },
                (key, handshakes) =>
                {
                    handshakes.Add(handshake);
                    return handshakes;
                });

            return true;
        }

        public bool RemoveHandshakingPeer(IPEndPoint endpoint, Handshake handshake)
        {
            bool removed = false;
            
            if (HandshakingPeers.TryGetValue(endpoint.Address.ToString(), out var handshakes))
            {
                // remove the corresponding handshake
                var toRemove = handshakes.FirstOrDefault(h => h.HandshakeData.Pubkey == handshake.HandshakeData.Pubkey);

                if (toRemove != null)
                {
                    handshakes.Remove(toRemove);
                    removed = true;
                }
                
                if (!handshakes.IsNullOrEmpty())
                    CleanHandshakes(endpoint.Address);
            }

            return removed;
        }

        private void CleanHandshakes(IPAddress ipAddress)
        {
            HandshakingPeers.TryRemove(ipAddress.ToString(), out _);
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