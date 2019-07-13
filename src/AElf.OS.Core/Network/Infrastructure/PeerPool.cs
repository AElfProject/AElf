using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
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
        
        public IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        private readonly ConcurrentDictionary<long, Hash> _recentBlockHeightAndHashMappings;

        public PeerPool()
        {
            AuthenticatedPeers = new ConcurrentDictionary<string, IPeer>();
            
            _recentBlockHeightAndHashMappings = new ConcurrentDictionary<long, Hash>();
            RecentBlockHeightAndHashMappings = new ReadOnlyDictionary<long, Hash>(_recentBlockHeightAndHashMappings);

            Logger = NullLogger<PeerPool>.Instance;
        }
        
        public bool IsFull()
        {
            return PeerCount >= NetworkOptions.MaxPeers;
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
        
        public async Task<bool> RemovePeerByAddressAsync(string address)
        {
            var peer = AuthenticatedPeers.FirstOrDefault(p => p.Value.IpAddress == address).Value;

            if (peer != null) 
                return await RemovePeerAsync(peer.Info.Pubkey, true) != null;
            
            Logger.LogWarning($"Could not find peer {address}.");
            
            return false;
        }
        
        public async Task<IPeer> RemovePeerAsync(string publicKey, bool sendDisconnect)
        {
            if (AuthenticatedPeers.TryRemove(publicKey, out IPeer removed))
            {
                await removed.DisconnectAsync(sendDisconnect); // TODO remove
                Logger.LogDebug($"Removed peer {removed}");
            }
            else
            {
                Logger.LogDebug($"Could not find {publicKey}");
            }

            return removed;
        }

        public bool TryAddPeer(IPeer peer)
        {
            return AuthenticatedPeers.TryAdd(peer.Info.Pubkey, peer);
        }
        
        public void AddRecentBlockHeightAndHash(long blockHeight,Hash blockHash, bool hasFork)
        {
            _recentBlockHeightAndHashMappings[blockHeight] = blockHash;
            while (_recentBlockHeightAndHashMappings.Count > 10)
            {
                _recentBlockHeightAndHashMappings.TryRemove(_recentBlockHeightAndHashMappings.Keys.Min(), out _);
            }
        }
    }
}