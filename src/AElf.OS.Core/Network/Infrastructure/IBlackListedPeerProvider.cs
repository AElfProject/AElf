using System.Collections.Concurrent;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface IBlackListedPeerProvider
    {
        bool AddHostToBlackList(string host, int limitSeconds);
        bool IsIpBlackListed(string host);
        bool RemoveHostFromBlackLis(string host);
    }
    
    public class BlackListedPeerProvider : IBlackListedPeerProvider, ISingletonDependency
    {
        public ILogger<BlackListedPeerProvider> Logger { get; set; }
        
        private readonly ConcurrentDictionary<string, Timestamp> _blackListedPeers;

        public BlackListedPeerProvider()
        {
            _blackListedPeers = new ConcurrentDictionary<string, Timestamp>();
        }

        public bool AddHostToBlackList(string host, int limitSeconds)
        {
            return _blackListedPeers.TryAdd(host, TimestampHelper.GetUtcNow().AddSeconds(limitSeconds));
        }

        public bool RemoveHostFromBlackLis(string host)
        {
            return _blackListedPeers.TryRemove(host, out _);
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
                if (TimestampHelper.GetUtcNow() > blackListedPeer.Value 
                    && _blackListedPeers.TryRemove(blackListedPeer.Key, out _))
                {
                    Logger.LogDebug($"Removed blacklisted peer {blackListedPeer.Key}");
                }
            }
        }
    }
}