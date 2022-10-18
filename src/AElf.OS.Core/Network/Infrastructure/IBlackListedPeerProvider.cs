using System.Collections.Concurrent;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure;

public interface IBlackListedPeerProvider
{
    bool AddHostToBlackList(string host, int limitSeconds);
    bool IsIpBlackListed(string host);
    bool RemoveHostFromBlackList(string host);
}

public class BlackListedPeerProvider : IBlackListedPeerProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, Timestamp> _blackListedPeers;

    public BlackListedPeerProvider()
    {
        _blackListedPeers = new ConcurrentDictionary<string, Timestamp>();
    }

    public ILogger<BlackListedPeerProvider> Logger { get; set; }

    public bool AddHostToBlackList(string host, int limitSeconds)
    {
        CleanBlackList();
        return _blackListedPeers.TryAdd(host, TimestampHelper.GetUtcNow().AddSeconds(limitSeconds));
    }

    public bool RemoveHostFromBlackList(string host)
    {
        Logger.LogDebug($"Removing blacklisted peer {host}");
        return _blackListedPeers.TryRemove(host, out _);
    }

    public bool IsIpBlackListed(string host)
    {
        if (!_blackListedPeers.TryGetValue(host, out var expirationDate))
            return false;

        if (IsOverdue(expirationDate))
        {
            RemoveHostFromBlackList(host);
            return false;
        }

        return true;
    }

    private bool IsOverdue(Timestamp expirationDate)
    {
        return TimestampHelper.GetUtcNow() > expirationDate;
    }

    private void CleanBlackList()
    {
        foreach (var blackListedPeer in _blackListedPeers)
            if (IsOverdue(blackListedPeer.Value))
                RemoveHostFromBlackList(blackListedPeer.Key);
    }
}