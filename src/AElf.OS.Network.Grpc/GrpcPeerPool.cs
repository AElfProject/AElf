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
using Volo.Abp.Threading;

namespace AElf.OS.Network.Grpc
{
    // TODO: Extract into a generic base class in OS.Core
    public class GrpcPeerPool : IPeerPool
    {
        public ILogger<GrpcPeerPool> Logger { get; set; }

        private readonly NetworkOptions _networkOptions;

        private readonly IAccountService _accountService;
        private readonly INodeManager _nodeManager;

        public int PeerCount => _authenticatedPeers.Count;
        private readonly ConcurrentDictionary<string, GrpcPeer> _authenticatedPeers;
        
        public IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        private readonly ConcurrentDictionary<long, Hash> _recentBlockHeightAndHashMappings;
        
        public GrpcPeerPool(IOptionsSnapshot<NetworkOptions> networkOptions, IAccountService accountService, 
            INodeManager nodeManager)
        {
            _networkOptions = networkOptions.Value;
            _accountService = accountService;
            _nodeManager = nodeManager;

            _authenticatedPeers = new ConcurrentDictionary<string, GrpcPeer>();
            
            _recentBlockHeightAndHashMappings = new ConcurrentDictionary<long, Hash>();
            RecentBlockHeightAndHashMappings = new ReadOnlyDictionary<long, Hash>(_recentBlockHeightAndHashMappings);

            Logger = NullLogger<GrpcPeerPool>.Instance;
        }

        public bool IsFull()
        {
            return PeerCount >= _networkOptions.MaxPeers;
        }
        
        public List<IPeer> GetPeers(bool includeFailing = false)
        {
            var peers = _authenticatedPeers.Select(p => p.Value);

            if (!includeFailing)
                peers = peers.Where(p => p.IsReady);

            return peers.Select(p => p as IPeer).ToList();
        }

        public IPeer FindPeerByAddress(string peerAddress)
        {
            return _authenticatedPeers
                .Where(p => p.Value.IpAddress == peerAddress)
                .Select(p => p.Value)
                .FirstOrDefault();
        }

        public IPeer FindPeerByPublicKey(string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey))
                return null;
            
            _authenticatedPeers.TryGetValue(publicKey, out GrpcPeer p);
            
            return p;
        }

        public IPeer GetBestPeer()
        {
            return GetPeers().FirstOrDefault(p => p.IsBest);
        }

        
        // TODO interface is T and this is defined as abstract.
        public bool TryAddPeer(IPeer peer)
        {
            if (!(peer is GrpcPeer p))
                return false;
            
            string localPubKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex();

            if (p.Info.Pubkey == localPubKey)
                throw new InvalidOperationException($"Connection to self detected {p.Info.Pubkey} ({p.IpAddress})");

            if (!_authenticatedPeers.TryAdd(p.Info.Pubkey, p))
            {
                Logger.LogWarning($"Could not add peer {p.Info.Pubkey} ({p.IpAddress})");
                return false;
            }
            
            AsyncHelper.RunSync(() => _nodeManager.AddNodeAsync(new Node { Pubkey = p.Info.Pubkey.ToByteString(), Endpoint = p.IpAddress}));
            
            return true;
        }

        public async Task<bool> RemovePeerByAddressAsync(string address)
        {
            var peer = _authenticatedPeers.FirstOrDefault(p => p.Value.IpAddress == address).Value;

            if (peer != null) 
                return await RemovePeerAsync(peer.Info.Pubkey, true) != null;
            
            Logger.LogWarning($"Could not find peer {address}.");
            
            return false;
        }
        
        public async Task<IPeer> RemovePeerAsync(string publicKey, bool sendDisconnect)
        {
            if (_authenticatedPeers.TryRemove(publicKey, out GrpcPeer removed))
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
            var peersToRemove = _authenticatedPeers.Keys.ToList();
            
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
}