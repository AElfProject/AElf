using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.CrossChain.Grpc
{
    public abstract class CrossChainGrpcClient<TResponse> : IGrpcCrossChainClient where TResponse : IResponseIndexingMessage
    {
        protected CrossChainRpc.CrossChainRpcClient Client;

        protected int LocalChainId;
//        private int _initInterval;
//        private int _adjustedInterval;
//        private const int UnavailableConnectionInterval = 1_000;
//        private readonly Channel _channel;
//        protected CrossChainGrpcClient()
//        {
//            //_channel = channel;
//            Logger = NullLogger<CrossChainGrpcClient<TResponse>>.Instance;
//            //_crossChainDataProducer = crossChainDataProducer;
//            _adjustedInterval = _initInterval = UnavailableConnectionInterval;
//        }

//        private void UpdateRequestInterval(int initInterval) 
//        {
//            _initInterval = initInterval;
//            _adjustedInterval = _initInterval;
//        }

//        public Task HandleEventAsync(GrpcClientRequestIntervalUpdateEvent receivedEventData)
//        {
//            UpdateRequestInterval(receivedEventData.Interval);
//            return Task.CompletedTask;
//        }
       
//        /// <summary>
//        /// Task to read response in loop.
//        /// </summary>
//        /// <param name="call"></param>
//        /// <returns></returns>
//        private Task ReadResponse(AsyncDuplexStreamingCall<RequestCrossChainBlockData, TResponse> call)
//        {
//            var responseReaderTask = Task.Run(async () =>
//            {
//                while (await call.ResponseStream.MoveNext())
//                {
//                    var response = call.ResponseStream.Current;
//
//                    // requestCrossChain failed or useless response
//                    if (!response.Success)
//                    {
//                        _adjustedInterval = AdjustInterval();
//                        continue;
//                    }
//                    if(!_crossChainDataProducer.AddNewBlockInfo(response.BlockInfoResult))
//                        continue;
//                    
//                    _adjustedInterval = _initInterval;
//                    Logger.LogTrace(
//                        $"Received response from chain {ChainHelpers.ConvertChainIdToBase58(response.BlockInfoResult.ChainId)} at height {response.Height}");
//                }
//            });
//    
//            return responseReaderTask;
//        }

//        private int AdjustInterval()
//        {
//            return Math.Min(_adjustedInterval * 2, UnavailableConnectionInterval);
//        }

//        /// <summary>
//        /// Task to create requestCrossChain in loop.
//        /// </summary>
//        /// <param name="call"></param>
//        /// <param name="cancellationToken"></param>
//        /// <param name="chainId"></param>
//        /// <returns></returns>
//        private async Task RequestLoop(AsyncDuplexStreamingCall<RequestCrossChainBlockData, TResponse> call, 
//            CancellationToken cancellationToken, int chainId)
//        {
//            while (!cancellationToken.IsCancellationRequested)
//            {
//                try
//                {
//                    var targetHeight = _crossChainDataProducer.GetChainHeightNeeded(chainId);
//                    var request = new RequestCrossChainBlockData
//                    {
//                        SideChainId = chainId,
//                        NextHeight = targetHeight
//                    };
//                    await call.RequestStream.WriteAsync(request);
//                }
//                catch (ChainCacheNotFoundException)
//                {
//                    Logger.LogWarning($"No cache for chain {ChainHelpers.ConvertChainIdToBase58(chainId)}");
//                }
//                finally
//                {
//                    await Task.Delay(_adjustedInterval);
//                }
//            }
//        }

//        /// <summary>
//        /// Start to requestCrossChain one by one and also response one bye one.
//        /// </summary>
//        /// <param name="chainId"></param>
//        /// <param name="cancellationToken"></param>
//        /// <returns></returns>
//        public async Task StartDuplexStreamingCall(int chainId, CancellationToken cancellationToken)
//        {
//            while (!cancellationToken.IsCancellationRequested)
//            {
//                using (var call = CallWithDuplexStreaming())
//                {
//                    while (_channel.State != ChannelState.Ready)
//                    {
//                        await _channel.WaitForStateChangedAsync(_channel.State);
//                    }
//                
//                    try
//                    {
//                        // response reader task
//                        var responseReaderTask = ReadResponse(call);
//
//                        // requestCrossChain in loop
//                        await RequestLoop(call, cancellationToken, chainId);
//                        await responseReaderTask;
//                    }
//                    catch (RpcException e)
//                    {
//                        var status = e.Status.StatusCode;
//                        if (status != StatusCode.Unavailable && status == StatusCode.DeadlineExceeded)
//                        {
//                            //var detail = e.Status.Detail;
//                            //var task = StartDuplexStreamingCall(chainId, cancellationToken);
//                            Logger.LogError(e, "Grpc cross chain client restarted with exception.");
//                        }
//                    }
//                    finally
//                    {
//                        await call.RequestStream.CompleteAsync();
//                    }
//                }
//            }
//        }

        public async Task<bool> StartIndexingRequest(int remoteChainId, ICrossChainDataProducer crossChainDataProducer)
        {
            var targetHeight = crossChainDataProducer.GetChainHeightNeeded(remoteChainId);
            var requestData = new RequestCrossChainBlockData
            {
                FromChainId = LocalChainId,
                NextHeight = targetHeight
            };

            var serverStream = RequestIndexing(requestData);
            await ReadResponse(serverStream, crossChainDataProducer);
            return true;
        }

        private Task ReadResponse(AsyncServerStreamingCall<TResponse> serverStream, ICrossChainDataProducer crossChainDataProducer)
        {
            var responseReaderTask = Task.Run(async () =>
            {
                while (await serverStream.ResponseStream.MoveNext())
                {
                    var response = serverStream.ResponseStream.Current;

                    // requestCrossChain failed or useless response
                    if (!response.Success)
                    {
                        continue;
                    }
                    if(!crossChainDataProducer.AddNewBlockInfo(response.BlockInfoResult))
                        continue;
                    crossChainDataProducer.Logger.LogTrace(
                        $"Received response from chain {ChainHelpers.ConvertChainIdToBase58(response.BlockInfoResult.ChainId)} at height {response.Height}");
                }
            });
    
            return responseReaderTask;
        }

        public Task<IndexingHandShakeReply> TryHandShakeAsync(int chainId, int localListeningPort)
        {
            var handShakeReply = Client.CrossChainIndexingShake(new IndexingHandShake
            {
                ChainId = chainId,
                ListeningPort = localListeningPort
                // use formatted chainId as certificate name, which can be changed later.  
            });
            return Task.FromResult(handShakeReply);
        }

        //protected abstract AsyncDuplexStreamingCall<RequestCrossChainBlockData, TResponse> CallWithDuplexStreaming(int milliSeconds = 0);

        protected abstract AsyncServerStreamingCall<TResponse> RequestIndexing(
            RequestCrossChainBlockData requestCrossChainBlockData);
        
        /// <summary>
        /// Create a new channel
        /// </summary>
        /// <param name="uriStr"></param>
        /// <param name="crt">Certificate</param>
        /// <returns></returns>
        protected Channel CreateChannel(string uriStr, string crt)
        {
            var channelCredentials = new SslCredentials(crt);
            var channel = new Channel(uriStr, channelCredentials);
            return channel;
        }
    }
    
    public class GrpcClientForSideChain : CrossChainGrpcClient<ResponseSideChainBlockData>
    {

        public GrpcClientForSideChain(string uri, string certificate)
        {
            Client = new CrossChainRpc.CrossChainRpcClient(CreateChannel(uri, certificate));
        }

//        protected override AsyncDuplexStreamingCall<RequestCrossChainBlockData, ResponseSideChainBlockData> CallWithDuplexStreaming(int milliSeconds = 0)
//        {
//            return milliSeconds == 0
//                ? _client.RequestSideChainDuplexStreaming()
//                : _client.RequestSideChainDuplexStreaming(deadline: DateTime.UtcNow.AddMilliseconds(milliSeconds));
//        }

//        public override Task<IndexingHandShakeReply> TryHandShakeAsync(int remoteChainId, int localListeningPort)
//        {
//            // dont handshake with side chain.
//            return await Client.CrossChainIndexingShakeAsync(new IndexingHandShake
//            {
//                SideChainId = remoteChainId,
//                ListeningPort = localListeningPort
//                // use formatted chainId as certificate name, which can be changed later.  
//            });
//        }

        protected override AsyncServerStreamingCall<ResponseSideChainBlockData> RequestIndexing(RequestCrossChainBlockData requestCrossChainBlockData)
        {
            return Client.RequestIndexingSideChain(requestCrossChainBlockData);
        }
    }
    
    public class GrpcClientForParentChain : CrossChainGrpcClient<ResponseParentChainBlockData>
    {
        public GrpcClientForParentChain(string uri, string certificate, int localChainId)
        {
            LocalChainId = localChainId;
            Client = new CrossChainRpc.CrossChainRpcClient(CreateChannel(uri, certificate));
        }

//        public override async Task<IndexingHandShakeReply> TryHandShakeAsync(int remoteChainId, int localListeningPort)
//        {
//            
//        }

//        protected override AsyncDuplexStreamingCall<RequestCrossChainBlockData, ResponseParentChainBlockData> CallWithDuplexStreaming(int milliSeconds = 0)
//        {
//            return milliSeconds == 0
//                ? _client.RequestParentChainDuplexStreaming()
//                : _client.RequestParentChainDuplexStreaming(deadline: DateTime.UtcNow.AddMilliseconds(milliSeconds));
//        }

        protected override AsyncServerStreamingCall<ResponseParentChainBlockData> RequestIndexing(RequestCrossChainBlockData requestCrossChainBlockData)
        {
            return Client.RequestIndexingParentChain(requestCrossChainBlockData);
        }
    }

    public interface IGrpcCrossChainClient
    {
        //Task StartDuplexStreamingCall(int chainId, CancellationToken cancellationToken);
        Task<IndexingHandShakeReply> TryHandShakeAsync(int chainId, int localListeningPort);
        Task<bool> StartIndexingRequest(int remoteChainaId, ICrossChainDataProducer crossChainDataProducer);
    }
}