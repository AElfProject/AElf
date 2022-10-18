using System;
using System.Threading.Tasks;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.OS.Worker;

public class PeerDiscoveryWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly INetworkService _networkService;
    private readonly IPeerDiscoveryService _peerDiscoveryService;
    private readonly IReconnectionService _reconnectionService;

    public PeerDiscoveryWorker(AbpAsyncTimer timer, IPeerDiscoveryService peerDiscoveryService,
        INetworkService networkService,
        IReconnectionService reconnectionService,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _peerDiscoveryService = peerDiscoveryService;
        Timer.Period = NetworkConstants.DefaultDiscoveryPeriod;

        _networkService = networkService;
        _reconnectionService = reconnectionService;

        Logger = NullLogger<PeerDiscoveryWorker>.Instance;
    }

    public new ILogger<PeerDiscoveryWorker> Logger { get; set; }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessPeerDiscoveryJobAsync();
    }

    internal async Task ProcessPeerDiscoveryJobAsync()
    {
        await _peerDiscoveryService.RefreshNodeAsync();

        await _peerDiscoveryService.DiscoverNodesAsync();

        if (_networkService.IsPeerPoolFull()) return;

        var nodes = await _peerDiscoveryService.GetNodesAsync(10);
        foreach (var node in nodes.Nodes)
            try
            {
                if (_networkService.IsPeerPoolFull())
                {
                    Logger.LogTrace("Peer pool is full, aborting add.");
                    break;
                }

                var reconnectingPeer = _reconnectionService.GetReconnectingPeer(node.Endpoint);
                if (reconnectingPeer != null)
                {
                    Logger.LogDebug($"Peer {node.Endpoint} is already in the reconnection queue.");
                    continue;
                }

                if (_networkService.GetPeerByPubkey(node.Pubkey.ToHex()) != null)
                {
                    Logger.LogDebug($"Peer {node} is already in the peer pool.");
                    continue;
                }

                await _networkService.AddPeerAsync(node.Endpoint);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Exception connecting to {node.Endpoint}.");
            }
    }
}