using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.OS.Worker
{
    public class PeerReconnectionWorker : PeriodicBackgroundWorkerBase
    {
        private readonly IPeerPool _peerPool;
        private readonly IReconnectionService _reconnectionService;
        private readonly INetworkService _networkService;

        private readonly NetworkOptions _networkOptions;

        public new ILogger<PeerReconnectionWorker> Logger { get; set; }

        public PeerReconnectionWorker(AbpTimer timer, IOptionsSnapshot<NetworkOptions> networkOptions, 
            INetworkService networkService, IPeerPool peerPool, IReconnectionService reconnectionService,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            _peerPool = peerPool;
            _reconnectionService = reconnectionService;
            _networkService = networkService;
            _networkOptions = networkOptions.Value;

            timer.Period = _networkOptions.PeerReconnectionPeriod;
        }
        
        protected override void DoWork(PeriodicBackgroundWorkerContext workerContext)
        {
            AsyncHelper.RunSync(DoReconnectionJobAsync);
        }

        internal async Task DoReconnectionJobAsync()
        {
            CheckNtpClockDrift();
                
            await _networkService.CheckPeersHealthAsync();
            
            var peersToConnect = _reconnectionService.GetPeersReadyForReconnection(TimestampHelper.GetUtcNow());

            if (peersToConnect.Count <= 0) 
                return;

            foreach (var peerToConnect in peersToConnect)
            {
                string peerEndpoint = peerToConnect.Endpoint;
                if (!AElfPeerEndpointHelper.TryParse(peerEndpoint, out var parsed))
                {
                    if (!_reconnectionService.CancelReconnection(peerEndpoint))
                        Logger.LogWarning($"Invalid {peerEndpoint}.");

                    continue;
                }

                // check that we haven't already reconnected to this node
                if (_peerPool.FindPeerByEndpoint(parsed) != null)
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
                    // down the stack the AddPeerAsync rethrows any exception,
                    // in order to continue this job, Exception has to be catched for now.
                    Logger.LogInformation(ex, $"Could not re-connect to {peerEndpoint}.");
                }

                if (connected)
                {
                    Logger.LogDebug($"Reconnection to {peerEndpoint} succeeded.");

                    if (!_reconnectionService.CancelReconnection(peerEndpoint))
                        Logger.LogDebug($"Could not find {peerEndpoint}.");
                }
                else
                {
                    var timeExtension = _networkOptions.PeerReconnectionPeriod * (int)Math.Pow(2, ++peerToConnect.RetryCount);
                    peerToConnect.NextAttempt = TimestampHelper.GetUtcNow().AddMilliseconds(timeExtension);

                    // if the option is set, verify that the next attempt does not exceed
                    // the maximum reconnection time. 
                    if (_networkOptions.MaximumReconnectionTime != 0)
                    {
                        var maxReconnectionDate = peerToConnect.DisconnectionTime +
                                      TimestampHelper.DurationFromMilliseconds(_networkOptions.MaximumReconnectionTime);

                        if (peerToConnect.NextAttempt > maxReconnectionDate)
                        {
                            _reconnectionService.CancelReconnection(peerEndpoint);
                            Logger.LogDebug($"Maximum reconnection time reached {peerEndpoint}, " +
                                            $"next was {peerToConnect.NextAttempt}.");
                            
                            continue;
                        }
                    }

                    Logger.LogDebug($"Could not connect to {peerEndpoint}, next attempt {peerToConnect.NextAttempt}, " +
                                    $"current retries {peerToConnect.RetryCount}.");
                }
            }

            void CheckNtpClockDrift()
            {
                try
                {
                    _networkService.CheckNtpDrift();
                }
                catch (Exception)
                {
                    // swallow any exception, we are not interested in anything else than valid checks. 
                }
            }
        }
    }
}