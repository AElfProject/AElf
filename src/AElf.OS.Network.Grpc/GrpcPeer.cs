using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeer : IPeer
    {
        private const int MaxMetricsPerMethod = 100;
        
        private const int AnnouncementTimeout = 300;
        private const int BlockRequestTimeout = 300;
        private const int TransactionBroadcastTimeout = 300;
        private const int BlocksRequestTimeout = 500;
        
        private enum MetricNames
        {
            Announce,
            GetBlocks,
            GetBlock,
            PreLibAnnounce
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
        public int ProtocolVersion { get; }
        public long ConnectionTime { get; }
        public bool Inbound { get; }
        public long StartHeight { get; }

        public IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        private readonly ConcurrentDictionary<long, Hash> _recentBlockHeightAndHashMappings;
        

        public IReadOnlyDictionary<long, Hash> PreLibBlockHeightAndHashMappings { get; }
        private readonly ConcurrentDictionary<long, Hash> _preLibBlockHeightAndHashMappings;
        
        public IReadOnlyDictionary<string, ConcurrentQueue<RequestMetric>> RecentRequestsRoundtripTimes { get; }
        private readonly ConcurrentDictionary<string, ConcurrentQueue<RequestMetric>> _recentRequestsRoundtripTimes;

        public GrpcPeer(Channel channel, PeerService.PeerServiceClient client, GrpcPeerInfo peerInfo)
        {
            _channel = channel;
            _client = client;

            PeerIpAddress = peerInfo.PeerIpAddress;
            PubKey = peerInfo.PublicKey;
            ProtocolVersion = peerInfo.ProtocolVersion;
            ConnectionTime = peerInfo.ConnectionTime;
            Inbound = peerInfo.IsInbound;
            StartHeight = peerInfo.StartHeight;

            _recentBlockHeightAndHashMappings = new ConcurrentDictionary<long, Hash>();
            RecentBlockHeightAndHashMappings = new ReadOnlyDictionary<long, Hash>(_recentBlockHeightAndHashMappings);
            
            _preLibBlockHeightAndHashMappings = new ConcurrentDictionary<long, Hash>();
            PreLibBlockHeightAndHashMappings = new ReadOnlyDictionary<long, Hash>(_preLibBlockHeightAndHashMappings);
            
            _recentRequestsRoundtripTimes = new ConcurrentDictionary<string, ConcurrentQueue<RequestMetric>>();
            RecentRequestsRoundtripTimes = new ReadOnlyDictionary<string, ConcurrentQueue<RequestMetric>>(_recentRequestsRoundtripTimes);

            _recentRequestsRoundtripTimes.TryAdd(nameof(MetricNames.Announce), new ConcurrentQueue<RequestMetric>());
            _recentRequestsRoundtripTimes.TryAdd(nameof(MetricNames.GetBlock), new ConcurrentQueue<RequestMetric>());
            _recentRequestsRoundtripTimes.TryAdd(nameof(MetricNames.GetBlocks), new ConcurrentQueue<RequestMetric>());
        }

        public Dictionary<string, List<RequestMetric>> GetRequestMetrics()
        {
            Dictionary<string, List<RequestMetric>> metrics = new Dictionary<string, List<RequestMetric>>();

            foreach (var roundtripTime in _recentRequestsRoundtripTimes.ToArray())
            {
                var metricsToAdd = new List<RequestMetric>();
                
                metrics.Add(roundtripTime.Key, metricsToAdd);
                foreach (var requestMetric in roundtripTime.Value)
                {
                    metricsToAdd.Add(requestMetric);
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

            Metadata data = new Metadata { {GrpcConstants.TimeoutMetadataKey, BlockRequestTimeout.ToString()} };

            var blockReply 
                = await RequestAsync(_client, c => c.RequestBlockAsync(blockRequest, data), request);

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

            Metadata data = new Metadata { {GrpcConstants.TimeoutMetadataKey, BlocksRequestTimeout.ToString()} };

            var list = await RequestAsync(_client, c => c.RequestBlocksAsync(blockRequest, data), request);

            if (list == null)
                return new List<BlockWithTransactions>();

            return list.Blocks.ToList();
        }

        public Task AnnounceAsync(PeerNewBlockAnnouncement header)
        {
            GrpcRequest request = new GrpcRequest
            {
                ErrorMessage = $"Broadcast announce for {header.BlockHash} failed.",
                MetricName = nameof(MetricNames.Announce),
                MetricInfo = $"Block hash {header.BlockHash}"
            };

            Metadata data = new Metadata { {GrpcConstants.TimeoutMetadataKey, AnnouncementTimeout.ToString()} };

            return RequestAsync(_client, c => c.AnnounceAsync(header, data), request);
        }
        
        public Task PreLibAnnounceAsync(PeerPreLibAnnouncement peerPreLibAnnouncement)
        {
            var request = new GrpcRequest
            {
                ErrorMessage = $"Broadcast announce for {peerPreLibAnnouncement.BlockHash} failed.",
                MetricName = nameof(MetricNames.PreLibAnnounce),
                MetricInfo = $"Block hash {peerPreLibAnnouncement.BlockHash}"
            };

            var data = new Metadata { {GrpcConstants.TimeoutMetadataKey, AnnouncementTimeout.ToString()} };

            return RequestAsync(_client, c => c.PreLibAnnounceAsync(peerPreLibAnnouncement, data), request);
        }
        
        

        public Task SendTransactionAsync(Transaction tx)
        {
            GrpcRequest request = new GrpcRequest
            {
                ErrorMessage = $"Broadcast transaction for {tx.GetHash()} failed."
            };
            
            Metadata data = new Metadata
            {
                {GrpcConstants.TimeoutMetadataKey, TransactionBroadcastTimeout.ToString()}
            };
            
            return RequestAsync(_client, c => c.SendTransactionAsync(tx, data), request);
        }

        private async Task<TResp> RequestAsync<TResp>(PeerService.PeerServiceClient client,
            Func<PeerService.PeerServiceClient, AsyncUnaryCall<TResp>> func, GrpcRequest requestParams)
        {
            var metricsName = requestParams.MetricName;
            bool timeRequest = !string.IsNullOrEmpty(metricsName);
            var requestStartTime = TimestampHelper.GetUtcNow();
            
            Stopwatch requestTimer = null;
            
            if (timeRequest)
                requestTimer = Stopwatch.StartNew();
                
            try
            {
                var response = await func(client);

                if (timeRequest)
                {
                    requestTimer.Stop();
                    RecordMetric(requestParams, requestStartTime, requestTimer.ElapsedMilliseconds);
                }
                
                return response;
            }
            catch (AggregateException e)
            {
                HandleFailure(e.Flatten(), requestParams.ErrorMessage);
            }
            finally
            {
                if (timeRequest)
                {
                    requestTimer.Stop();
                    RecordMetric(requestParams, requestStartTime, requestTimer.ElapsedMilliseconds);
                }
            }

            return default(TResp);
        }

        private void RecordMetric(GrpcRequest grpcRequest, Timestamp requestStartTime, long elapsedMilliseconds)
        {
            var metrics = _recentRequestsRoundtripTimes[grpcRequest.MetricName];
                    
            while (metrics.Count >= MaxMetricsPerMethod)
                metrics.TryDequeue(out _);
                    
            metrics.Enqueue(new RequestMetric
            {
                Info = grpcRequest.MetricInfo,
                RequestTime = requestStartTime,
                MethodName = grpcRequest.MetricName,
                RoundTripTime = elapsedMilliseconds
            });
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
                        DateTime.UtcNow.AddSeconds(NetworkConstants.DefaultPeerDialTimeoutInMilliSeconds));

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
            if (peerNewBlockAnnouncement.HasFork)
            {
                _recentBlockHeightAndHashMappings.Clear();
                return;
            }
            
            CurrentBlockHeight = peerNewBlockAnnouncement.BlockHeight;
            CurrentBlockHash = peerNewBlockAnnouncement.BlockHash;
            _recentBlockHeightAndHashMappings[CurrentBlockHeight] = CurrentBlockHash;
            while (_recentBlockHeightAndHashMappings.Count > 10)
            {
                _recentBlockHeightAndHashMappings.TryRemove(_recentBlockHeightAndHashMappings.Keys.Min(), out _);
            }
        }

        public void HandlerRemotePreLibAnnounce(PeerPreLibAnnouncement peerPreLibAnnouncement)
        {
            CurrentBlockHeight = peerPreLibAnnouncement.BlockHeight;
            CurrentBlockHash = peerPreLibAnnouncement.BlockHash;
            _preLibBlockHeightAndHashMappings[CurrentBlockHeight] = CurrentBlockHash;
            while (_preLibBlockHeightAndHashMappings.Count > 10)
            {
                _preLibBlockHeightAndHashMappings.TryRemove(_preLibBlockHeightAndHashMappings.Keys.Min(), out _);
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