using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface IBlackListedPeerProvider
    {
        bool AddIpToBlackList(IPAddress ipAddress);
        bool IsIpBlackListed(IPAddress address);
    }
    
    public class BlackListedPeerProvider : IBlackListedPeerProvider, ISingletonDependency
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        public ILogger<BlackListedPeerProvider> Logger { get; set; }
        
        private readonly ConcurrentDictionary<IPAddress, DateTime> _blackListedPeers;

        public BlackListedPeerProvider()
        {
            _blackListedPeers = new ConcurrentDictionary<IPAddress, DateTime>();
        }

        public bool AddIpToBlackList(IPAddress ipAddress)
        {
            return _blackListedPeers.TryAdd(ipAddress, DateTime.Now);
        }
        
        public bool IsIpBlackListed(IPAddress address)
        {
            CleanBlackListed();
            return _blackListedPeers.ContainsKey(address);
        }

        private void CleanBlackListed()
        {
            foreach (var blackListedPeer in _blackListedPeers)
            {
                if ((DateTime.Now - blackListedPeer.Value).TotalSeconds >= NetworkOptions.PeerBlackListTimeoutInSeconds 
                    && _blackListedPeers.TryRemove(blackListedPeer.Key, out _))
                {
                    Logger.LogDebug($"Removed blacklisted peer {blackListedPeer.Key}");
                }
            }
        }
    }
}