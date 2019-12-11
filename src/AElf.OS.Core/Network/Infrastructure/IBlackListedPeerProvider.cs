using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface IBlackListedPeerProvider
    {
        bool AddHostToBlackList(string host);
        bool IsIpBlackListed(string host);
    }
    
    public class BlackListedPeerProvider : IBlackListedPeerProvider, ISingletonDependency
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        public ILogger<BlackListedPeerProvider> Logger { get; set; }
        
        private readonly ConcurrentDictionary<string, Timestamp> _blackListedPeers;

        public BlackListedPeerProvider()
        {
            _blackListedPeers = new ConcurrentDictionary<string, Timestamp>();
        }

        public bool AddHostToBlackList(string host)
        {
            return _blackListedPeers.TryAdd(host, TimestampHelper.GetUtcNow());
        }
        
        public bool IsIpBlackListed(string host)
        {
            CleanBlackListed();
            return _blackListedPeers.ContainsKey(host);
        }

        private void CleanBlackListed()
        {
            foreach (var blackListedPeer in _blackListedPeers)
            {
                if ((TimestampHelper.GetUtcNow() - blackListedPeer.Value).Seconds >= NetworkOptions.PeerBlackListTimeoutInSeconds 
                    && _blackListedPeers.TryRemove(blackListedPeer.Key, out _))
                {
                    Logger.LogDebug($"Removed blacklisted peer {blackListedPeer.Key}");
                }
            }
        }
    }
}