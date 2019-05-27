using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeer : IPeer
    {
        private static readonly object _metricsLock = new object();
        
        private const int MaxMetricsPerMethod = 100;
        private const int DefaultRequestTimeoutMs = 700;

        private enum MetricNames
        {
            Announce,
            GetBlocks,
            GetBlock
        };
        
        public event EventHandler DisconnectionEvent;

        private readonly Channel _channel;
        private readonly PeerService.PeerServiceClient _client;

        /// <summary>
        /// Property that describes a valid state. Valid here means that the peer is ready to be used for communication.
        /// </summary>
        public bool IsReady
        {
            get { return _channel.State == ChannelState.Idle || _channel.State == ChannelState.Ready; }
        }

        public bool IsBest { get; set; }
        public Hash CurrentBlockHash { get; private set; }
        public long CurrentBlockHeight { get; private set; }
        public string PeerIpAddress { get; }
        public string PubKey { get; }
        public int ProtocolVersion { get; set; }
        public long ConnectionTime { get; set; }
        public bool Inbound { get; set; }
        public long StartHeight { get; set; }

        public IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        private readonly ConcurrentDictionary<long, Hash> _recentBlockHeightAndHashMappings;
        
        public IReadOnlyDictionary<string, List<RequestMetric>> RecentRequestsRoundtripTime { get; }
        private readonly ConcurrentDictionary<string, List<RequestMetric>> _recentRequestsRoundtripTimes;

        public GrpcPeer(Channel channel, PeerService.PeerServiceClient client, string pubKey, string peerIpAddress,
            int protocolVersion, long connectionTime, long startHeight, bool inbound = true)
        {
            _channel = channel;
            _client = client;

            PeerIpAddress = peerIpAddress;
            PubKey = pubKey;
            ProtocolVersion = protocolVersion;
            ConnectionTime = connectionTime;
            Inbound = inbound;
            StartHeight = startHeight;

            _recentBlockHeightAndHashMappings = new ConcurrentDictionary<long, Hash>();
            RecentBlockHeightAndHashMappings = new ReadOnlyDictionary<long, Hash>(_recentBlockHeightAndHashMappings);
            
            _recentRequestsRoundtripTimes = new ConcurrentDictionary<string, List<RequestMetric>>();
            RecentRequestsRoundtripTime = new ReadOnlyDictionary<string, List<RequestMetric>>(_recentRequestsRoundtripTimes);

            _recentRequestsRoundtripTimes.TryAdd(nameof(MetricNames.Announce), new List<RequestMetric>(MaxMetricsPerMethod));
            _recentRequestsRoundtripTimes.TryAdd(nameof(MetricNames.GetBlock), new List<RequestMetric>(MaxMetricsPerMethod));
            _recentRequestsRoundtripTimes.TryAdd(nameof(MetricNames.GetBlocks), new List<RequestMetric>(MaxMetricsPerMethod));
        }

        public Dictionary<string, List<RequestMetric>> GetRequestMetrics()
        {
            Dictionary<string, List<RequestMetric>> metrics = new Dictionary<string, List<RequestMetric>>();
            
            lock (_metricsLock)
            {
                foreach (var roundtripTime in _recentRequestsRoundtripTimes)
                {
                    var metricsToAdd = new List<RequestMetric>();
                    
                    metrics.Add(roundtripTime.Key, metricsToAdd);
                    foreach (var requestMetric in roundtripTime.Value)
                    {
                        metricsToAdd.Add(requestMetric);
                    }
                }
            }

            return metrics;
        }

        public async Task<BlockWithTransactions> RequestBlockAsync(Hash hash)
        {
            var blockRequest = new BlockRequest {Hash = hash};

            GrpcRequest request = new GrpcRequest
            {
                ErrorMessage = $"Block request for {hash} failed.",
                MetricName = nameof(MetricNames.GetBlock),
                MetricInfo = $"Block request for {hash}"
            };

            var blockReply 
                = await RequestAsync(_client, (c, d) => c.RequestBlockAsync(blockRequest, deadline: d), request);

            return blockReply?.Block;
        }

        public async Task<List<BlockWithTransactions>> GetBlocksAsync(Hash firstHash, int count)
        {
            var blockRequest = new BlocksRequest {PreviousBlockHash = firstHash, Count = count};
            var blockInfo = $"{{ first: {firstHash}, count: {count} }}";
            
            GrpcRequest request = new GrpcRequest
            {
                ErrorMessage = $"Get blocks for {blockInfo} failed.",
                MetricName = nameof(MetricNames.GetBlocks),
                MetricInfo = $"Get blocks for {blockInfo}"
            };

            var list = await RequestAsync(_client, (c, d) => c.RequestBlocksAsync(blockRequest, deadline: d), request);

            if (list == null)
                return new List<BlockWithTransactions>();

            return list.Blocks.ToList();
        }

        public async Task AnnounceAsync(PeerNewBlockAnnouncement header)
        {
            GrpcRequest request = new GrpcRequest
            {
                ErrorMessage = $"Bcast announce for {header.BlockHash} failed.",
                MetricName = nameof(MetricNames.Announce),
                MetricInfo = $"Block hash {header.BlockHash}"
            };
            
            await RequestAsync(_client, (c, d) => c.AnnounceAsync(header, deadline: d), request);
        }

        public async Task SendTransactionAsync(Transaction tx)
        {
            GrpcRequest request = new GrpcRequest
            {
                ErrorMessage = $"Bcast tx for {tx.GetHash()} failed."
            };
            
            await RequestAsync(_client, (c, d) => c.SendTransactionAsync(tx, deadline: d), request);
        }

        private async Task<TResp> RequestAsync<TResp>(PeerService.PeerServiceClient client,
            Func<PeerService.PeerServiceClient, DateTime, AsyncUnaryCall<TResp>> func, GrpcRequest requestParams)
        {
            var metricsName = requestParams.MetricName;
            bool timeRequest = !string.IsNullOrEmpty(metricsName);
            var timeoutMs = requestParams.TimeoutMs < 0 ? requestParams.TimeoutMs : DefaultRequestTimeoutMs;
            var dateBeforeRequest = DateTime.Now;
            var utcNow = DateTime.UtcNow;
            var timeout = utcNow.Add(TimeSpan.FromMilliseconds(timeoutMs));
            
            Stopwatch s = null;
            
            if (timeRequest)
                s = Stopwatch.StartNew();
                
            try
            {
                var response = await func(client, timeout);

                if (timeRequest)
                {
                    s.Stop();

                    lock (_metricsLock)
                    {
                        var metrics = _recentRequestsRoundtripTimes[metricsName];
                        
                        if (metrics.Count >= MaxMetricsPerMethod)
                            metrics.RemoveAt(0);
                        
                        metrics.Add(new RequestMetric
                        {
                            Info = requestParams.MetricInfo,
                            RequestTime = dateBeforeRequest,
                            MethodName = metricsName,
                            RoundTripTime = s.ElapsedMilliseconds
                        });
                    }
                }
                
                return response;
            }
            catch (AggregateException e)
            {
                HandleFailure(e.Flatten(), requestParams.ErrorMessage);
            }
            finally
            {
                s?.Stop();
            }

            return default(TResp);
        }

        /// <summary>
        /// This method handles the case where the peer is potentially down. If the Rpc call
        /// put the channel in TransientFailure or Connecting, we give the connection a certain time to recover.
        /// </summary>
        private void HandleFailure(AggregateException exceptions, string errorMessage)
        {
            // If channel has been shutdown (unrecoverable state) remove it.
            if (_channel.State == ChannelState.Shutdown)
            {
                DisconnectionEvent?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (_channel.State == ChannelState.TransientFailure || _channel.State == ChannelState.Connecting)
            {
                Task.Run(async () =>
                {
                    await _channel.TryWaitForStateChangedAsync(_channel.State,
                        DateTime.UtcNow.AddSeconds(NetworkConsts.DefaultPeerDialTimeout));

                    // Either we connected again or the state change wait timed out.
                    if (_channel.State == ChannelState.TransientFailure || _channel.State == ChannelState.Connecting)
                    {
                        await StopAsync();
                        DisconnectionEvent?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
            else
            {
                throw new NetworkException($"Failed request to {this}: {errorMessage}", exceptions);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                await _channel.ShutdownAsync();
            }
            catch (InvalidOperationException)
            {
                // If channel already shutdown
            }
        }

        public void HandlerRemoteAnnounce(PeerNewBlockAnnouncement peerNewBlockAnnouncement)
        {
            CurrentBlockHeight = peerNewBlockAnnouncement.BlockHeight;
            CurrentBlockHash = peerNewBlockAnnouncement.BlockHash;
            _recentBlockHeightAndHashMappings[CurrentBlockHeight] = CurrentBlockHash;
            while (_recentBlockHeightAndHashMappings.Count > 10)
            {
                _recentBlockHeightAndHashMappings.TryRemove(_recentBlockHeightAndHashMappings.Keys.Min(), out _);
            }
        }

        public async Task SendDisconnectAsync()
        {
            await _client.DisconnectAsync(new DisconnectReason {Why = DisconnectReason.Types.Reason.Shutdown});
        }

        public override string ToString()
        {
            return $"{{ listening-port: {PeerIpAddress}, key: {PubKey.Substring(0, 45)}... }}";
        }
    }
}