using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.OS.Worker
{
    public class PeerReconnectionWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly IPeerPool _peerPool;
        private readonly IReconnectionService _reconnectionService;
        private readonly INetworkService _networkService;

        private readonly NetworkOptions _networkOptions;
        
        public new ILogger<PeerReconnectionWorker> Logger { get; set; }

        public PeerReconnectionWorker(AbpTimer timer, IOptionsSnapshot<NetworkOptions> networkOptions, 
            INetworkService networkService, IPeerPool peerPool, IReconnectionService reconnectionService)
            : base(timer)
        {
            _peerPool = peerPool;
            _reconnectionService = reconnectionService;
            _networkService = networkService;
            _networkOptions = networkOptions.Value;

            timer.Period = _networkOptions.PeerReconnectionPeriod;
        }
        
        protected override void DoWork()
        {
            AsyncHelper.RunSync(DoReconnectionJobAsync);
        }

        internal async Task DoReconnectionJobAsync()
        {
            await _networkService.SendHealthChecksAsync();
            
            var peersToConnect = _reconnectionService.GetPeersReadyForReconnection(TimestampHelper.GetUtcNow());

            if (peersToConnect.Count <= 0)
            {
                Logger.LogDebug("No peers to reconnect.");
                return;
            }

            foreach (var peerToConnect in peersToConnect)
            {
                string peerEndpoint = peerToConnect.Endpoint;
                
                // check that we haven't already reconnected to this node
                if (_peerPool.FindPeerByEndpoint(IpEndPointHelper.Parse(peerEndpoint)) != null)
                {
                    Logger.LogDebug($"Peer {peerEndpoint} already in the pool, no need to reconnect.");

                    if (!_reconnectionService.CancelReconnection(peerEndpoint))
                        Logger.LogDebug($"Could not find to {peerEndpoint}.");
                    
                    continue;
                }

                Logger.LogDebug($"Starting reconnection to {peerToConnect.Endpoint}.");

                var connected = false;
                
                try
                {
                    connected = await _networkService.AddPeerAsync(peerEndpoint);
                }
                catch (Exception ex)
                {
                    // todo consider different handling of the exception in dialer 
                    // down the stack the AddPeerAsync rethrows any exception,
                    // in order to continue this job, Exception has to be catched for now.
                    Logger.LogError(ex, $"Could not re-connect to {peerEndpoint}.");
                }

                if (connected)
                {
                    Logger.LogDebug($"Reconnection to {peerEndpoint} succeeded.");

                    if (!_reconnectionService.CancelReconnection(peerEndpoint))
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