using System;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Worker
{
    public class PeerReconnectionWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly IPeerPool _peerPool;
        private readonly IPeerReconnectionStateProvider _reconnectionStateProvider;
        private readonly INetworkService _networkService;

        private readonly NetworkOptions _networkOptions;
        
        public new ILogger<PeerReconnectionWorker> Logger { get; set; }

        public PeerReconnectionWorker(AbpTimer timer, IOptionsSnapshot<NetworkOptions> networkOptions, 
            IPeerReconnectionStateProvider reconnectionStateProvider, INetworkService networkService, IPeerPool peerPool)
            : base(timer)
        {
            _peerPool = peerPool;
            _reconnectionStateProvider = reconnectionStateProvider;
            _networkService = networkService;
            _networkOptions = networkOptions.Value;

            timer.Period = _networkOptions.PeerReconnectionPeriod;
        }

        protected override async void DoWork()
        {
            var peersToConnect = _reconnectionStateProvider.GetPeersReadyForReconnection(TimestampHelper.GetUtcNow());

            if (peersToConnect.Count <= 0)
            {
                Logger.LogDebug("No peers to reconnect.");
                return;
            }

            foreach (var peerToConnect in peersToConnect)
            {
                string peerEndpoint = peerToConnect.Endpoint;
                
                // check that we haven't already reconnected to this node
                if (_peerPool.FindPeerByEndpoint(IpEndpointHelper.Parse(peerEndpoint)) != null)
                {
                    Logger.LogDebug($"Peer {peerEndpoint} already in the pool, no need to reconnect.");

                    if (!_reconnectionStateProvider.RemoveReconnectionPeer(peerEndpoint))
                        Logger.LogDebug($"Could not find to {peerEndpoint}.");
                    
                    continue;
                }

                Logger.LogDebug($"Starting reconnection to {peerToConnect}.");

                var connected = false;
                
                try
                {
                    connected = await _networkService.AddPeerAsync(peerEndpoint);
                }
                catch (AggregateException)
                {
                    Logger.LogDebug($"Could not re-connect to {peerEndpoint}.");
                }

                if (connected)
                {
                    Logger.LogDebug($"Reconnection to {peerEndpoint} succeeded.");

                    if (!_reconnectionStateProvider.RemoveReconnectionPeer(peerEndpoint))
                        Logger.LogDebug($"Could not find {peerEndpoint}.");
                }
                else
                {
                    peerToConnect.NextAttempt =
                        TimestampHelper.GetUtcNow().AddMilliseconds(_networkOptions.PeerReconnectionPeriod);
                    
                    Logger.LogDebug($"Could not connect to {peerEndpoint}, next attempt {peerToConnect.NextAttempt}.");
                }
            }
        }
    }
}