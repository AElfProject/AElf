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
using AElf.OS.Network.Metrics;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    /// <summary>
    /// Represents a connection to a peer.
    /// </summary>
    public class GrpcPeer : IPeer
    {
        private const int MaxMetricsPerMethod = 100;
        
        private const int BlockRequestTimeout = 300;
        private const int BlocksRequestTimeout = 500;
        private const int GetNodesTimeout = 500;

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
        /// Property that describes a valid state. Valid here means that the peer is ready to be used for communications.
        /// </summary>
        public bool IsReady
        {
            get { return (_channel.State == ChannelState.Idle || _channel.State == ChannelState.Ready) && IsConnected; }
        }
        
        public long LastKnownLibHeight { get; private set; }

        public bool IsBest { get; set; }
        public bool IsConnected { get; set; }
        public bool IsShutdown { get; set; }
        public Hash CurrentBlockHash { get; private set; }
        public long CurrentBlockHeight { get; private set; }

        public string IpAddress { get; }

        public PeerInfo Info { get; }
        
        public Handshake LastReceivedHandshake { get; private set; }
        
        public IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        private readonly ConcurrentDictionary<long, Hash> _recentBlockHeightAndHashMappings;
        
        public IReadOnlyDictionary<string, ConcurrentQueue<RequestMetric>> RecentRequestsRoundtripTimes { get; }
        private readonly ConcurrentDictionary<string, ConcurrentQueue<RequestMetric>> _recentRequestsRoundtripTimes;
        
        private AsyncClientStreamingCall<Transaction, VoidReply> _transactionStreamCall;
        private AsyncClientStreamingCall<BlockAnnouncement, VoidReply> _announcementStreamCall;
        private AsyncClientStreamingCall<BlockWithTransactions, VoidReply> _blockStreamCall;

        public GrpcPeer(GrpcClient client, string ipAddress, PeerInfo peerInfo)
        {
            _channel = client.Channel;
            _client = client.Client;

            IpAddress = ipAddress;
            Info = peerInfo;

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

        public Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest)
        {
            GrpcRequest request = new GrpcRequest { ErrorMessage = "Request nodes failed." };
            Metadata data = new Metadata { {GrpcConstants.TimeoutMetadataKey, GetNodesTimeout.ToString()} };
            
            return RequestAsync(_client, c => c.GetNodesAsync(new NodesRequest { MaxCount = count }, data), request);
        }
        
        public async Task<Handshake> DoHandshakeAsync(Handshake handshake)
        {
            GrpcRequest request = new GrpcRequest { ErrorMessage = "Error while updating handshake." };
            
            Metadata data = new Metadata {
                {GrpcConstants.TimeoutMetadataKey, UpdateHandshakeTimeout.ToString()}
            };

            var handshakeReply = await RequestAsync(_client, 
                c => c.DoHandshakeAsync(new HandshakeRequest { Handshake = handshake}, data), request);

            LastReceivedHandshake = handshakeReply?.Handshake;
            
            // Do some pre-checks that represent the minimum acceptable for the peers state.
            if (LastReceivedHandshake?.HandshakeData?.BestChainHead == null)
            {
                IsConnected = false;
                return null;
            }
            
            UpdateLastReceivedHandshake(LastReceivedHandshake);

            return LastReceivedHandshake;
        }

        public void UpdateLastReceivedHandshake(Handshake handshake)
        {
            IsConnected = true;

            LastKnownLibHeight = handshake.HandshakeData.LibBlockHeight;
            CurrentBlockHash = handshake.HandshakeData.BestChainHead.GetHash();
            CurrentBlockHeight = handshake.HandshakeData.BestChainHead.Height;
        }
        
        public async Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash)
        {
            var blockRequest = new BlockRequest {Hash = hash};

            GrpcRequest request = new GrpcRequest
            {
                ErrorMessage = $"Block request for {hash} failed.",
                MetricName = nameof(MetricNames.GetBlock),
                MetricInfo = $"Block request for {hash}"
            };

            Metadata data = new Metadata { {GrpcConstants.TimeoutMetadataKey, BlockRequestTimeout.ToString()} };

            var blockReply = await RequestAsync(_client, c => c.RequestBlockAsync(blockRequest, data), request);

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

            Metadata data = new Metadata {{GrpcConstants.TimeoutMetadataKey, BlocksRequestTimeout.ToString()}};

            var list = await RequestAsync(_client, c => c.RequestBlocksAsync(blockRequest, data), request);

            if (list == null)
                return new List<BlockWithTransactions>();

            return list.Blocks.ToList();
        }

        #region Streaming
        
        public async Task SendBlockAsync(BlockWithTransactions blockWithTransactions)
        {
            if (!IsConnected)
                return;
            
            if (_blockStreamCall == null)
                _blockStreamCall = _client.BlockBroadcastStream();

            try
            {
                await _blockStreamCall.RequestStream.WriteAsync(blockWithTransactions);
            }
            catch (RpcException e)
            {
                _blockStreamCall.Dispose();
                _blockStreamCall = null;
                
                HandleFailure(e, $"Error during block broadcast: {blockWithTransactions.Header.GetHash()}.");
            }
        }

        /// <summary>
        /// Send a announcement to the peer using the stream call.
        /// Note: this method is not thread safe.
        /// </summary>
        public async Task SendAnnouncementAsync(BlockAnnouncement header)
        {
            if (!IsConnected)
                return;
            
            if (_announcementStreamCall == null)
                _announcementStreamCall = _client.AnnouncementBroadcastStream();
            
            try
            {
                await _announcementStreamCall.RequestStream.WriteAsync(header);
            }
            catch (RpcException e)
            {
                _announcementStreamCall.Dispose();
                _announcementStreamCall = null;
                
                HandleFailure(e, $"Error during announcement broadcast: {header.BlockHash}.");
            }
        }
        
        
        /// <summary>
        /// Send a transaction to the peer using the stream call.
        /// Note: this method is not thread safe.
        /// </summary>
        public async Task SendTransactionAsync(Transaction transaction)
        {
            if (!IsConnected)
                return;
                
            if (_transactionStreamCall == null)
                _transactionStreamCall = _client.TransactionBroadcastStream();

            try
            {
                await _transactionStreamCall.RequestStream.WriteAsync(transaction);
            }
            catch (RpcException e)
            {
                _transactionStreamCall.Dispose();
                _transactionStreamCall = null;
                
                HandleFailure(e, $"Error during transaction broadcast: {transaction.GetHash()}.");
            }
        }

        #endregion
        
        // todo consider removing client from the lambda as it is not used. It can be capture by the func.
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
        private void HandleFailure(Exception exception, string errorMessage)
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
            else if (exception.InnerException is RpcException rpcEx && rpcEx.StatusCode == StatusCode.Cancelled)
            {
                message = $"Failed request to {this}: {errorMessage}";
                type = NetworkExceptionType.Unrecoverable;
            }
            
            throw new NetworkException(message, exception, type);
        }

        public async Task<bool> TryRecoverAsync()
        {
            await _channel.TryWaitForStateChangedAsync(_channel.State,
                DateTime.UtcNow.AddSeconds(NetworkConstants.DefaultPeerDialTimeoutInMilliSeconds));

            // Either we connected again or the state change wait timed out.
            if (_channel.State == ChannelState.TransientFailure || _channel.State == ChannelState.Connecting)
                return false;

            return true;
        }
        
        public void AddKnowBlock(BlockAnnouncement blockAnnouncement)
        {
            if (blockAnnouncement.HasFork)
            {
                _recentBlockHeightAndHashMappings.Clear();
                return;
            }
            
            CurrentBlockHeight = blockAnnouncement.BlockHeight;
            CurrentBlockHash = blockAnnouncement.BlockHash;
            _recentBlockHeightAndHashMappings[CurrentBlockHeight] = CurrentBlockHash;
            while (_recentBlockHeightAndHashMappings.Count > 10)
            {
                _recentBlockHeightAndHashMappings.TryRemove(_recentBlockHeightAndHashMappings.Keys.Min(), out _);
            }
        }
        
        public async Task DisconnectAsync(bool gracefulDisconnect)
        {
            IsConnected = false;
            IsShutdown = true;
            
            // send disconnect message if the peer is still connected and the connection
            // is stable.
            if (gracefulDisconnect && IsReady)
            {
                GrpcRequest request = new GrpcRequest { ErrorMessage = "Error while sending disconnect." };
                
                try
                {
                    await RequestAsync(_client, c => c.DisconnectAsync(new DisconnectReason {Why = DisconnectReason.Types.Reason.Shutdown}), request);
                }
                catch (NetworkException)
                {
                    // swallow the exception, we don't care because we're disconnecting.
                }
            }
            
            try
            {
                await _channel.ShutdownAsync();
            }
            catch (InvalidOperationException)
            {
                // if channel already shutdown
            }
        }

        public override string ToString()
        {
            return $"{{ listening-port: {IpAddress}, key: {Info.Pubkey.Substring(0, 45)}... }}";
        }
    }
}