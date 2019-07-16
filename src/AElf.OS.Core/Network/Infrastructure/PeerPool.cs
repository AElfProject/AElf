using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        public int PeerCount => AuthenticatedPeers.Count;
        protected readonly ConcurrentDictionary<string, IPeer> AuthenticatedPeers;

        public PeerPool()
        {
            AuthenticatedPeers = new ConcurrentDictionary<string, IPeer>();
            Logger = NullLogger<PeerPool>.Instance;
        }

        public bool IsFull()
        {
            return NetworkOptions.MaxPeers == 0 || PeerCount >= NetworkOptions.MaxPeers;
        }

        public List<IPeer> GetPeers(bool includeFailing = false)
        {
            var peers = AuthenticatedPeers.Select(p => p.Value);

            if (!includeFailing)
                peers = peers.Where(p => p.IsReady);

            return peers.Select(p => p).ToList();
        }

        public IPeer FindPeerByAddress(string peerAddress)
        {
            return AuthenticatedPeers
                .Where(p => p.Value.IpAddress == peerAddress)
                .Select(p => p.Value)
                .FirstOrDefault();
        }

        public IPeer FindPeerByPublicKey(string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey))
                return null;
            
            AuthenticatedPeers.TryGetValue(publicKey, out IPeer p);
            
            return p;
        }

        public IPeer GetBestPeer()
        {
            return GetPeers().FirstOrDefault(p => p.IsBest);
        }

        public IPeer RemovePeer(string publicKey)
        {
            AuthenticatedPeers.TryRemove(publicKey, out IPeer removed);
            return removed;
        }

        public bool TryAddPeer(IPeer peer)
        {
            return AuthenticatedPeers.TryAdd(peer.Info.Pubkey, peer);
        }
    }
}