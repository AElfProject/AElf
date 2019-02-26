using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.CrossChain.Grpc.Client
{
    public abstract class GrpcCrossChainClient<TResponse> : IGrpcCrossChainClient where TResponse : IResponseIndexingMessage
    {
        public ILogger<GrpcCrossChainClient<TResponse>> Logger { get; }
        private int _initInterval;
        private int _adjustedInterval;
        private const int UnavailableConnectionInterval = 1_000;
        private Channel _channel;
        private readonly ICrossChainDataProducer _crossChainDataProducer;
        protected GrpcCrossChainClient(Channel channel, ICrossChainDataProducer crossChainDataProducer)
        {
            _channel = channel;
            Logger = NullLogger<GrpcCrossChainClient<TResponse>>.Instance;
            _crossChainDataProducer = crossChainDataProducer;
            _adjustedInterval = _initInterval;
        }

        public void UpdateRequestInterval(int initInterval)
        {
            _initInterval = initInterval;
            _adjustedInterval = _initInterval;
        }
        
        /// <summary>
        /// Task to read response in loop.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        private Task ReadResponse(AsyncDuplexStreamingCall<RequestCrossChainBlockData, TResponse> call)
        {
            var responseReaderTask = Task.Run(async () =>
            {
                while (await call.ResponseStream.MoveNext())
                {
                    var response = call.ResponseStream.Current;

                    // requestCrossChain failed or useless response
                    if (!response.Success)
                    {
                        _adjustedInterval = AdjustInterval();
                        continue;
                    }
                    if(!_crossChainDataProducer.AddNewBlockInfo(response.BlockInfoResult))
                        continue;
                    
                    _adjustedInterval = _initInterval;
                    Logger.LogTrace(
                        $"Received response from chain {ChainHelpers.ConvertChainIdToBase58(response.BlockInfoResult.ChainId)} at height {response.Height}");
                }
            });

            return responseReaderTask;
        }

        private int AdjustInterval()
        {
            return Math.Min(_adjustedInterval * 2, UnavailableConnectionInterval);
        }

        /// <summary>
        /// Task to create requestCrossChain in loop.
        /// </summary>
        /// <param name="call"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="chainId"></param>
        /// <returns></returns>
        private async Task RequestLoop(AsyncDuplexStreamingCall<RequestCrossChainBlockData, TResponse> call, 
            CancellationToken cancellationToken, int chainId)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var request = new RequestCrossChainBlockData
                {
                    SideChainId = chainId,
                    NextHeight = _crossChainDataProducer.GetChainHeightNeededForCache(chainId)
                };
                //Logger.LogTrace($"New requestCrossChain for height {requestCrossChain.NextHeight} to chain {_targetChainId.DumpHex()}");
                await call.RequestStream.WriteAsync(request);
                await Task.Delay(_adjustedInterval);
            }
        }

        /// <summary>
        /// Start to requestCrossChain one by one and also response one bye one.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartDuplexStreamingCall(int chainId, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using (var call = Call())
                {
                    while (_channel.State != ChannelState.Ready)
                    {
                        await _channel.WaitForStateChangedAsync(_channel.State);
                    }
                
                    try
                    {
                        // response reader task
                        var responseReaderTask = ReadResponse(call);

                        // requestCrossChain in loop
                        await RequestLoop(call, cancellationToken, chainId);
                        await responseReaderTask;
                    }
                    catch (RpcException e)
                    {
                        var status = e.Status.StatusCode;
                        if (status != StatusCode.Unavailable && status == StatusCode.DeadlineExceeded)
                        {
                            //var detail = e.Status.Detail;
                            //var task = StartDuplexStreamingCall(chainId, cancellationToken);
                            Logger.LogError(e, "Grpc cross chain client restarted with exception.");
                        }
                    }
                    finally
                    {
                        await call.RequestStream.CompleteAsync();
                    }
                } 
            }
        }

        protected abstract AsyncDuplexStreamingCall<RequestCrossChainBlockData, TResponse> Call(int milliSeconds = 0);
        protected abstract AsyncServerStreamingCall<TResponse> Call(RequestCrossChainBlockData requestCrossChainBlockData);
    }

    public interface IGrpcCrossChainClient
    {
        Task StartDuplexStreamingCall(int chainId, CancellationToken cancellationToken);
    }
}