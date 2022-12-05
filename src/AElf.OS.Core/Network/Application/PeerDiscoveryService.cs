using System.Linq;
using System.Threading.Tasks;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Extensions;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp;

namespace AElf.OS.Network.Application;

public class PeerDiscoveryService : IPeerDiscoveryService
{
    private readonly IAElfNetworkServer _aelfNetworkServer;
    private readonly IDiscoveredNodeCacheProvider _discoveredNodeCacheProvider;
    private readonly INodeManager _nodeManager;
    private readonly IPeerDiscoveryJobProcessor _peerDiscoveryJobProcessor;
    private readonly IPeerPool _peerPool;

    public PeerDiscoveryService(IPeerPool peerPool, INodeManager nodeManager,
        IDiscoveredNodeCacheProvider discoveredNodeCacheProvider, IAElfNetworkServer aelfNetworkServer,
        IPeerDiscoveryJobProcessor peerDiscoveryJobProcessor)
    {
        _peerPool = peerPool;
        _nodeManager = nodeManager;
        _discoveredNodeCacheProvider = discoveredNodeCacheProvider;
        _aelfNetworkServer = aelfNetworkServer;
        _peerDiscoveryJobProcessor = peerDiscoveryJobProcessor;

        Logger = NullLogger<PeerDiscoveryService>.Instance;
    }

    public ILogger<PeerDiscoveryService> Logger { get; set; }

    public async Task DiscoverNodesAsync()
    {
        var peers = _peerPool.GetPeers()
            .OrderBy(x => RandomHelper.GetRandom())
            .Take(NetworkConstants.DefaultDiscoveryPeersToRequestCount)
            .ToList();

        foreach (var peer in peers)
        {
            var result = await _peerDiscoveryJobProcessor.SendDiscoveryJobAsync(peer);
            if (!result)
                Logger.LogWarning($"Send discovery job failed: {peer}");
        }
    }

    public async Task RefreshNodeAsync()
    {
        var endpoint = await TakeEndpointFromDiscoveredNodeCacheAsync();
        if (endpoint != null)
        {
            if (await _aelfNetworkServer.CheckEndpointAvailableAsync(endpoint))
            {
                _discoveredNodeCacheProvider.Add(endpoint);
                Logger.LogDebug($"Refresh node successfully: {endpoint}");
            }
            else
            {
                await _nodeManager.RemoveNodeAsync(endpoint);
                Logger.LogDebug($"Clean unavailable node: {endpoint}");
            }
        }
    }

    public async Task AddNodeAsync(NodeInfo nodeInfo)
    {
        if (await _nodeManager.AddNodeAsync(nodeInfo))
            _discoveredNodeCacheProvider.Add(nodeInfo.Endpoint);
    }

    public Task<NodeList> GetNodesAsync(int maxCount)
    {
        return _nodeManager.GetRandomNodesAsync(maxCount);
    }

    private async Task<string> TakeEndpointFromDiscoveredNodeCacheAsync()
    {
        while (_discoveredNodeCacheProvider.TryTake(out var endpoint))
            if (await _nodeManager.GetNodeAsync(endpoint) != null)
                return endpoint;

        return null;
    }
}