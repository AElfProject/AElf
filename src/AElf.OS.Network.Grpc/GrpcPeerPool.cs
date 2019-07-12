using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Grpc
{

    public class PeerPool<T> : IPeerPool where T : IPeer
    {
        public ILogger<PeerPool<T>> Logger { get; set; }

        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        public int PeerCount => AuthenticatedPeers.Count;
        protected readonly ConcurrentDictionary<string, T> AuthenticatedPeers;
        
        public IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        private readonly ConcurrentDictionary<long, Hash> _recentBlockHeightAndHashMappings;

        public PeerPool()
        {
            AuthenticatedPeers = new ConcurrentDictionary<string, T>();
            
            _recentBlockHeightAndHashMappings = new ConcurrentDictionary<long, Hash>();
            RecentBlockHeightAndHashMappings = new ReadOnlyDictionary<long, Hash>(_recentBlockHeightAndHashMappings);

            Logger = NullLogger<PeerPool<T>>.Instance;
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

            return peers.Select(p => p as IPeer).ToList();
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
            
            AuthenticatedPeers.TryGetValue(publicKey, out T p);
            
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
            if (AuthenticatedPeers.TryRemove(publicKey, out T removed))
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

        public async Task ClearAllPeersAsync(bool sendDisconnect)
        {
            var peersToRemove = AuthenticatedPeers.Keys.ToList();
            
            foreach (string peer in peersToRemove)
            {
                await RemovePeerAsync(peer, sendDisconnect);
            }
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
    
    // TODO: Extract into a generic base class in OS.Core
    public class GrpcPeerPool : PeerPool<GrpcPeer>, ISingletonDependency
    {
        private readonly IAccountService _accountService;
        private readonly INodeManager _nodeManager;

        public GrpcPeerPool(IAccountService accountService, INodeManager nodeManager)
        {
            _accountService = accountService;
            _nodeManager = nodeManager;
        }
        
        public bool TryAddPeer(GrpcPeer p)
        {
            string localPubKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex();

            if (p.Info.Pubkey == localPubKey)
                throw new InvalidOperationException($"Connection to self detected {p.Info.Pubkey} ({p.IpAddress})");

            if (!AuthenticatedPeers.TryAdd(p.Info.Pubkey, p))
            {
                Logger.LogWarning($"Could not add peer {p.Info.Pubkey} ({p.IpAddress})");
                return false;
            }
            
            AsyncHelper.RunSync(() => _nodeManager.AddNodeAsync(new Node { Pubkey = p.Info.Pubkey.ToByteString(), Endpoint = p.IpAddress}));
            
            return true;
        }

        public GrpcPeer GetGrpcPeer(string pubkey)
        {
            AuthenticatedPeers.TryGetValue(pubkey, out GrpcPeer peer);
            return peer;
        }
    }
}