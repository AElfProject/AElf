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
        private const int BlockRequestTimeout = 500;
        private const int TransactionSendTimeout = 300;
        private const int BlocksRequestTimeout = 500;

        private const int FinalizeConnectTimeout = 500;
        private const int UpdateHandshakeTimeout = 400;
        
        private enum MetricNames
        {
            Announce,
            GetBlocks,
            GetBlock
        };
        
        private readonly Channel _channel;
        private readonly PeerService.PeerServiceClient _client;

        /// <summary>
        /// Property that describes a valid state. Valid here means that the peer is ready to be used for communication.
        /// </summary>
        public bool IsReady
        {
            get { return _channel.State == ChannelState.Idle || _channel.State == ChannelState.Ready; }
        }
        
        public long LastKnowLibHeight { get; private set; }

        public bool IsBest { get; set; }
        public Hash CurrentBlockHash { get; private set; }
        public long CurrentBlockHeight { get; private set; }
        
        public string PeerIpAddress { get; }
        public string PubKey { get; }
        public int ProtocolVersion { get; }
        public long ConnectionTime { get; }
        public bool Inbound { get; }
        public long StartHeight { get; }

        public bool CanStreamTransactions { get; private set; }
        public bool CanStreamAnnouncements { get; private set; }

        public IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        private readonly ConcurrentDictionary<long, Hash> _recentBlockHeightAndHashMappings;
        
        public IReadOnlyDictionary<string, ConcurrentQueue<RequestMetric>> RecentRequestsRoundtripTimes { get; }
        private readonly ConcurrentDictionary<string, ConcurrentQueue<RequestMetric>> _recentRequestsRoundtripTimes;
        
        private AsyncClientStreamingCall<Transaction, VoidReply> _transactionStreamCall;
        private AsyncClientStreamingCall<PeerNewBlockAnnouncement, VoidReply> _announcementStreamCall;

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
            LastKnowLibHeight = peerInfo.LibHeightAtHandshake;

            _recentBlockHeightAndHashMappings = new ConcurrentDictionary<long, Hash>();
            RecentBlockHeightAndHashMappings = new ReadOnlyDictionary<long, Hash>(_recentBlockHeightAndHashMappings);
            
            _recentRequestsRoundtripTimes = new ConcurrentDictionary<string, ConcurrentQueue<RequestMetric>>();
            RecentRequestsRoundtripTimes =
                new ReadOnlyDictionary<string, ConcurrentQueue<RequestMetric>>(_recentRequestsRoundtripTimes);

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
        
        public async Task UpdateHandshakeAsync()
        {
            GrpcRequest request = new GrpcRequest
            {
                ErrorMessage = $"Error while updating handshake."
            };
            
            Metadata data = new Metadata
            {
                {GrpcConstants.TimeoutMetadataKey, UpdateHandshakeTimeout.ToString()}
            };
            
            var handshake = await RequestAsync(_client, c => c.UpdateHandshakeAsync(new UpdateHandshakeRequest(), data), request);
             
            if (handshake != null)
                LastKnowLibHeight = handshake.LibBlockHeight;
        }

        public async Task FinalizeConnectAsync()
        {
            GrpcRequest request = new GrpcRequest { ErrorMessage = $"Error while finalizing request to {this}." };
            Metadata data = new Metadata { {GrpcConstants.TimeoutMetadataKey, FinalizeConnectTimeout.ToString()} };

            await RequestAsync(_client, c => c.FinalizeConnectAsync(new Handshake(), data), request);
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

        #region Streaming

        public void StartAnnouncementStreaming()
        {
            _announcementStreamCall = _client.AnnouncementBroadcastStream();
            CanStreamAnnouncements = true;
        }
        
        public async Task AnnounceAsync(PeerNewBlockAnnouncement header)
        {
            if (!CanStreamAnnouncements)
            {
                // if we cannot stream we use the unary version of the send.
                await UnaryAnnounceAsync(header);
                return;
            }
            
            try
            {
                await _announcementStreamCall.RequestStream.WriteAsync(header);
            }
            catch (RpcException e)
            {
                if (!CanStreamAnnouncements) // Already down
                    return;
                
                CanStreamAnnouncements = false;
                _announcementStreamCall.Dispose();
                
                throw new NetworkException($"Failed stream to {this}: ", e, NetworkExceptionType.AnnounceStream);
            }
        }

        public void StartTransactionStreaming()
        {
            _transactionStreamCall = _client.TransactionBroadcastStream();
            CanStreamTransactions = true;
        }

        public async Task SendTransactionAsync(Transaction tx)
        {
            if (!CanStreamTransactions)
            {
                // if we cannot stream we use the unary version of the send.
                await UnarySendTransactionAsync(tx);
                return;
            }
            
            try
            {
                await _transactionStreamCall.RequestStream.WriteAsync(tx);
            }
            catch (RpcException e)
            {
                if (!CanStreamTransactions) // Already down
                    return;
                
                CanStreamTransactions = false;
                _transactionStreamCall.Dispose();
                
                throw new NetworkException($"Failed stream to {this}: ", e, NetworkExceptionType.TransactionStream);
            }
        }

        #endregion
        
        public Task UnarySendTransactionAsync(Transaction tx)
        {
            var request = new GrpcRequest { ErrorMessage = $"Broadcast transaction for {tx.GetHash()} failed." };
            var data = new Metadata {{ GrpcConstants.TimeoutMetadataKey, TransactionSendTimeout.ToString() }};
            
            return RequestAsync(_client, c => c.SendTransactionAsync(tx, data), request);
        }
        
        public Task UnaryAnnounceAsync(PeerNewBlockAnnouncement header)
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
            string message = $"Failed request to {this}: {errorMessage}";
            NetworkExceptionType type = NetworkExceptionType.Rpc;
            
            if (_channel.State == ChannelState.Shutdown)
            {
                message = $"Peer is shutdown - {this}: {errorMessage}";
                type = NetworkExceptionType.Unrecoverable;
            }
            else if (_channel.State == ChannelState.TransientFailure || _channel.State == ChannelState.Connecting)
            {
                message = $"Failed request to {this}: {errorMessage}";
                type = NetworkExceptionType.PeerUnstable;
            }
            else if (exceptions.InnerException is RpcException rpcEx && rpcEx.StatusCode == StatusCode.Cancelled)
            {
                message = $"Failed request to {this}: {errorMessage}";
                type = NetworkExceptionType.Unrecoverable;
            }
            
            throw new NetworkException(message, exceptions, type);
        }

        public async Task<bool> TryWaitForStateChangedAsync()
        {
            await _channel.TryWaitForStateChangedAsync(_channel.State,
                DateTime.UtcNow.AddSeconds(NetworkConstants.DefaultPeerDialTimeoutInMilliSeconds));

            // Either we connected again or the state change wait timed out.
            if (_channel.State == ChannelState.TransientFailure || _channel.State == ChannelState.Connecting)
                return false;

            return true;
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